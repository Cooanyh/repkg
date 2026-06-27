using System;
using System.Collections.Generic;

namespace RePKG.Gui.Wpf
{
    internal enum UiLanguage
    {
        Chinese,
        English
    }

    internal static class UiLanguageState
    {
        public static UiLanguage CurrentLanguage { get; set; } = UiLanguage.Chinese;
    }

    internal static class UiTextCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> ChineseTexts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["window.title"] = "RePKG",
            ["hero.subtitle"] = "提取 Wallpaper Engine PKG，并在右侧快速预览素材。",
            ["hero.dragHint"] = "拖入 pkg / tex / 文件夹",
            ["section.extract"] = "提取",
            ["section.info"] = "信息",
            ["section.preview"] = "预览",
            ["section.previewFiles"] = "预览文件",
            ["section.log"] = "日志",
            ["label.inputPath"] = "输入文件或目录",
            ["label.outputDirectory"] = "输出目录",
            ["label.ignoreExts"] = "忽略扩展名",
            ["label.onlyExts"] = "仅保留扩展名",
            ["label.maxMipmap"] = "单 mipmap 安全上限（MB）",
            ["label.maxMipmapHint"] = "默认 1024 MB。超大视频纹理可提高，建议按需调整。",
            ["label.infoSort"] = "排序方式",
            ["label.projectInfo"] = "project.json 字段",
            ["label.titleFilter"] = "标题过滤",
            ["label.currentSelection"] = "当前选择",
            ["label.volume"] = "音量",
            ["button.selectFile"] = "选择文件",
            ["button.selectFolder"] = "选择目录",
            ["button.clear"] = "清空",
            ["button.selectOutput"] = "选择输出",
            ["button.openOutput"] = "打开输出",
            ["button.runExtract"] = "开始提取",
            ["button.refreshPreview"] = "刷新预览",
            ["button.deleteExtracted"] = "删除提取",
            ["button.runInfo"] = "运行 Info",
            ["button.play"] = "播放",
            ["button.pause"] = "暂停",
            ["button.stop"] = "停止",
            ["button.clearLog"] = "清空",
            ["button.copyFile"] = "复制文件",
            ["button.openFile"] = "打开文件",
            ["button.saveFile"] = "保存文件",
            ["button.deleteFile"] = "删除文件",
            ["checkbox.texDirectory"] = "TEX 目录模式",
            ["checkbox.singleDir"] = "单目录输出",
            ["checkbox.recursive"] = "递归子目录",
            ["checkbox.copyProject"] = "复制 project 文件",
            ["checkbox.useName"] = "使用标题命名",
            ["checkbox.noTexConvert"] = "不转换 TEX",
            ["checkbox.overwrite"] = "覆盖现有文件",
            ["checkbox.infoSort"] = "排序",
            ["checkbox.printEntries"] = "打印条目",
            ["sort.name"] = "名称",
            ["sort.extension"] = "扩展名",
            ["sort.size"] = "大小",
            ["status.idle"] = "空闲。运行提取后，这里会自动刷新图片和视频预览。",
            ["status.extracting"] = "提取中，当前单 mipmap 上限：{0} MB",
            ["status.extractFinished"] = "提取完成，右侧预览已刷新。",
            ["status.infoFinished"] = "Info 执行完成，请查看日志输出。",
            ["status.running"] = "运行中：{0}",
            ["status.failed"] = "{0} 执行失败。",
            ["status.videoReady"] = "视频预览已就绪。",
            ["status.imageReady"] = "图片预览已就绪。",
            ["status.dragLoaded"] = "已载入拖拽项：{0}",
            ["status.copiedFile"] = "已复制文件：{0}",
            ["status.deletedFile"] = "已删除文件：{0}",
            ["status.deletedExtracted"] = "已删除提取结果，界面已恢复初始状态。",
            ["preview.placeholder"] = "提取完成后，这里会显示图片或视频预览。",
            ["preview.noFiles"] = "没有可预览文件",
            ["dialog.noPreviewFileTitle"] = "未选择文件",
            ["dialog.noPreviewFile"] = "请先在预览文件区选择一个文件。",
            ["dialog.invalidPathTitle"] = "路径无效",
            ["dialog.invalidExtractPath"] = "请输入要提取的文件或目录。",
            ["dialog.invalidInfoPath"] = "请输入要分析的文件或目录。",
            ["dialog.invalidConfigTitle"] = "配置无效",
            ["dialog.invalidMaxMipmap"] = "单 mipmap 上限请输入大于 0 的 MB 数值。",
            ["dialog.runFailedTitle"] = "执行失败",
            ["dialog.outputMissingTitle"] = "无法打开",
            ["dialog.outputMissing"] = "输出目录还不存在。",
            ["dialog.confirmDeleteFileTitle"] = "确认删除文件",
            ["dialog.confirmDeleteFile"] = "确定要删除这个提取文件吗？\n\n{0}",
            ["dialog.confirmDeleteExtractedTitle"] = "确认删除提取结果",
            ["dialog.confirmDeleteExtracted"] = "确定要删除当前输出目录及其中的所有提取结果吗？\n\n{0}",
            ["operation.extract"] = "提取",
            ["operation.info"] = "Info",
            ["app.startupFailedTitle"] = "RePKG GUI 启动失败",
            ["app.runtimeErrorTitle"] = "RePKG GUI 运行时错误"
        };

        private static readonly IReadOnlyDictionary<string, string> EnglishTexts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["window.title"] = "RePKG",
            ["hero.subtitle"] = "Extract Wallpaper Engine PKG files and preview assets on the right.",
            ["hero.dragHint"] = "Drop pkg / tex / folder",
            ["section.extract"] = "Extract",
            ["section.info"] = "Info",
            ["section.preview"] = "Preview",
            ["section.previewFiles"] = "Preview Files",
            ["section.log"] = "Log",
            ["label.inputPath"] = "Input file or folder",
            ["label.outputDirectory"] = "Output folder",
            ["label.ignoreExts"] = "Ignore extensions",
            ["label.onlyExts"] = "Keep only extensions",
            ["label.maxMipmap"] = "Single mipmap safety limit (MB)",
            ["label.maxMipmapHint"] = "Default is 1024 MB. Raise it only for very large video textures when needed.",
            ["label.infoSort"] = "Sort by",
            ["label.projectInfo"] = "project.json field",
            ["label.titleFilter"] = "Title filter",
            ["label.currentSelection"] = "Current selection",
            ["label.volume"] = "Volume",
            ["button.selectFile"] = "Select File",
            ["button.selectFolder"] = "Select Folder",
            ["button.clear"] = "Clear",
            ["button.selectOutput"] = "Select Output",
            ["button.openOutput"] = "Open Output",
            ["button.runExtract"] = "Extract",
            ["button.refreshPreview"] = "Refresh Preview",
            ["button.deleteExtracted"] = "Delete Output",
            ["button.runInfo"] = "Run Info",
            ["button.play"] = "Play",
            ["button.pause"] = "Pause",
            ["button.stop"] = "Stop",
            ["button.clearLog"] = "Clear",
            ["button.copyFile"] = "Copy File",
            ["button.openFile"] = "Open File",
            ["button.saveFile"] = "Save File",
            ["button.deleteFile"] = "Delete File",
            ["checkbox.texDirectory"] = "TEX directory mode",
            ["checkbox.singleDir"] = "Single output folder",
            ["checkbox.recursive"] = "Recursive subfolders",
            ["checkbox.copyProject"] = "Copy project files",
            ["checkbox.useName"] = "Use project title",
            ["checkbox.noTexConvert"] = "Skip TEX conversion",
            ["checkbox.overwrite"] = "Overwrite existing files",
            ["checkbox.infoSort"] = "Sort",
            ["checkbox.printEntries"] = "Print entries",
            ["sort.name"] = "Name",
            ["sort.extension"] = "Extension",
            ["sort.size"] = "Size",
            ["status.idle"] = "Idle. Run extract to refresh image and video previews here.",
            ["status.extracting"] = "Extracting. Current single mipmap limit: {0} MB",
            ["status.extractFinished"] = "Extract finished. Preview has been refreshed.",
            ["status.infoFinished"] = "Info finished. Check the log output.",
            ["status.running"] = "Running: {0}",
            ["status.failed"] = "{0} failed.",
            ["status.videoReady"] = "Video preview is ready.",
            ["status.imageReady"] = "Image preview is ready.",
            ["status.dragLoaded"] = "Loaded dropped item: {0}",
            ["status.copiedFile"] = "Copied file: {0}",
            ["status.deletedFile"] = "Deleted file: {0}",
            ["status.deletedExtracted"] = "Deleted extracted output and reset the UI.",
            ["preview.placeholder"] = "Extract assets to preview images or videos here.",
            ["preview.noFiles"] = "No previewable files",
            ["dialog.noPreviewFileTitle"] = "No file selected",
            ["dialog.noPreviewFile"] = "Select a file from the preview list first.",
            ["dialog.invalidPathTitle"] = "Invalid path",
            ["dialog.invalidExtractPath"] = "Please choose a file or folder to extract.",
            ["dialog.invalidInfoPath"] = "Please choose a file or folder to inspect.",
            ["dialog.invalidConfigTitle"] = "Invalid configuration",
            ["dialog.invalidMaxMipmap"] = "Enter a mipmap limit greater than 0 in MB.",
            ["dialog.runFailedTitle"] = "Execution failed",
            ["dialog.outputMissingTitle"] = "Cannot open output",
            ["dialog.outputMissing"] = "The output folder does not exist yet.",
            ["dialog.confirmDeleteFileTitle"] = "Delete file",
            ["dialog.confirmDeleteFile"] = "Delete this extracted file?\n\n{0}",
            ["dialog.confirmDeleteExtractedTitle"] = "Delete extracted output",
            ["dialog.confirmDeleteExtracted"] = "Delete the current output folder and all extracted files?\n\n{0}",
            ["operation.extract"] = "extract",
            ["operation.info"] = "info",
            ["app.startupFailedTitle"] = "RePKG GUI startup failed",
            ["app.runtimeErrorTitle"] = "RePKG GUI runtime error"
        };

        public static string Get(UiLanguage language, string key, params object[] args)
        {
            var texts = language == UiLanguage.English ? EnglishTexts : ChineseTexts;
            string value;
            if (!texts.TryGetValue(key, out value))
            {
                value = key;
            }

            return args != null && args.Length > 0
                ? string.Format(value, args)
                : value;
        }
    }
}
