# StlTechRelGen

**English** | [中文](README.zh.md)

> Stellaris Tech Relations Generator

This tool processes Stellaris technology data (both vanilla and modded) using
[CWTools](https://github.com/cwtools/cwtools) and generates `.yml` localization
files. When loaded by the game via a mod, these files augment each technology's
description with:

- Tech area, tier, and category
- Rare, dangerous, or repeatable indicators
- Prerequisite technologies
- Unlocked technologies

## Installation

This application requires the **.NET Runtime 10** to run.

1. **Install .NET Runtime**

   Open the download page:
   <https://dotnet.microsoft.com/download/dotnet/10.0/runtime>

   Select the tab for your operating system and follow the instructions.

   > For Windows, selecting the **Desktop runtime** is recommended.

2. **Download the tool**

   Go to the [latest release page](https://github.com/Clazex/stl-tech-rel-gen/releases/latest)
   and download the archive for your platform:

   - **`win-x64`**, for Windows x64
   - **`linux-x64`**, for Linux x64
   - **`osx-arm64`**, for MacOS ARM64

3. **Extract**

   Extract the downloaded archive to a folder of your choice.

## Usage

### Prerequisites

- The Paradox Launcher and Stellaris must have been launched at least once on this
  computer to initialize necessary files.
- **Close Paradox Launcher** before running the tool — the launcher may lock files needed by this tool.
- For Irony Mod Manager users: apply your collection first, then select the `IronyModManager` playset during setup.

### Target Mod Setup

Creating a dedicated target mod is recommended to keep generated files organized.
The tool writes its output into this mod so the game can load the generated
files.
Existing content in the target mod is ignored during generation to
prevent conflicts.

1. Create a mod in the launcher. See the
   [wiki](https://stellaris.paradoxwikis.com/Modding#Creating_a_mod) if you need help.
2. Place it at the end of your desired playset.

### Running the Tool

1. **Start the application** by double-clicking the executable inside the extracted folder.

2. **First-time setup** — the application prompts for two paths:
   - **Game path** — the Stellaris installation directory (contains the `stellaris` executable).
   - **Document path** — the Stellaris user data directory, one level above the
     mod folder (see the
     [wiki](https://stellaris.paradoxwikis.com/Modding#Mod_folder_location)).

   These paths are typically auto-detected. You can enter them by typing, pasting
   (right-click), or dragging a folder into the window.

3. **Select a playset:**
   - Choose a modded playset from the list, or
   - Press `Esc` to use the vanilla game.

4. **Select output destination:**
   - **Playset mode**: choose the target mod to receive the generated files.
   - **Vanilla mode**: enter an output folder path (must be a mod root folder containing `descriptor.mod`).

5. **Save settings** (optional) to reuse them on subsequent runs.

6. **Wait for generation** to complete. For vanilla data, this usually takes less than a minute.

> If no configuration file (typically `StlTechRelGen.toml`) exists on startup,
> the tool launches an interactive setup wizard. To run the setup wizard again,
> simply delete the configuration file and restart the application.

### Output

Generated files are named `techrel_l_{language}.yml` (e.g.,
`techrel_l_english.yml`, `techrel_l_simp_chinese.yml`) and should be placed in
the `localisation/replace/` directory of the target mod.

Auxiliary files (`!techrel_l_{language}.yml`) are written alongside the main
output, defining small fragments referenced in the generated descriptions.

You are free to share or upload the generated mod to the Steam Workshop.

## Commandline Interface

The tool supports command-line arguments for scripting and automation.
When using CLI arguments, all interactive prompts are skipped if possible.
Use `-h` or `--help` to see the full list of options.

```bash
# Playset mode example:
StlTechRelGen.exe -p "My Playset" -m "Tech Relations"

# Vanilla mode example:
StlTechRelGen.exe -p "" -m "/path/to/output/folder"
```

## Configuration File

The tool saves its settings in a TOML configuration file alongside the executable (typically `StlTechRelGen.toml`).

```toml
yesmen = false                   # Auto-answers "yes" to confirmation prompts
override_language = "en"         # Override UI language ("en" or "zh"); omit to auto-detect
suppresses_cwtools_errors = true # Silence CWTools error messages

[game]
game_path = "<...>/Stellaris"
document_path = "<...>/Stellaris"

[playset] # Optional
name = "My Playset"              # Empty string means vanilla game mode
target = "Tech Relations"        # In playset mode: target mod name; in vanilla mode: output directory path

[update]
check_update = true              # Enables update checker
github_api_token = "ghp_..."     # GitHub token for higher API rate limits
# No permission is required for the token since the repository is public.
# Be aware that the token is stored in plain text without protection, so use it at your own risk.
# It is recommended to create a separate token with minimal permissions for this purpose.
```
