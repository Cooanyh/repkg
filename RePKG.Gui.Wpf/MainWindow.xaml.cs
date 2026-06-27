using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RePKG.Application;
using RePKG.Command;

namespace RePKG.Gui.Wpf
{
    internal enum PreviewContentKind
    {
        None,
        Image,
        Video
    }

    public partial class MainWindow : Window
    {
        private readonly GuiCommandRunner _runner;
        private bool _updatingOutputDirectory;
        private bool _outputDirectoryWasEdited;
        private string _lastSuggestedOutputDirectory;
        private string _activeOutputDirectory;
        private readonly DispatcherTimer _mediaPositionTimer;
        private bool _isMediaScrubbing;
        private bool _isMediaPlaying;
        private PreviewContentKind _previewContentKind;
        private string _activeStatusKey = "status.idle";
        private object[] _activeStatusArgs = Array.Empty<object>();

        public MainWindow()
        {
            InitializeComponent();
            _runner = new GuiCommandRunner(AppendLogLine);
            _mediaPositionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _mediaPositionTimer.Tick += MediaPositionTimer_OnTick;
            LanguageComboBox.SelectedIndex = UiLanguageState.CurrentLanguage == UiLanguage.English ? 1 : 0;
            PreviewMedia.Volume = VolumeSlider.Value;
            ResetPreviewState();
            ApplyLanguage();
            TryApplyWindowIcon();
        }

        private UiLanguage CurrentLanguage
        {
            get { return LanguageComboBox.SelectedIndex == 1 ? UiLanguage.English : UiLanguage.Chinese; }
        }

        private string T(string key, params object[] args)
        {
            return UiTextCatalog.Get(CurrentLanguage, key, args);
        }

        private void TryApplyWindowIcon()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    return;
                }

