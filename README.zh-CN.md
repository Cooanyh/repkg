# RePKG

**中文** | [English](README.md)

RePKG 是一个使用 C# 编写的 Wallpaper Engine PKG 解包和 TEX 转换工具。

这个分支在保留原始 CLI 工作流的基础上，新增了一个更适合日常使用的现代化 WPF GUI。

## 主要特性

- 提取 PKG 文件
- 将 TEX 转换为图片
- 更稳定地识别并导出大体积视频纹理
- 在 GUI 中直接预览提取后的图片和视频
- GUI 支持中英文切换
- 提供单文件 GUI 可执行程序，分发更方便

## 可执行文件

- CLI：`RePKG.exe`
- GUI：`RePKG.GUI.exe`

GUI 版本为单文件可执行程序，适合 Windows 桌面环境直接使用。

## GUI 功能

- 支持拖入 `pkg`、`tex` 或文件夹
- 默认输出目录为输入位置下的 `output`
- 支持带引号的路径
- 可调整单 mipmap 安全上限
- 右侧图片 / 视频预览区
- 视频控制：播放、暂停、停止、进度拖动、音量
- 界面支持中文 / English 切换

## CLI 命令

### `help`

显示可用命令，可使用 `help "extract"` 和 `help "info"` 查看详细参数。

### `extract`

提取指定 PKG/TEX 文件，或提取目录中的文件。

```text
-o, --output          (默认: ./output) 输出目录
-i, --ignoreexts      不提取指定扩展名的文件（逗号分隔）
-e, --onlyexts        仅提取指定扩展名的文件（逗号分隔）
-d, --debuginfo       提取/反编译时输出调试信息
-t, --tex             将输入目录中的 TEX 文件批量转换为图片
-s, --singledir       将提取结果放入单一目录
-r, --recursive       递归搜索子目录
-c, --copyproject     复制 PKG 同目录下的 project.json 和 preview.jpg 到输出目录
-n, --usename         使用 project.json 中的标题作为项目目录名，而不是 id
--no-tex-convert      提取 PKG 时不转换 TEX
--overwrite           覆盖已有文件
```

### `info`

输出 PKG/TEX 信息。

```text
-s, --sort            按 a-z 排序条目
-b, --sortby          (默认: name) 按 name、extension、size 排序
-t, --tex             输出输入目录下所有 TEX 文件的信息
-p, --projectinfo     输出 project.json 指定字段（逗号分隔，`*` 表示全部）
-e, --printentries    打印包内条目
--title-filter        按标题过滤
```

## 使用示例

提取 PKG，并将其中的 TEX 条目转换为图片，输出到本地 `output` 目录：

```text
repkg extract E:\Games\steamapps\workshop\content\123\scene.pkg
```

从目录中批量提取，并生成 Wallpaper Engine 项目输出：

```text
repkg extract -c E:\Games\steamapps\workshop\content\123
```

仅将 TEX 条目转换为 PNG，并将输出扁平化到单目录：

```text
repkg extract -e tex -s -o .\output E:\Games\steamapps\workshop\content\123
```

批量转换目录中的 TEX 文件：

```text
repkg extract -t -s E:\path\to\dir\with\tex\files
```

## 说明

- GUI 主要面向 Windows 桌面使用。
- 这个分支尽量保持与原始 CLI 行为兼容。
