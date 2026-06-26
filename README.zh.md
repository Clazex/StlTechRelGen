# StlTechRelGen

[English](README.md) | **中文**

> 群星科技关联生成器

本工具使用 [CWTools](https://github.com/cwtools/cwtools) 处理群星科技数据（原版及模组），并生成 `.yml` 本地化文件。当游戏通过模组加载这些文件后，将为每项科技的描述补充以下信息：

- 科技领域、层级和类别
- 稀有、危险或可重复科技标识
- 前置科技
- 解锁的后续科技

## 安装

本应用程序需要 **.NET Runtime 10** 才能运行。

1. **安装 .NET Runtime**

   打开下载页面：
   <https://dotnet.microsoft.com/download/dotnet/10.0/runtime>

   选择对应操作系统的选项卡并按说明操作。

   > 对于 Windows，建议选择 **桌面运行时**。

2. **下载工具**

   前往[最新发布页面](https://github.com/Clazex/stl-tech-rel-gen/releases/latest)
   下载对应你平台的压缩包：

   - **`win-x64`**，适用于 Windows x64
   - **`linux-x64`**，适用于 Linux x64
   - **`osx-arm64`**，适用于 MacOS ARM64

3. **解压**

   将下载的压缩包解压到任意文件夹。

## 使用方法

### 前置条件

- Paradox Launcher 和群星必须至少在此计算机上启动过一次，以初始化必要文件。
- **运行工具前请关闭 Paradox Launcher**——启动器可能会锁定本工具所需的文件。
- 对于 Irony Mod Manager 用户：请先应用你的模组集合，然后在设置中选择 `IronyModManager` 播放集。

### 目标模组设置

建议创建一个专用的目标模组来存放生成的文件。工具会将输出写入此模组，以便游戏加载生成的文件。
生成过程中会忽略目标模组中的现有内容，以避免冲突。

1. 在启动器中创建一个模组。如需帮助，请参阅 [Wiki](https://stellaris.paradoxwikis.com/Modding#Creating_a_mod)。
2. 将其放在所需播放集的末尾。

### 运行工具

1. **启动应用程序**，双击解压文件夹中的可执行文件。

2. **首次设置**——应用程序会提示输入两个路径：
   - **游戏路径**——群星安装目录（包含 `stellaris` 可执行文件）。
   - **文档路径**——群星用户数据目录，即模组文件夹的上一级目录（参见 [Wiki](https://stellaris.paradoxwikis.com/Modding#Mod_folder_location)）。

   这些路径通常会自动检测。你可以通过输入、粘贴（右键单击）或将文件夹拖入窗口来指定路径。

3. **选择播放集：**
   - 从列表中选择一个模组播放集，或
   - 按 `Esc` 键使用原版游戏。

4. **选择输出目标：**
   - **播放集模式**：选择用于接收生成文件的目标模组。
   - **原版模式**：输入输出文件夹路径（必须是包含 `descriptor.mod` 的模组根目录）。

5. **保存设置**（可选），以便后续运行时重复使用。

6. **等待生成完成**。对于原版数据，通常需要不到 1 分钟。

> 如果启动时不存在配置文件（通常为 `StlTechRelGen.toml`），工具将启动交互式设置向导。如需重新运行设置向导，只需删除配置文件并重新启动应用程序即可。

### 输出

生成的文件命名为 `techrel_l_{language}.yml`（例如 `techrel_l_english.yml`、`techrel_l_simp_chinese.yml`），应位于目标模组的
`localisation/replace/` 目录中。

辅助文件（`!techrel_l_{language}.yml`）会与主要输出文件一起写入，用于定义生成的描述中引用的小片段。

你可以自由地将生成的模组分享或上传至 Steam 创意工坊。

## 命令行界面

本工具支持命令行参数，方便脚本编写和自动化操作。
使用命令行参数时，所有交互式提示将尽可能跳过。
使用 `-h` 或 `--help` 查看完整选项列表。

```bash
# 播放集模式示例：
StlTechRelGen.exe -p "My Playset" -m "Tech Relations"

# 原版模式示例：
StlTechRelGen.exe -p "" -m "/path/to/output/folder"
```

## 配置文件

本工具将设置保存在可执行文件旁边的 TOML 配置文件中（通常为 `StlTechRelGen.toml`）。

```toml
yesmen = false                   # 自动对所有确认提示回答"是"
override_language = "en"         # 覆盖界面语言（"en" 或 "zh"）；省略则自动检测
suppresses_cwtools_errors = true # 静默 CWTools 错误信息

[game]
game_path = "<...>/Stellaris"
document_path = "<...>/Stellaris"

[playset] # 可选
name = "My Playset"              # 空字符串表示原版游戏模式
target = "Tech Relations"        # 播放集模式：目标模组名称；原版模式：输出目录路径

[update]
check_update = true              # 启用更新检查
github_api_token = "ghp_..."     # GitHub 令牌，用于提高 API 速率限制
# 由于仓库是公开的，令牌无需任何权限。
# 请注意，令牌以明文形式存储且无保护措施，请自行承担风险使用。
# 建议为此用途创建一个具有最小权限的单独令牌。
```