                using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath))
                {
                    if (icon == null)
                    {
                        return;
                    }

                    var iconSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));

                    Icon = iconSource;
                    HeroIconImage.Source = iconSource;
                }
            }
            catch
            {
                // Ignore icon-load issues so the window can still open normally.
            }
        }

        private async void RunExtract_Click(object sender, RoutedEventArgs e)
        {
            var input = NormalizePathInput(ExtractInputTextBox.Text);
            if (!ValidatePath(input, T("dialog.invalidExtractPath")))
            {
                return;
            }

            ExtractInputTextBox.Text = input;

            var output = NormalizePathInput(OutputDirectoryTextBox.Text);
            if (string.IsNullOrWhiteSpace(output))
            {
                output = GetDefaultOutputDirectory(input);
                SetSuggestedOutputDirectory(output);
            }

            var maxMipmapBytes = ParseMaxMipmapBytes();
            if (maxMipmapBytes <= 0)
            {
                MessageBox.Show(this, T("dialog.invalidMaxMipmap"), T("dialog.invalidConfigTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RuntimeSafetySettings.MaximumMipmapByteCount = maxMipmapBytes;
            _activeOutputDirectory = output;
            SetStatus("status.extracting", maxMipmapBytes / 1024 / 1024);

            var options = new ExtractOptions
            {
                Input = input,
                OutputDirectory = output,
                IgnoreExts = NullIfWhiteSpace(IgnoreExtsTextBox.Text),
                OnlyExts = NullIfWhiteSpace(OnlyExtsTextBox.Text),
                TexDirectory = ExtractTexDirectoryCheckBox.IsChecked == true,
                SingleDir = SingleDirCheckBox.IsChecked == true,
                Recursive = RecursiveCheckBox.IsChecked == true,
                CopyProject = CopyProjectCheckBox.IsChecked == true,
                UseName = UseNameCheckBox.IsChecked == true,
                NoTexConvert = NoTexConvertCheckBox.IsChecked == true,
                Overwrite = OverwriteCheckBox.IsChecked == true
            };

            await RunCommandAsync("extract", "operation.extract", () => _runner.RunExtractAsync(options), true);
        }

        private async void RunInfo_Click(object sender, RoutedEventArgs e)
        {
            var input = NormalizePathInput(InfoInputTextBox.Text);
            if (!ValidatePath(input, T("dialog.invalidInfoPath")))
            {
                return;
            }

            InfoInputTextBox.Text = input;
            var selectedSortItem = InfoSortByComboBox.SelectedItem as ComboBoxItem;

            var options = new InfoOptions
            {
                Input = input,
                Sort = InfoSortCheckBox.IsChecked == true,
                SortBy = selectedSortItem?.Tag?.ToString() ?? "name",
                TexDirectory = InfoTexDirectoryCheckBox.IsChecked == true,
                ProjectInfo = NullIfWhiteSpace(ProjectInfoTextBox.Text),
                PrintEntries = PrintEntriesCheckBox.IsChecked == true,
                TitleFilter = NullIfWhiteSpace(TitleFilterTextBox.Text)
            };

            await RunCommandAsync("info", "operation.info", () => _runner.RunInfoAsync(options), false);
        }

        private async Task RunCommandAsync(string operationName, string operationTextKey, Func<Task> action, bool refreshPreview)
        {
            SetBusyState(true, operationTextKey);
            AppendLogLine(string.Empty);
            AppendLogLine("============================================================");
            AppendLogLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting {operationName}");

            try
            {
                await action();
                AppendLogLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operationName} finished");
                SetStatus(refreshPreview ? "status.extractFinished" : "status.infoFinished");

                if (refreshPreview)
                {
                    RefreshPreviewFiles();
                }
            }
            catch (Exception ex)
            {
                AppendLogLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {operationName} failed");
                AppendLogLine(ex.ToString());
                SetStatus("status.failed", T(operationTextKey));
                MessageBox.Show(this, ex.Message, T("dialog.runFailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusyState(false, string.Empty);
            }
        }

        private void SetBusyState(bool busy, string operationTextKey)
        {
            RunExtractButton.IsEnabled = !busy;
            RunInfoButton.IsEnabled = !busy;
            Cursor = busy ? Cursors.Wait : Cursors.Arrow;
            if (busy)
            {
                SetStatus("status.running", T(operationTextKey));
            }
        }

        private void AppendLogLine(string line)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(line))
                {
                    LogTextBox.AppendText(Environment.NewLine);
                }
                else
                {
                    LogTextBox.AppendText(line + Environment.NewLine);
                }

                LogTextBox.ScrollToEnd();
            });
        }

        private void RefreshPreviewFiles()
        {
            ResetPreviewState();

            var output = NormalizePathInput(OutputDirectoryTextBox.Text);
            if (string.IsNullOrWhiteSpace(output) || !Directory.Exists(output))
            {
                PreviewFilesListBox.ItemsSource = null;
                return;
            }

            var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".mp4", ".webm"
            };

            var files = Directory.EnumerateFiles(output, "*.*", SearchOption.AllDirectories)
                .Where(x => supportedExtensions.Contains(System.IO.Path.GetExtension(x)))
                .Select(x => new PreviewFileItem
                {
                    Path = x,
                    Extension = System.IO.Path.GetExtension(x),
                    DisplayName = System.IO.Path.GetFileName(x)
                })
                .OrderBy(x => x.DisplayName)
                .ToList();

            PreviewFilesListBox.ItemsSource = files;
            if (files.Count > 0)
            {
                PreviewFilesListBox.SelectedIndex = 0;
            }
            else
            {
                SelectedPreviewTextBlock.Text = T("preview.noFiles");
                UpdatePreviewActionButtons();
            }
        }

        private void ShowPreview(PreviewFileItem item)
        {
            if (item == null || !File.Exists(item.Path))
            {
                ResetPreviewState();
                return;
            }

            SelectedPreviewTextBlock.Text = item.Path;
            PreviewPlaceholder.Visibility = Visibility.Collapsed;

            if (IsImageFile(item.Extension))
            {
                _previewContentKind = PreviewContentKind.Image;
                ResetMediaPlayback(clearSource: true);
                PreviewPlaceholder.Visibility = Visibility.Collapsed;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(item.Path);
                bitmap.DecodePixelWidth = 960;
                bitmap.EndInit();
                bitmap.Freeze();

                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
                MediaControlsPanel.Visibility = Visibility.Collapsed;
                SetStatus("status.imageReady");
                return;
            }

            if (IsVideoFile(item.Extension))
            {
                _previewContentKind = PreviewContentKind.Video;
                PreviewImage.Source = null;
                PreviewImage.Visibility = Visibility.Collapsed;
                ResetMediaPlayback(clearSource: true);
                PreviewMedia.Source = new Uri(item.Path);
                PreviewMedia.Visibility = Visibility.Visible;
                MediaControlsPanel.Visibility = Visibility.Visible;
                PreviewMedia.Position = TimeSpan.Zero;
                PreviewMedia.Play();
                _isMediaPlaying = true;
                UpdateMediaButtons();
            }
        }

        private static bool IsImageFile(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsVideoFile(string extension)
        {
            switch ((extension ?? string.Empty).ToLowerInvariant())
            {
                case ".mp4":
                case ".webm":
                    return true;
                default:
                    return false;
            }
        }

        private bool ValidatePath(string path, string message)
        {
            if (!string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path)))
            {
                return true;
            }

            MessageBox.Show(this, message, T("dialog.invalidPathTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private static string NullIfWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizePathInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().Trim('"');
        }

        private static string GetDefaultOutputDirectory(string inputPath)
        {
            var normalized = NormalizePathInput(inputPath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            if (File.Exists(normalized))
            {
                var parent = System.IO.Path.GetDirectoryName(normalized);
                return string.IsNullOrWhiteSpace(parent) ? string.Empty : System.IO.Path.Combine(parent, "output");
            }

            if (Directory.Exists(normalized))
            {
                return System.IO.Path.Combine(normalized, "output");
            }

            var fileParent = System.IO.Path.GetDirectoryName(normalized);
            return string.IsNullOrWhiteSpace(fileParent) ? string.Empty : System.IO.Path.Combine(fileParent, "output");
        }

        private int ParseMaxMipmapBytes()
        {
            int valueMb;
            if (!int.TryParse(MaxMipmapMbTextBox.Text.Trim(), out valueMb) || valueMb <= 0)
            {
                return 0;
            }

            if (valueMb > 2048)
            {
                valueMb = 2048;
                MaxMipmapMbTextBox.Text = "2048";
            }

            return checked(valueMb * 1024 * 1024);
        }

        private void SetSuggestedOutputDirectory(string path)
        {
            _updatingOutputDirectory = true;
            OutputDirectoryTextBox.Text = path;
            _updatingOutputDirectory = false;
            _lastSuggestedOutputDirectory = path;
            _outputDirectoryWasEdited = false;
        }

        private void ExtractInputTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var suggested = GetDefaultOutputDirectory(ExtractInputTextBox.Text);
            if (string.IsNullOrWhiteSpace(suggested))
            {
                return;
            }

            var currentOutput = NormalizePathInput(OutputDirectoryTextBox.Text);
            if (!_outputDirectoryWasEdited || string.IsNullOrWhiteSpace(currentOutput) ||
                string.Equals(currentOutput, _lastSuggestedOutputDirectory, StringComparison.OrdinalIgnoreCase))
            {
                SetSuggestedOutputDirectory(suggested);
            }
        }

        private void OutputDirectoryTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingOutputDirectory)
            {
                return;
            }

            _outputDirectoryWasEdited = true;
        }

        private void BrowseExtractFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = CurrentLanguage == UiLanguage.English
                    ? "RePKG files|*.pkg;*.tex|All files|*.*"
                    : "RePKG 文件|*.pkg;*.tex|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                ExtractInputTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseExtractFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ExtractInputTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (Directory.Exists(OutputDirectoryTextBox.Text))
                {
                    dialog.SelectedPath = OutputDirectoryTextBox.Text;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputDirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseInfoFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = CurrentLanguage == UiLanguage.English
                    ? "RePKG files|*.pkg;*.tex|All files|*.*"
                    : "RePKG 文件|*.pkg;*.tex|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                InfoInputTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseInfoFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    InfoInputTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OpenOutput_Click(object sender, RoutedEventArgs e)
        {
            var output = NormalizePathInput(OutputDirectoryTextBox.Text);
            if (!Directory.Exists(output))
            {
                MessageBox.Show(this, T("dialog.outputMissing"), T("dialog.outputMissingTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = output,
                UseShellExecute = true
            });
        }

        private void ClearExtractInput_Click(object sender, RoutedEventArgs e)
        {
            ExtractInputTextBox.Clear();
        }

        private void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            RefreshPreviewFiles();
        }

        private void PreviewFilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PreviewFilesListBox.Focus();
            ShowPreview(PreviewFilesListBox.SelectedItem as PreviewFileItem);
            UpdatePreviewActionButtons();
        }

        private void PreviewFilesListBox_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!PreviewFilesListBox.IsKeyboardFocusWithin)
            {
                Keyboard.Focus(PreviewFilesListBox);
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private void CopyPreviewFile_Click(object sender, RoutedEventArgs e)
        {
            var item = ResolvePreviewFileItem(sender, notifyIfMissing: true);
            if (item == null)
            {
                return;
            }

            CopyPreviewFileToClipboard(item);
            SetStatus("status.copiedFile", item.DisplayName);
        }

        private void OpenPreviewFile_Click(object sender, RoutedEventArgs e)
        {
            var item = ResolvePreviewFileItem(sender, notifyIfMissing: true);
            if (item == null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = item.Path,
                UseShellExecute = true
            });
        }

        private void SavePreviewFile_Click(object sender, RoutedEventArgs e)
        {
            var item = ResolvePreviewFileItem(sender, notifyIfMissing: true);
            if (item == null)
            {
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = item.DisplayName,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                File.Copy(item.Path, dialog.FileName, true);
            }
        }

        private void DeletePreviewFile_Click(object sender, RoutedEventArgs e)
        {
            var item = ResolvePreviewFileItem(sender, notifyIfMissing: true);
            if (item == null)
            {
                return;
            }

            var result = MessageBox.Show(
                this,
                T("dialog.confirmDeleteFile", item.Path),
                T("dialog.confirmDeleteFileTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            DeletePreviewFile(item);
        }

        private void PreviewFilesListBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var item = GetSelectedPreviewFileOrNotify();
                if (item != null)
                {
                    CopyPreviewFileToClipboard(item);
                    SetStatus("status.copiedFile", item.DisplayName);
                }

                e.Handled = true;
            }
        }

        private void DeleteExtracted_Click(object sender, RoutedEventArgs e)
        {
            var output = NormalizePathInput(OutputDirectoryTextBox.Text);
            if (!Directory.Exists(output))
            {
                MessageBox.Show(this, T("dialog.outputMissing"), T("dialog.outputMissingTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                this,
                T("dialog.confirmDeleteExtracted", output),
                T("dialog.confirmDeleteExtractedTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            Directory.Delete(output, true);
            ResetPreviewState();
            PreviewFilesListBox.ItemsSource = null;
            LogTextBox.Clear();
            SetStatus("status.deletedExtracted");
        }

        private void LanguageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyLanguage();
        }

        private void ToggleMediaPlayback_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewMedia.Visibility != Visibility.Visible || PreviewMedia.Source == null)
            {
                return;
            }

            if (_isMediaPlaying)
            {
                PreviewMedia.Pause();
                _isMediaPlaying = false;
            }
            else
            {
                PreviewMedia.Play();
                _isMediaPlaying = true;
            }

            UpdateMediaButtons();
        }

        private void StopMediaPlayback_Click(object sender, RoutedEventArgs e)
        {
            if (PreviewMedia.Visibility != Visibility.Visible || PreviewMedia.Source == null)
            {
                return;
            }

            PreviewMedia.Stop();
            PreviewMedia.Position = TimeSpan.Zero;
            _isMediaPlaying = false;
            UpdateMediaPositionDisplay(TimeSpan.Zero, GetNaturalDuration());
            UpdateMediaButtons();
        }

        private void PreviewMedia_OnMediaOpened(object sender, RoutedEventArgs e)
        {
            var duration = GetNaturalDuration();
            MediaPositionSlider.Minimum = 0;
            MediaPositionSlider.Maximum = Math.Max(duration.TotalSeconds, 1);
            MediaPositionSlider.Value = 0;
            UpdateMediaPositionDisplay(TimeSpan.Zero, duration);
            _mediaPositionTimer.Start();
            _isMediaPlaying = true;
            UpdateMediaButtons();
            SetStatus("status.videoReady");
        }

        private void PreviewMedia_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            PreviewMedia.Position = TimeSpan.Zero;
            _isMediaPlaying = false;
            UpdateMediaPositionDisplay(TimeSpan.Zero, GetNaturalDuration());
            UpdateMediaButtons();
        }

        private void MediaPositionTimer_OnTick(object sender, EventArgs e)
        {
            if (_isMediaScrubbing || PreviewMedia.Visibility != Visibility.Visible || PreviewMedia.Source == null)
            {
                return;
            }

            UpdateMediaPositionDisplay(PreviewMedia.Position, GetNaturalDuration());
        }

        private void MediaPositionSlider_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isMediaScrubbing = true;
        }

        private void MediaPositionSlider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SeekPreviewMediaToSlider();
            _isMediaScrubbing = false;
        }

        private void MediaPositionSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isMediaScrubbing)
            {
                return;
            }

            var previewPosition = TimeSpan.FromSeconds(MediaPositionSlider.Value);
            UpdateMediaPositionDisplay(previewPosition, GetNaturalDuration(), updateSlider: false);
        }

        private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            PreviewMedia.Volume = VolumeSlider.Value;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            var first = files?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(first))
            {
                return;
            }

            ExtractInputTextBox.Text = first;
            SetStatus("status.dragLoaded", first);
        }

        private void SeekPreviewMediaToSlider()
        {
            if (PreviewMedia.Visibility != Visibility.Visible || PreviewMedia.Source == null)
            {
                return;
            }

            var duration = GetNaturalDuration();
            var target = TimeSpan.FromSeconds(MediaPositionSlider.Value);
            if (target > duration)
            {
                target = duration;
            }

            PreviewMedia.Position = target;
            UpdateMediaPositionDisplay(target, duration, updateSlider: false);
        }

        private void UpdateMediaPositionDisplay(TimeSpan current, TimeSpan total, bool updateSlider = true)
        {
            if (updateSlider)
            {
                MediaPositionSlider.Value = Math.Max(MediaPositionSlider.Minimum, Math.Min(MediaPositionSlider.Maximum, current.TotalSeconds));
            }

            CurrentTimeTextBlock.Text = FormatMediaTime(current);
            TotalTimeTextBlock.Text = FormatMediaTime(total);
        }

        private void UpdateMediaButtons()
        {
            var hasVideo = PreviewMedia.Visibility == Visibility.Visible && PreviewMedia.Source != null;
            MediaControlsPanel.Visibility = hasVideo ? Visibility.Visible : Visibility.Collapsed;
            PlayPauseButton.IsEnabled = hasVideo;
            StopMediaButton.IsEnabled = hasVideo;
            MediaPositionSlider.IsEnabled = hasVideo;
            VolumeSlider.IsEnabled = hasVideo;
            PlayPauseButton.Content = _isMediaPlaying ? T("button.pause") : T("button.play");
            StopMediaButton.Content = T("button.stop");
        }

        private void ResetPreviewState()
        {
            _previewContentKind = PreviewContentKind.None;
            ResetMediaPlayback(clearSource: true);
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            MediaControlsPanel.Visibility = Visibility.Collapsed;
            PreviewPlaceholder.Visibility = Visibility.Visible;
            PreviewPlaceholderTextBlock.Text = T("preview.placeholder");
            SelectedPreviewTextBlock.Text = T("preview.noFiles");
            UpdatePreviewActionButtons();
            SetStatus("status.idle");
        }

        private void ResetMediaPlayback(bool clearSource)
        {
            _mediaPositionTimer.Stop();
            _isMediaScrubbing = false;
            _isMediaPlaying = false;
            PreviewMedia.Stop();
            PreviewMedia.Visibility = Visibility.Collapsed;

            if (clearSource)
            {
                PreviewMedia.Source = null;
            }

            UpdateMediaPositionDisplay(TimeSpan.Zero, TimeSpan.Zero);
            UpdateMediaButtons();
        }

        private void SetStatus(string key, params object[] args)
        {
            _activeStatusKey = key;
            _activeStatusArgs = args ?? Array.Empty<object>();
            StatusTextBlock.Text = T(key, _activeStatusArgs);
        }

        private void ApplyLanguage()
        {
            UiLanguageState.CurrentLanguage = CurrentLanguage;
            RootWindow.Title = T("window.title");
            HeroSubtitleTextBlock.Text = T("hero.subtitle");
            DragHintTextBlock.Text = T("hero.dragHint");
            LanguageLabelTextBlock.Text = "语言 / Language";

            ExtractSectionTitleTextBlock.Text = T("section.extract");
            ExtractInputLabelTextBlock.Text = T("label.inputPath");
            BrowseExtractFileButton.Content = T("button.selectFile");
            BrowseExtractFolderButton.Content = T("button.selectFolder");
            ClearExtractInputButton.Content = T("button.clear");
            OutputDirectoryLabelTextBlock.Text = T("label.outputDirectory");
            BrowseOutputButton.Content = T("button.selectOutput");
            OpenOutputButton.Content = T("button.openOutput");
            IgnoreExtsLabelTextBlock.Text = T("label.ignoreExts");
            OnlyExtsLabelTextBlock.Text = T("label.onlyExts");
            MaxMipmapLabelTextBlock.Text = T("label.maxMipmap");
            MaxMipmapHintTextBlock.Text = T("label.maxMipmapHint");
            ExtractTexDirectoryCheckBox.Content = T("checkbox.texDirectory");
            SingleDirCheckBox.Content = T("checkbox.singleDir");
            RecursiveCheckBox.Content = T("checkbox.recursive");
            CopyProjectCheckBox.Content = T("checkbox.copyProject");
            UseNameCheckBox.Content = T("checkbox.useName");
            NoTexConvertCheckBox.Content = T("checkbox.noTexConvert");
            OverwriteCheckBox.Content = T("checkbox.overwrite");
            RunExtractButton.Content = T("button.runExtract");
            RefreshPreviewButton.Content = T("button.refreshPreview");
            DeleteExtractedButton.Content = T("button.deleteExtracted");

            InfoSectionTitleTextBlock.Text = T("section.info");
            InfoInputLabelTextBlock.Text = T("label.inputPath");
            BrowseInfoFileButton.Content = T("button.selectFile");
            BrowseInfoFolderButton.Content = T("button.selectFolder");
            InfoSortLabelTextBlock.Text = T("label.infoSort");
            ProjectInfoLabelTextBlock.Text = T("label.projectInfo");
            TitleFilterLabelTextBlock.Text = T("label.titleFilter");
            InfoSortCheckBox.Content = T("checkbox.infoSort");
            InfoTexDirectoryCheckBox.Content = T("checkbox.texDirectory");
            PrintEntriesCheckBox.Content = T("checkbox.printEntries");
            RunInfoButton.Content = T("button.runInfo");
            InfoSortNameItem.Content = T("sort.name");
            InfoSortExtensionItem.Content = T("sort.extension");
            InfoSortSizeItem.Content = T("sort.size");

            PreviewSectionTitleTextBlock.Text = T("section.preview");
            SelectedPreviewLabelTextBlock.Text = T("label.currentSelection");
            PreviewFilesTitleTextBlock.Text = T("section.previewFiles");
            LogTitleTextBlock.Text = T("section.log");
            ClearLogButton.Content = T("button.clearLog");
            PreviewOpenButton.Content = T("button.openFile");
            PreviewSaveButton.Content = T("button.saveFile");
            PreviewDeleteButton.Content = T("button.deleteFile");
            VolumeLabelTextBlock.Text = T("label.volume");
            PreviewPlaceholderTextBlock.Text = T("preview.placeholder");

            UpdateMediaButtons();
            StatusTextBlock.Text = T(_activeStatusKey, _activeStatusArgs);

            if (_previewContentKind == PreviewContentKind.None)
            {
                SelectedPreviewTextBlock.Text = T("preview.noFiles");
            }
        }

        private void UpdatePreviewActionButtons()
        {
            var hasPreviewFile = PreviewFilesListBox?.SelectedItem is PreviewFileItem item && File.Exists(item.Path);
            PreviewOpenButton.IsEnabled = hasPreviewFile;
            PreviewSaveButton.IsEnabled = hasPreviewFile;
            PreviewDeleteButton.IsEnabled = hasPreviewFile;
        }

        private TimeSpan GetNaturalDuration()
        {
            return PreviewMedia.NaturalDuration.HasTimeSpan ? PreviewMedia.NaturalDuration.TimeSpan : TimeSpan.Zero;
        }

        private static string FormatMediaTime(TimeSpan value)
        {
            return value.TotalHours >= 1
                ? value.ToString(@"hh\:mm\:ss")
                : value.ToString(@"mm\:ss");
        }

        private PreviewFileItem GetSelectedPreviewFileOrNotify()
        {
            var item = PreviewFilesListBox.SelectedItem as PreviewFileItem;
            if (item != null && File.Exists(item.Path))
            {
                return item;
            }

            MessageBox.Show(this, T("dialog.noPreviewFile"), T("dialog.noPreviewFileTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        private PreviewFileItem ResolvePreviewFileItem(object sender, bool notifyIfMissing)
        {
            var element = sender as FrameworkElement;
            var taggedItem = element?.Tag as PreviewFileItem;
            if (taggedItem != null && File.Exists(taggedItem.Path))
            {
                PreviewFilesListBox.SelectedItem = taggedItem;
                return taggedItem;
            }

            return notifyIfMissing ? GetSelectedPreviewFileOrNotify() : null;
        }

        private void DeletePreviewFile(PreviewFileItem item)
        {
            File.Delete(item.Path);
            SetStatus("status.deletedFile", item.DisplayName);
            RefreshPreviewFiles();
        }

        private static void CopyPreviewFileToClipboard(PreviewFileItem item)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, new[] { item.Path }, false);

            // Mark as copy so Explorer and other shell targets treat this as a file copy operation.
            data.SetData("Preferred DropEffect", new MemoryStream(new byte[] { 5, 0, 0, 0 }), false);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Clipboard.Clear();
                    Clipboard.SetDataObject(data, true);
                    return;
                }
                catch when (attempt < 4)
                {
                    Thread.Sleep(40);
                }
            }

            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);
        }
    }
}
