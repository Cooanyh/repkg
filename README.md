# RePKG

用 C# 编写的 Wallpaper Engine PKG 解包器 / TEX 转换器。

PKG 和 TEX 格式由我进行逆向工程实现。

欢迎反馈错误。

# Features

* 提取 PKG 文件
* 将 PKG 转换为 Wallpaper Engine 项目
* 将 TEX 转换为图片
* 导出 PKG/TEX 信息

### Commands

* help - 显示这些命令，使用 `help "extract"` 和 `help "info"` 查看对应选项
* extract - 提取指定的 PKG/TEX 文件，或文件夹中的文件

```
-o, --output          (默认: ./output) 输出目录
-i, --ignoreexts      不提取指定扩展名的文件（用英文逗号 "," 分隔）
-e, --onlyexts        只提取指定扩展名的文件（用英文逗号 "," 分隔）
-d, --debuginfo       在提取/解压时打印调试信息
-t, --tex             将指定输入目录中的所有 TEX 文件转换为图片
-s, --singledir       是否将所有解压文件放入同一目录，而不是按原路径结构保存
-r, --recursive       递归搜索指定目录下的所有子文件夹
-c, --copyproject     将 PKG 旁边的 project.json 和 preview.jpg 复制到输出目录
-n, --usename         使用 project.json 中的名称作为项目子文件夹名称，而不是 id
--no-tex-convert      提取 PKG 时不将 TEX 文件转换为图片
--overwrite           覆盖所有已存在文件
```

* info - 导出 PKG/TEX 信息

```
-s, --sort             按 a-z 排序
-b, --sortby           (默认: name) 排序方式...（可选: name, extension, size）
-t, --tex              导出指定目录中所有 TEX 文件的信息
-p, --projectinfo      要从 project.json 导出的键（用逗号分隔，* 表示全部）
-e, --printentries     打印包内条目
--title-filter         标题过滤器
```

### Examples

简单地提取 PKG，并将 TEX 条目转换为图片到当前目录下创建的 output 文件夹中

```
repkg extract E:\Games\steamapps\workshop\content\123\scene.pkg
```

在指定目录的子文件夹中查找 PKG 文件，并在 output 目录中将其转换为 Wallpaper Engine 项目

```
repkg extract -c E:\Games\steamapps\workshop\content\123
```

在指定目录的子文件夹中查找 PKG 文件，并仅将 TEX 条目转换为 png，然后放入 ./output，同时忽略其在 PKG 中的路径：

```
repkg extract -e tex -s -o ./output E:\Games\steamapps\workshop\content\123
```

将某个文件夹中的所有 TEX 文件转换为图片

```
repkg extract -t -s E:\path\to\dir\with\tex\files
```
