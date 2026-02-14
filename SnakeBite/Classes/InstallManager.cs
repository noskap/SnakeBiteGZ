using GzsTool.Core.Fpk;
using GzsTool.Core.Qar;
using ICSharpCode.SharpZipLib.Zip;
using SnakeBite.GzsTool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace SnakeBite
{
    internal static class InstallManager
    {
        public static bool InstallMods(List<string> ModFiles, bool skipCleanup = false)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Debug.LogLine("[Install] Start", Debug.LogLevel.Basic);
            ModManager.ClearBuildFiles(GamePaths.OnePath, GamePaths.chunk0Path, GamePaths.SnakeBiteSettings, GamePaths.SavePresetPath); // deletes any leftover sb_build files that might still be in the directory (ie from a mid-process shutdown) 
            ModManager.ClearSBGameDir(); // deletes the game directory sb_build
            ModManager.CleanupFolders(); // deletes the work folders which contain extracted files from 00/01

            if (Properties.Settings.Default.AutosaveRevertPreset == true)
            {
                PresetManager.SavePreset(GamePaths.SavePresetPath + GamePaths.build_ext); // creates a backup preset file sb_build
            }
            else
            {
                Debug.LogLine("[Install] Skipping RevertChanges.GZPreset Save", Debug.LogLevel.Basic);
            }
            File.Copy(GamePaths.SnakeBiteSettings, GamePaths.SnakeBiteSettings + GamePaths.build_ext, true); // creates a settings sb_build

            GzsLib.LoadDictionaries();
            List<ModEntry> installEntryList = new List<ModEntry>();
            foreach(string modFile in ModFiles) installEntryList.Add(Tools.ReadMetaData(modFile));


            // GZ: Swapping strict 00/01 logic for generic list based logic or specific data_02 support
            // data_00 is ignored (WMV). data_01 (01.dat) and data_02 (chunk0) are supported.

            List<string> oneFiles = new List<string>(); // data_01
            List<string> twoFiles = new List<string>(); // data_02

            bool hasData01 = ModManager.hasQarZeroFiles(installEntryList) || ModManager.foundLooseFtexs(installEntryList); // Reusing check but applying to data_01/02
            // Actually hasQarZeroFiles checked for non-ftex files.
            // In our mapping: non-ftex -> data_02. ftex -> data_01.
            
            bool hasData02 = ModManager.hasQarZeroFiles(installEntryList); // non-ftex files go to data_02 now

            if (hasData02)
            {
                 Debug.LogLine("[Install] Extracting data_02.g0s", Debug.LogLevel.Basic);
                 twoFiles = GzsLib.ExtractArchive<QarFile>(GamePaths.chunk0Path, "_working2");
            }

            if (hasData01)
            {
                Debug.LogLine("[Install] Extracting data_01.g0s", Debug.LogLevel.Basic);
                oneFiles = GzsLib.ExtractArchive<QarFile>(GamePaths.OnePath, "_working1");
            }

            SettingsManager SBBuildManager = new SettingsManager(GamePaths.SnakeBiteSettings + GamePaths.build_ext);
            var gameData = SBBuildManager.GetGameData();
            ModManager.ValidateGameData(ref gameData);

            var twoFilesHashSet = new HashSet<string>(twoFiles);

            Debug.LogLine("[Install] Building gameFiles lists", Debug.LogLevel.Basic);
            var baseGameFiles = GzsLib.ReadBaseData();
            var allQarGameFiles = new List<Dictionary<ulong, GameFile>>();
            allQarGameFiles.AddRange(baseGameFiles);


            try
            {
                ModManager.PrepGameDirFiles();
                List<string> pullFromVanillas; List<string> pullFromMods; Dictionary<string, bool> pathUpdatesExist;

                Debug.LogLine("[Install] Writing FPK data to Settings", Debug.LogLevel.Basic);
                AddToSettingsFpk(installEntryList, SBBuildManager, allQarGameFiles, out pullFromVanillas, out pullFromMods, out pathUpdatesExist);
                InstallMods(ModFiles, SBBuildManager, pullFromVanillas, pullFromMods, ref twoFilesHashSet, ref oneFiles, pathUpdatesExist);

                if (hasData02)
                {
                    twoFiles = twoFilesHashSet.ToList();
                    twoFiles.Sort();
                    GzsLib.WriteQarArchive(GamePaths.chunk0Path + GamePaths.build_ext, "_working2", twoFiles, GzsLib.chunk0Flags);
                }
                if (hasData01)
                {
                    oneFiles.Sort();
                    GzsLib.WriteQarArchive(GamePaths.OnePath + GamePaths.build_ext, "_working1", oneFiles, GzsLib.oneFlags);
                }

                ModManager.PromoteGameDirFiles();
                ModManager.PromoteBuildFiles(GamePaths.ZeroPath, GamePaths.OnePath, GamePaths.chunk0Path, GamePaths.SnakeBiteSettings, GamePaths.SavePresetPath);

                if (!skipCleanup)
                {
                    ModManager.CleanupFolders();
                    ModManager.ClearSBGameDir();
                }

                stopwatch.Stop();
                Debug.LogLine(String.Format("[Install] Installation finished in {0} ms", stopwatch.ElapsedMilliseconds), Debug.LogLevel.Basic);
                return true;
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                Debug.LogLine(String.Format("[Install] Installation failed at {0} ms", stopwatch.ElapsedMilliseconds), Debug.LogLevel.Basic);
                Debug.LogLine("[Install] Exception: " + e, Debug.LogLevel.Basic);
                MessageBox.Show("An error has occurred during the installation process and SnakeBite could not install the selected mod(s).\nException: " + e, "Mod(s) could not be installed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                ModManager.ClearBuildFiles(GamePaths.ZeroPath, GamePaths.OnePath, GamePaths.chunk0Path, GamePaths.SnakeBiteSettings, GamePaths.SavePresetPath);
                ModManager.CleanupFolders();

                bool restoreRetry = false;
                do
                {
                    try
                    {
                        ModManager.RestoreBackupGameDir(SBBuildManager);
                    }
                    catch (Exception f)
                    {
                        Debug.LogLine("[Uninstall] Exception: " + f, Debug.LogLevel.Basic);
                        restoreRetry = DialogResult.Retry == MessageBox.Show("SnakeBite could not restore Game Directory mod files due to the following exception: {f} \nWould you like to retry?", "Exception Occurred", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    }
                } while (restoreRetry);

                ModManager.ClearSBGameDir();
                return false;
            }
        }

        /// <summary>
        /// Merges the new mod files with the existing modded files and vanilla game files.
        /// </summary>
        private static void InstallMods(List<string> modFilePaths, SettingsManager manager, List<string> pullFromVanillas, List<string> pullFromMods, ref HashSet<string> twoFiles, ref List<string> oneFilesList, Dictionary<string, bool> pathUpdatesExist)
        {
            //Assumption: modded packs have already been extracted to _working2 directory - qarEntryEditList
            //Assumption: vanilla packs have already been extracted to _gameFpk directory 
            FastZip unzipper = new FastZip();
            GameData gameData = manager.GetGameData();

            bool isAddingWMV = false; //ZIP: WMV Support
            foreach (string modFilePath in modFilePaths) 
            {
                Debug.LogLine(String.Format("[Install] Installation started: {0}", Path.GetFileName(modFilePath)), Debug.LogLevel.Basic);
                
                Debug.LogLine(String.Format("[Install] Unzipping mod .mgsv ({0} KB)", Tools.GetFileSizeKB(modFilePath)), Debug.LogLevel.Basic);
                unzipper.ExtractZip(modFilePath, "_extr", "(.*?)");

                Debug.LogLine("[Install] Load mod metadata", Debug.LogLevel.Basic);
                ModEntry extractedModEntry = new ModEntry("_extr\\metadata.xml");
                if (pathUpdatesExist[extractedModEntry.Name])
                {
                    Debug.LogLine(string.Format("[Install] Checking for Qar path updates: {0}", extractedModEntry.Name), Debug.LogLevel.Basic);
                    foreach (ModQarEntry modQar in extractedModEntry.ModQarEntries.Where(entry => !entry.FilePath.StartsWith("/Assets/")))
                    {
                        string unhashedName = HashingExtended.UpdateName(modQar.FilePath);
                        if (unhashedName != null)
                        {
                            Debug.LogLine(string.Format("[Install] Update successful: {0} -> {1}", modQar.FilePath, unhashedName), Debug.LogLevel.Basic);

                            string workingOldPath = Path.Combine("_extr", Tools.ToWinPath(modQar.FilePath));
                            string workingNewPath = Path.Combine("_extr", Tools.ToWinPath(unhashedName));
                            if (!Directory.Exists(Path.GetDirectoryName(workingNewPath))) Directory.CreateDirectory(Path.GetDirectoryName(workingNewPath));
                            if (!File.Exists(workingNewPath)) File.Move(workingOldPath, workingNewPath);

                            modQar.FilePath = unhashedName;

                        }
                    }
                }
                    
                GzsLib.LoadModDictionary(extractedModEntry);
                ValidateModEntries(ref extractedModEntry);

                Debug.LogLine("[Install] Check mod FPKs against game .dat fpks", Debug.LogLevel.Basic);
                twoFiles.UnionWith(MergePacks(extractedModEntry, pullFromVanillas, pullFromMods));

                Debug.LogLine("[Install] Copying loose textures to 01.", Debug.LogLevel.Basic);
                InstallLooseFtexs(extractedModEntry, ref oneFilesList);

                Debug.LogLine("[Install] Copying game dir files", Debug.LogLevel.Basic);
                InstallGameDirFiles(extractedModEntry, ref gameData);

                //ZIP: WMV Support
                if (!isAddingWMV)
                {
                    if (extractedModEntry.ModWmvEntries.Count > 0)
                        isAddingWMV = true;
                }
            }

            manager.SetGameData(gameData);

            //ZIP: Are we installing any mods containing custom WMVs? If so, update.
            if (isAddingWMV) ModManager.UpdateFoxfs(manager.GetInstalledMods());
        }

        private static void ValidateModEntries(ref ModEntry modEntry)
        {
            Debug.LogLine("[ValidateModEntries] Validating qar entries", Debug.LogLevel.Basic);
            for (int i = modEntry.ModQarEntries.Count - 1; i >= 0; i--)
            {
                ModQarEntry qarEntry = modEntry.ModQarEntries[i];
                if (!GzsLib.IsExtensionValidForArchive(qarEntry.FilePath, ".dat") && !GzsLib.IsExtensionValidForArchive(qarEntry.FilePath, ".g0s"))
                {
                    Debug.LogLine(String.Format("[ValidateModEntries] Found invalid file entry {0} for archive {1}", qarEntry.FilePath, qarEntry.SourceName), Debug.LogLevel.Basic);
                    modEntry.ModQarEntries.RemoveAt(i);
                }
            }
            Debug.LogLine("[ValidateModEntries] Validating fpk entries", Debug.LogLevel.Basic);
            for (int i = modEntry.ModFpkEntries.Count - 1; i >= 0; i--)
            {
                ModFpkEntry fpkEntry = modEntry.ModFpkEntries[i];
                if (!GzsLib.IsExtensionValidForArchive(fpkEntry.FilePath, fpkEntry.FpkFile))
                {
                    Debug.LogLine(String.Format("[ValidateModEntries] Found invalid file entry {0} for archive {1}", fpkEntry.FilePath, fpkEntry.FpkFile), Debug.LogLevel.Basic);
                    modEntry.ModFpkEntries.RemoveAt(i);
                }
            }
        }

        private static HashSet<string> MergePacks(ModEntry extractedModEntry, List<string> pullFromVanillas, List<string> pullFromMods)
        {
            HashSet<string> modQarTwoPaths = new HashSet<string>();
            foreach (ModQarEntry modQar in extractedModEntry.ModQarEntries)
            {
                string workingDestination = Path.Combine("_working2", Tools.ToWinPath(modQar.FilePath));
                if (!Directory.Exists(Path.GetDirectoryName(workingDestination))) Directory.CreateDirectory(Path.GetDirectoryName(workingDestination));
                string modQarSource = Path.Combine("_extr", Tools.ToWinPath(modQar.FilePath));
                string existingQarSource;

                if (pullFromMods.FirstOrDefault(e => e == modQar.FilePath) != null)
                {
                    existingQarSource = workingDestination;
                }
                else
                {
                    int indexToRemove = pullFromVanillas.FindIndex(m => m == modQar.FilePath); // remove from retrievalfilepaths and add to editlist
                    if (indexToRemove >= 0)
                    {
                        existingQarSource = Path.Combine("_gameFpk", Tools.ToWinPath(modQar.FilePath));
                        pullFromVanillas.RemoveAt(indexToRemove); pullFromMods.Add(modQar.FilePath);
                    }
                    else
                    {
                        existingQarSource = null;
                        if (modQar.FilePath.EndsWith(".fpk") || modQar.FilePath.EndsWith(".fpkd"))
                            pullFromMods.Add(modQar.FilePath); // for merging non-native fpk files consecutively
                    }
                }

                if (existingQarSource != null)
                {
                    var pulledPack = GzsLib.ExtractArchive<FpkFile>(existingQarSource, "_build");
                    var extrPack = GzsLib.ExtractArchive<FpkFile>(modQarSource, "_build");
                    pulledPack = pulledPack.Union(extrPack).ToList();
                    var fpkReferences = GzsLib.GetFpkReferences(existingQarSource);
                    GzsLib.WriteFpkArchive(workingDestination, "_build", pulledPack, fpkReferences);
                }
                else
                {
                    File.Copy(modQarSource, workingDestination, true);
                }

                if (!modQar.FilePath.Contains(".ftex"))
                {
                    modQarTwoPaths.Add(Tools.ToWinPath(modQar.FilePath));
                }
            }

            return modQarTwoPaths;
        }

        // i/o: _extr to _working1
        private static void InstallLooseFtexs(ModEntry modEntry, ref List<string> oneFilesList)
        {
            foreach (ModQarEntry modQarEntry in modEntry.ModQarEntries)
            {
                if (modQarEntry.FilePath.Contains(".ftex"))
                {
                    if (!oneFilesList.Contains(Tools.ToWinPath(modQarEntry.FilePath)))
                    {
                        oneFilesList.Add(Tools.ToWinPath(modQarEntry.FilePath));
                    }
                    string sourceFile = Path.Combine("_extr", Tools.ToWinPath(modQarEntry.FilePath));
                    string destFile = Path.Combine("_working1", Tools.ToWinPath(modQarEntry.FilePath));
                    string destDir = Path.GetDirectoryName(destFile);
                    Debug.LogLine(String.Format("[Install] Copying texture file: {0}", modQarEntry.FilePath), Debug.LogLevel.All);
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                    File.Copy(sourceFile, destFile, true);
                }
            }
        }

        // i/o: _extr to GameDir
        private static void InstallGameDirFiles(ModEntry modEntry, ref GameData gameData)
        {
            foreach (ModFileEntry fileEntry in modEntry.ModFileEntries)
            {
                bool skipFile = false;
                foreach (string ignoreFile in Tools.ignoreFileList)
                {
                    if (fileEntry.FilePath.Contains(ignoreFile))
                    {
                        skipFile = true;
                    }
                }
                /*
                foreach (string ignoreExt in ignoreExtList)
                {
                    if (fileEntry.FilePath.Contains(ignoreExt))
                    {
                        skipFile = true;
                    }
                }
                */
                if (skipFile == false)
                {
                    string sourceFile = Path.Combine("_extr\\GameDir", Tools.ToWinPath(fileEntry.FilePath));
                    string destFile = Path.Combine(GamePaths.GameDirSB_Build, Tools.ToWinPath(fileEntry.FilePath));
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(sourceFile, destFile, true);

                    if (gameData.GameFileEntries.FirstOrDefault(e => e.FilePath == fileEntry.FilePath) == null)
                        gameData.GameFileEntries.Add(fileEntry);
                }
            }
        }//InstallGameDirFiles

        private static void AddToSettingsFpk(List<ModEntry> installEntryList, SettingsManager manager, List<Dictionary<ulong, GameFile>> allQarGameFiles, out List<string> PullFromVanillas, out List<string> pullFromMods, out Dictionary<string, bool> pathUpdatesExist)
        {
            GameData gameData = manager.GetGameData();
            pathUpdatesExist = new Dictionary<string, bool>();

            List<string> newModQarEntries = new List<string>();
            List<string> modQarFiles = manager.GetModQarFiles();
            pullFromMods = new List<string>();
            foreach (ModEntry modToInstall in installEntryList)
            {
                Dictionary<string, string> newNameDictionary = new Dictionary<string, string>();
                int foundUpdate = 0;
                foreach (ModQarEntry modQar in modToInstall.ModQarEntries.Where(entry => !entry.FilePath.StartsWith("/Assets/")))
                {
                    //Debug.LogLine(string.Format("Attempting to update Qar filename: {0}", modQar.FilePath), Debug.LogLevel.Basic);
                    string unhashedName = HashingExtended.UpdateName(modQar.FilePath);
                    if (unhashedName != null)
                    {
                        //Debug.LogLine(string.Format("Success: {0}", unhashedName), Debug.LogLevel.Basic);
                        newNameDictionary.Add(modQar.FilePath, unhashedName);
                        foundUpdate++;

                        modQar.FilePath = unhashedName;
                        if (!pathUpdatesExist.ContainsKey(modToInstall.Name))
                            pathUpdatesExist.Add(modToInstall.Name, true);
                        else
                            pathUpdatesExist[modToInstall.Name] = true;
                    }
                }
                if (foundUpdate > 0)
                {
                    foreach (ModFpkEntry modFpkEntry in modToInstall.ModFpkEntries)
                    {
                        string unHashedName;
                        if (newNameDictionary.TryGetValue(modFpkEntry.FpkFile, out unHashedName))
                            modFpkEntry.FpkFile = unHashedName;
                    }
                }
                else if(!pathUpdatesExist.ContainsKey(modToInstall.Name))
                        pathUpdatesExist.Add(modToInstall.Name, false);

                manager.AddMod(modToInstall);
                //foreach (ModQarEntry modqar in modToInstall.ModQarEntries) Debug.LogLine("Mod Qar in modToInstall: " + modqar.FilePath);
                foreach (ModQarEntry modQarEntry in modToInstall.ModQarEntries) // add qar entries (fpk, fpkd) to GameData if they don't already exist
                {
                    string modQarFilePath = modQarEntry.FilePath;
                    if (!(modQarFilePath.EndsWith(".fpk") || modQarFilePath.EndsWith(".fpkd"))) continue; // only pull for Qar's with Fpk's

                    if (modQarFiles.Any(entry => entry == modQarFilePath))
                    {
                        pullFromMods.Add(modQarFilePath);
                        //Debug.LogLine("Pulling from 00.dat: {0} " + modQarFilePath);
                    }
                    else if (!newModQarEntries.Contains(modQarFilePath))
                    {
                        newModQarEntries.Add(modQarFilePath);
                        //Debug.LogLine("Pulling from base archives: {0} " + modQarFilePath);
                    }

                } 
            }
            //Debug.LogLine(string.Format("Foreach nest 1 complete"));
            List<ModFpkEntry> newModFpkEntries = new List<ModFpkEntry>();
            foreach (ModEntry modToInstall in installEntryList)
            {
                foreach (ModFpkEntry modFpkEntry in modToInstall.ModFpkEntries)
                {
                    //Debug.LogLine(string.Format("Checking out {0} from {1}", modFpkEntry.FilePath, modFpkEntry.FpkFile));

                    if(newModQarEntries.Contains(modFpkEntry.FpkFile)) // it isn't already part of the snakebite environment
                    {
                        //Debug.LogLine(string.Format("seeking repair files around {0}", modFpkEntry.FilePath));
                        newModFpkEntries.Add(modFpkEntry);
                    }
                    else
                    {
                        //Debug.LogLine(string.Format("Removing {0} from gameFpkEntries so it will only be listed in the mod's entries", modFpkEntry.FilePath));
                        int indexToRemove = gameData.GameFpkEntries.FindIndex(m => m.FilePath == Tools.ToWinPath(modFpkEntry.FilePath)); // this will remove the gamedata's listing of the file under fpkentries (repair entries), so the filepath will only be listed in the modentry
                        if (indexToRemove >= 0) gameData.GameFpkEntries.RemoveAt(indexToRemove);
                    }
                }
            }
            //Debug.LogLine(string.Format("Foreach nest 2 complete"));
            HashSet<ulong> mergeFpkHashes = new HashSet<ulong>();
            PullFromVanillas = new List<string>();
            var repairFpkEntries = new List<ModFpkEntry>();
            foreach (ModFpkEntry newFpkEntry in newModFpkEntries) // this will add the fpkentry listings (repair entries) to the settings xml
            {
                //Debug.LogLine(string.Format("checking {0} for repairs", newFpkEntry.FilePath));
                ulong packHash = Tools.NameToHash(newFpkEntry.FpkFile);
                if (mergeFpkHashes.Contains(packHash)) continue; // the process has already plucked this particular qar file

                foreach (var archiveQarGameFiles in allQarGameFiles) // check every archive (except 00) to see if the particular qar file already exists
                {
                    //Debug.LogLine(string.Format("checking archive for an existing qar file"));
                    if (archiveQarGameFiles.Count > 0)
                    {
                        GameFile existingPack = null;
                        archiveQarGameFiles.TryGetValue(packHash, out existingPack);
                        if (existingPack != null) // the qar file is found
                        {
                            //Debug.LogLine(string.Format("Qar file {0} found in {1}. adding to gameqarentries", newFpkEntry.FpkFile, existingPack.QarFile));
                            mergeFpkHashes.Add(packHash);
                            gameData.GameQarEntries.Add(new ModQarEntry{
                                FilePath = newFpkEntry.FpkFile,
                                SourceType = FileSource.Merged,
                                SourceName = existingPack.QarFile,
                                Hash = existingPack.FileHash
                            });
                            PullFromVanillas.Add(newFpkEntry.FpkFile);

                            string windowsFilePath = Tools.ToWinPath(newFpkEntry.FpkFile); // Extract the pack file from the vanilla game files, place into _gamefpk for future use
                            string sourceArchive = Path.Combine(GamePaths.GameDir, "master\\" + existingPack.QarFile);
                            string workingPath = Path.Combine("_gameFpk", windowsFilePath);
                            if (!Directory.Exists(Path.GetDirectoryName(workingPath))) Directory.CreateDirectory(Path.GetDirectoryName(workingPath));

                            GzsLib.ExtractFileByHash<QarFile>(sourceArchive, existingPack.FileHash, workingPath); // extracts the specific .fpk from the game data
                            foreach (string listedFile in GzsLib.ListArchiveContents<FpkFile>(workingPath))
                            {
                                repairFpkEntries.Add(new ModFpkEntry {
                                    FpkFile = newFpkEntry.FpkFile,
                                    FilePath = listedFile,
                                    SourceType = FileSource.Merged,
                                    SourceName = existingPack.QarFile
                                });
                                //Debug.LogLine(string.Format("File Listed: {0} in {1}", extractedFile, newFpkEntry.FpkFile));
                            }
                            break;
                        }
                    }
                }
            }
            //Debug.LogLine(string.Format("Foreach nest 3 complete"));
            foreach (ModFpkEntry newFpkEntry in newModFpkEntries) // finally, strip away the modded entries from the repair entries
            {
                //Debug.LogLine(string.Format("checking to remove {0} from gamefpkentries", Tools.ToWinPath(newFpkEntry.FilePath)));
                int indexToRemove = repairFpkEntries.FindIndex(m => m.FilePath == Tools.ToWinPath(newFpkEntry.FilePath));
                if (indexToRemove >= 0) repairFpkEntries.RemoveAt(indexToRemove);
            }
            gameData.GameFpkEntries = gameData.GameFpkEntries.Union(repairFpkEntries).ToList();
            manager.SetGameData(gameData);
        }
    }
}
