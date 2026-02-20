# SnakeBiteGZ
SnakeBiteGZ is a mod manager/launcher for Metal Gear Solid V: Ground Zeroes (Steam). This mod wouldn't be possible without all the hardworking modders who have been modding this game for years. 

Based on the fork at: https://github.com/TinManTex/SnakeBite

https://github.com/noskap/SnakeBiteGZ

### Future work
- Fork GzsLib and unify classes between makebite and snakebite
- Fix RevertChanges.MGSVPreset
- Fix Start game not working properly
- Unit tests using md5 checksums
- Add more entries to the QAR Dictionary

### Key Features
- **Full support for MGSV:GZ**: Supports unpacking repacking of g0s archives, as well as .mgsvgz mod files.
- **XML Merging**: Supports merging custom entries into `01.xml` (for `data_01.g0s`) and `02.xml` (for `data_02.g0s`). This is particularly useful for custom texture mods that need to register new file paths.

### How MakeBite Works
MakeBite for GZ has been updated to use the internal `GzsLib` library. Unlike the TPP version which might repack archives, this version generates a mod file that tells SnakeBiteGZ to inject file replacements directly into the game's `data_01.g0s` or `data_02.g0s` archives without fully repacking nested FPKs.

### Differences from SnakeBite (TPP)
- **Archive Targets**: Operates on `data_01.g0s` and `data_02.g0s` instead of TPP's `00.dat` and `01.dat`.
- **No Chunk7/Texture7**: The "Archive Expand" logic (migrating files to `chunk7.dat`) has been removed as it is specific to TPP.
- **G0S Support**: Native handling of Ground Zeroes' specific archive format.

### Why does this exist?
Because I felt like it, and honestly, I wanted a cleaner way to be able to share GZ mods with my friends without doing a lot of horrible manual unpacking and file managing, even if it means I had to strip back features to make it worth with the older archive system.

### GZ has already been ported to TPP. Isn't this pointless?
Maybe to you, but to me, Ground Zeroes is this special little toybox game I've always loved tinkering with. I also don't want to see it fall into obscurity and be forgotten. The modding scene for GZ is basically non-existant. If this Mod Manager gets even just one more person into GZ modding, I'll be satisfied.

## Getting started with SnakeBiteGZ
Before running makebite make sure the data_01.g0s and data_02.g0s in \MgsGroundZeroes\ are unmodified.
Use the validate game cache option via steam if nessesary.

When you first run the application, you will be greeted by the setup wizard. This will walk you through the steps required to run SnakeBiteGZ.

During the setup process you will be prompted to

1. Select your MGSVGZ installation directory
2. Create a backup of the game data

## Mods and Mod Files
Mods can be installed and uninstalled by selecting **MODS** from the main window. Additionally, you can temporarily disable/enable all mods by clicking the toggle switch next to **MODS**.

### If your mod is a **.MGSVGZ**:

Click "Install .MGSVGZ" from the mod manager window and select the mod you wish to install.

## Mod Preset Files
A 'Mod Preset' is a collection of mods which can be saved and loaded with SnakeBiteGZ. Saving a Preset will pack your current modded game data into a .MGSVPreset file. Loading a Preset will simply replace your game data with the files stored in the .MGSVPreset file. Presets are a fast and simple method of organizing your favorite mods or trying new mod combinations. You can also utilize Presets as restore checkpoints if SnakeBiteGZ encounters a serious error or your game data becomes corrupted. 

# Troubleshooting
For those with issues with SnakeBiteGZ try:

Do not use the Install .ZIP option, instead extract the mods .mgsvgz file that's in the mods .zip and use Install .MGSVGZ

Installing Microsoft .Net 4.6.1:
https://www.microsoft.com/en-us/download/details.aspx?id=49981

If you have a warning about permissions try right clicking SnakeBiteGZ shortcut and choosing Run as administrator
Or try reinstalling SnakeBiteGZ to a different folder than it's default.

When using the revalidate option it will likely pop up the steam process in the background, make sure you wait till it's finished.

To manually revalidate the files through steam:
Right click on Metal Gear Solid V: Ground Zeroes in Steam
Choose properties from the right click menu
Click the Local Files tab
Click Verify Integrity of Game Cache.
Wait till it says it's complete.

If you get "The selected mod conflicts with existing MGSV system files" warning then SnakeBiteGZ has likely messed up it's data xml and added mod files to it's default files list.
The only solution is to hit Restore Original Game Files in the SnakeBiteGZ settings. Verify MGSVs game cache (through steam, not SnakeBiteGZ) to make sure everything is default and try again.
While the disable compatability check setting will bypass this, it is likely to cause problems with the game.

SnakeBiteGZ prints information to Log.txt in its install folder.
Check this before you close SnakeBiteGZ (since it's cleared on next SnakeBiteGZ start) to see if there's any error messages. 

# Mod Developers

Use MakeBite (included) to create .MGSVGZ mod files compatible with SnakeBiteGZ

For information regarding using makebite to create mod files, please see here: https://github.com/topher-au/SnakeBite/wiki/Using-MakeBite

In addition to this, files inside a folder named GameDir will be installed to the MgsGroundZeroes folder.

# Command-line args
Makebite just takes the input path (that you'd normally browse to) and outputs mod.mgsv there.
This means makebite will create a mgsv by drag-and-drop a valid folder onto MakeBiteGZ.exe or using shortcut in windows send-to menu.
Makebite will fill the info from a metadata.xml in the input folder (previously generated by the usual makebite ui).
Also you can throw a readme.txt in the input folder and it will overwrite the metadata description.

Snakebites args:

-completeuninstall
(as the only arg)
Same as settings > Restore original game files button.

-i <path to .mgsvgz file>
install - the -i can be ommitted, path can also be a folder of .mgsvs, and any other paths passed in as args will also be processed.
This means you can install by drag-and-drop onto snakebite.exe or using shortcut in windows send-to menu.

-u <name of mod>
uninstall an installed mod (name matching the Name field in makebite/metadata.xml/the snakebite installed mods list.

-c
bypass conflict checks, I think necessary because makebite doesn't use the snakebite version value from metadata.xml when building from command line.

-x
close snakebite when done

-d
resets snakebites dat hash, snakebite uses this to detect if the 00.dat has changed, and usually throws up ui saying it's doing so, this option just silently does it.
this was to smooth over my 'build tool' process - which basically just copies over stuff from my project folder then runs makebite/snakebite by command line - rather than wait for uninstall of previous version it copies over a 'snakebite clean' 00.dat/ie the 2kb snakebite installed but nothing else 00.dat, then runs snakebite with -i -d -c -x

# Found a bug?

Please submit a bug report to GitHub with as much detail as possible. Please include log.txt, which is accessed by double clicking the version in the main window, or found in your SnakeBite install directory (default %LocalAppData%\SnakeBite). Warning: the logfile is reset every time you launch SnakeBite so please save it immediately after the application crashes.