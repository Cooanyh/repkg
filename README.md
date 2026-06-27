# RePKG

[中文](README.zh-CN.md) | **English**

RePKG is a Wallpaper Engine PKG unpacker and TEX converter written in C#.

This fork keeps the original CLI workflow and adds a modern WPF GUI for day-to-day extraction and preview work.

## Highlights

- Extract PKG files
- Convert TEX files to images
- Detect and export large video textures more reliably
- Preview extracted images and videos inside the GUI
- Switch the GUI between Chinese and English
- Ship a single-file GUI executable for easier distribution

## Binaries

- CLI: `RePKG.exe`
- GUI: `RePKG.GUI.exe`

The GUI build is a single executable and is intended for regular desktop use on Windows.

## GUI Features

- Drag and drop `pkg`, `tex`, or folders
- Default output folder is `output` under the input location
- Quoted paths are supported
- Adjustable single-mipmap safety limit
- Image and video preview pane
- Video controls: play, pause, stop, seek, volume
- Chinese / English UI switch

## CLI Commands

### `help`

Shows available commands. Use `help "extract"` and `help "info"` for details.

### `extract`

Extracts a PKG/TEX file, or extracts files from a folder.

```text
-o, --output          (Default: ./output) Output directory
-i, --ignoreexts      Don't extract files with specified extensions (comma separated)
-e, --onlyexts        Only extract files with specified extensions (comma separated)
-d, --debuginfo       Print debug info while extracting/decompiling
-t, --tex             Convert all TEX files into images from the input directory
-s, --singledir       Put all extracted files into one directory
-r, --recursive       Search subfolders recursively
-c, --copyproject     Copy project.json and preview.jpg beside PKG into output
-n, --usename         Use project title from project.json instead of id
--no-tex-convert      Skip TEX conversion while extracting PKG
--overwrite           Overwrite existing files
```

### `info`

Dumps PKG/TEX information.

```text
-s, --sort            Sort entries a-z
-b, --sortby          (Default: name) Sort by name, extension, or size
-t, --tex             Dump info for all TEX files from the input directory
-p, --projectinfo     Keys to dump from project.json (comma separated, `*` for all)
-e, --printentries    Print package entries
--title-filter        Filter by title
```

## Examples

Extract a PKG and convert TEX entries into images in a local output folder:

```text
repkg extract E:\Games\steamapps\workshop\content\123\scene.pkg
```

Build Wallpaper Engine project output from a folder:

```text
repkg extract -c E:\Games\steamapps\workshop\content\123
```

Only convert TEX entries to PNG and flatten the output:

```text
repkg extract -e tex -s -o .\output E:\Games\steamapps\workshop\content\123
```

Convert TEX files from a folder:

```text
repkg extract -t -s E:\path\to\dir\with\tex\files
```

## Notes

- The GUI targets Windows desktop usage.
- This fork keeps compatibility with the original CLI behavior where possible.
