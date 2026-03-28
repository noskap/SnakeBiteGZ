# SnakeBiteGZ

SnakeBiteGZ is a mod manager/launcher for Metal Gear Solid V: Ground Zeroes (Steam). This mod wouldn't be possible without all the hardworking modders who have been modding this game and it's bigger brother TPP for the last decade.

Based on the fork at: https://github.com/TinManTex/SnakeBite

https://github.com/noskap/SnakeBiteGZ

### Future work
- Fork GzsLib and unify classes between makebite and snakebite
- Fix RevertChanges.MGSVPreset
- Fix Start game not working properly
- Unit tests for uninstallation
- Add more entries to the QAR Dictionary
- MakeBite: Add support for creating mods by diffing a modified unpacked archive against an original vanilla unpacked archive.
- PFTXS support

### Key Features
- **Full support for MGSV:GZ**: Supports unpacking repacking of g0s archives, as well as .mgsvgz mod files.
- **XML Merging**: Supports merging custom entries into `01.xml` (for `data_01.g0s`) and `02.xml` (for `data_02.g0s`). This is particularly useful for custom texture mods that need to register new file paths.

### How MakeBite Works
MakeBite for GZ has been updated to use the internal `GzsLib` library. Unlike the TPP version which might repack archives, this version generates a mod file that tells SnakeBiteGZ to inject file replacements directly into the game's `data_01.g0s` or `data_02.g0s` archives without fully repacking nested FPKs.

### Differences from SnakeBite (TPP)
- **Archive Targets**: Operates on `data_01.g0s` and `data_02.g0s` instead of TPP's dat files.
- **No Chunk7/Texture7**: Migrating files to `chunk7.dat` has been removed as it is specific to TPP.
- **G0S Support**: Native handling of Ground Zeroes' specific archive format.

### Why does this exist?
Because I felt like it, and honestly, I wanted a cleaner way to be able to share GZ mods with my friends without doing a lot of horrible manual unpacking and file managing, even if it means I had to strip back features to make it worth with the older archive system.

### GZ has already been ported to TPP. Isn't this pointless?
Maybe to you, but to me, Ground Zeroes is this special little toybox game I've always loved tinkering with. I also don't want to see it fall into obscurity and be forgotten. The modding scene for GZ is basically non-existent. If this Mod Manager gets even just one more person into GZ modding, I'll be satisfied.

I must also stress, this is still very much a work-in-progress. There are many things I would like to get done before I even call this being close to finished. If you do download it for yourself, I'd love to hear about your experience. Thanks

## Getting started with SnakeBiteGZ

Before running makebiteGZ make sure the `data_01.g0s` and `data_02.g0s` in `\MgsGroundZeroes\` are unmodified.
Use the validate game cache option via steam if necessary.

When you first run the application, you will be greeted by the setup wizard. This will walk you through the steps required to run SnakeBiteGZ.

During the setup process you will be prompted to
- Select your MGSVGZ installation directory
- Create a backup of the game data

## Mods and Mod Files
Mods can be installed and uninstalled by selecting **MODS** from the main window. Additionally, you can temporarily disable/enable all mods by clicking the toggle switch next to **MODS**.

Click "Install .MGSVGZ" from the mod manager window and select the mod you wish to install.

## Troubleshooting
Installing Microsoft .Net 4.6.1:
https://www.microsoft.com/en-us/download/details.aspx?id=49981

When using the revalidate option it will likely pop up the steam process in the background, make sure you wait till it's finished.

To manually revalidate the files through steam:
1. Right click on Metal Gear Solid V: Ground Zeroes in Steam
2. Choose properties from the right click menu
3. Click the Local Files tab
4. Click Verify Integrity of Game Cache.
5. Wait till it says it's complete.

## Mod Developers
Use MakeBiteGZ (included) to create `.MGSVGZ` mod files compatible with SnakeBiteGZ.

## Running Unit Tests
If you intend to contribute to the codebase, you can execute the unit tests by running `build.bat` and selecting `4. Run Tests`.

**Note for Developers:** Game archives are excluded from this repository due to size and copyright constraints. To run the test suite locally, you must extract a completely clean, vanilla copy of `data_02.g0s` (approx 1.5GB) from your Steam installation and place it at:
`[Repository Root]\gz\data_02.g0s`

## Found a bug?
Please submit a bug report to GitHub with as much detail as possible. Please include `log.txt`, which is accessed by double clicking the version in the main window, or found in your SnakeBiteGZ install directory (default `%LocalAppData%\SnakeBite`). Warning: the logfile is reset every time you launch SnakeBite so please save it immediately after the application crashes.