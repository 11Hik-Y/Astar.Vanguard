# Third-party notices

## MiyakoCarryService

Astar Vanguard is an independently maintained modified derivative of:

- Project: MiyakoCarryService
- Original author / maintainer: 姫様の夢 / `himesamanoyume`
- Repository: https://github.com/himesamanoyume/MiyakoCarryService
- Baseline branch: `4.0.X`
- Fixed baseline commit: `8a6297c155accd1d04f2d211b701f1a724444c00`
- License: Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International

Astar Vanguard substantially changes the original project, including branding,
operator identity generation and persistence, trader presentation, command-center
UI, AI attribute handling, network/update behavior, build layout, and Fika
integration behavior.

Original portions remain copyright their respective authors and contributors.
Astar's new original code, redesigns, documentation, branding, and systems are
copyright Astar. The combined derivative is distributed under CC BY-NC-SA 4.0.

Astar Vanguard is not affiliated with, sponsored by, endorsed by, or an official
continuation of MiyakoCarryService.

## Plain Craft Launcher (PCL)

The command-center presentation layer uses Plain Craft Launcher only as a visual
and interaction-language reference.

- Project: Plain Craft Launcher
- Repository: https://github.com/Meloong-Git/PCL
- Author attribution: 龙腾猫跃 / PCL contributors
- Reference revision used during local research: `8f7686457443e790670ee22157d98eee8ca2e20c`
- Upstream terms: `LICENCE` in the PCL repository

No PCL source code, XAML, binaries, fonts, images, or runtime components are
linked, packaged, imported, or required. The local Unity uGUI implementation is
independently authored. The excluded `reference/PCL` checkout is development-only
research material and is not part of builds or releases.

## Runtime and build dependencies

Astar Vanguard builds against and/or interoperates with SPT, BepInEx, HarmonyX,
Fika and DrakiaXYZ BigBrain. Their binaries are not part of this source repository;
local compile-time copies live under the ignored `Ref/` directory.

These projects remain subject to their own licenses and copyrights. Astar
Vanguard does not claim ownership of those third-party projects or binaries.
