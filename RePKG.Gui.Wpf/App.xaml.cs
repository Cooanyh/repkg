using System;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace RePKG.Gui.Wpf
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            UiLanguageState.CurrentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en"
                ? UiLanguage.English
                : UiLanguage.Chinese;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                var window = new MainWindow();
                MainWindow = window;
                window.Show();
                window.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    UiTextCatalog.Get(UiLanguageState.CurrentLanguage, "app.startupFailedTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.Exception.ToString(),
                UiTextCatalog.Get(UiLanguageState.CurrentLanguage, "app.runtimeErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
