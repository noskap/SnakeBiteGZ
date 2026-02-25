using GzsTool.Core.Fpk;
using GzsTool.Core.Qar;
using SnakeBite.Forms;
using SnakeBite.GzsTool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
//using static SnakeBite.GamePaths; // Removed for C# 5.0 compatibility

namespace SnakeBite
{
    internal static class ModManager
    {

        //Cull any invalid entries that might have slipped in via older versions of snakebite
        public static void ValidateGameData(ref GameData gameData)
        {
            Debug.LogLine("[ValidateGameData] Validating gameData files", Debug.LogLevel.Basic);
            Debug.LogLine("[ValidateGameData] Validating qar entries", Debug.LogLevel.Basic);
            for (int i = gameData.GameQarEntries.Count-1; i >= 0; i--)
            {
                ModQarEntry qarEntry = gameData.GameQarEntries[i];
                if (!GzsLib.IsExtensionValidForArchive(qarEntry.FilePath, ".dat") && !GzsLib.IsExtensionValidForArchive(qarEntry.FilePath, ".g0s"))
                {
                    Debug.LogLine(String.Format("[ValidateGameData] Found invalid file entry {0} for archive {1}", qarEntry.FilePath, qarEntry.SourceName), Debug.LogLevel.Basic);
                    gameData.GameQarEntries.RemoveAt(i);
                }
            }
            Debug.LogLine("[ValidateGameData] Validating fpk entries", Debug.LogLevel.Basic);
            for (int i = gameData.GameFpkEntries.Count-1; i >= 0; i--)
            {
                ModFpkEntry fpkEntry = gameData.GameFpkEntries[i];
                if (!GzsLib.IsExtensionValidForArchive(fpkEntry.FilePath, fpkEntry.FpkFile))
                {
                    Debug.LogLine(String.Format("[ValidateGameData] Found invalid file entry {0} for archive {1}", fpkEntry.FilePath, fpkEntry.FpkFile), Debug.LogLevel.Basic);
                    gameData.GameFpkEntries.RemoveAt(i);
                }
            }
        }

        public static bool foundLooseFtexs(List<string> ModFiles) // returns true if any mods in the list contain a loose texture file which was installed to 01
        {
            ModEntry metaData;
            foreach (string modfile in ModFiles)
            {
                metaData = Tools.ReadMetaData(modfile);
                foreach (ModQarEntry qarFile in metaData.ModQarEntries)
                {
                    if (qarFile.FilePath.Contains(".ftex"))
                        return true;
                }
            }
            return false;
        }

        public static bool foundLooseFtexs(List<ModEntry> checkMods) // returns true if any mods in the list contain a loose texture file which was installed to 01
        {
            foreach (ModEntry mod in checkMods)
            {
                foreach (ModQarEntry qarFile in mod.ModQarEntries)
                {
                    if (qarFile.FilePath.Contains(".ftex"))
                        return true;
                }
            }
            return false;
        }

        public static bool hasQarZeroFiles(List<string> ModFiles) // returns true if any mods in the list contain a loose texture file which was installed to 01
        {
            ModEntry metaData;
            foreach (string modfile in ModFiles)
            {
                metaData = Tools.ReadMetaData(modfile);
                foreach (ModQarEntry qarFile in metaData.ModQarEntries)
                {
                    if (!qarFile.FilePath.Contains(".ftex"))
                        return true;
                }
            }
            return false;
        }

        public static bool hasQarZeroFiles(List<ModEntry> checkMods) // any non-.ftex(s) file in modQarEntries will return true
        {
            foreach (ModEntry mod in checkMods)
            {
                foreach (ModQarEntry qarFile in mod.ModQarEntries)
                {
                    if (!qarFile.FilePath.Contains(".ftex")) 
                        return true;
                }
            }
            return false;
        }



        public static void ClearBuildFiles(string onePath, string chunk0Path, string settingsPath, string savePresetPath)
        {
            if (File.Exists(onePath + GamePaths.build_ext)) File.Delete(onePath + GamePaths.build_ext);
            if (File.Exists(chunk0Path + GamePaths.build_ext)) File.Delete(chunk0Path + GamePaths.build_ext);
            if (File.Exists(settingsPath + GamePaths.build_ext)) File.Delete(settingsPath + GamePaths.build_ext);
            if (File.Exists(savePresetPath + GamePaths.build_ext)) File.Delete(savePresetPath + GamePaths.build_ext);
        }


        public static void AddChunks(ref List<string> foxfsLine)//ZIP: Retain additional chunk support
        {
            // GZ specific: insert a_chunk7 at the top of the chunk list (ID 0) and shift others or just ensure it's loaded.
            // Since we don't know the exact GZ foxfs structure, we look for the first <chunk> tag.
            
            string newChunkLine = "		<chunk id=\"0\" label=\"old\" qar=\"a_chunk7.dat\" textures=\"a_texture7.dat\"/>";
            
            int startIndex = -1;
            // Find the first occurrence of <chunk id=...
            for(int i=0; i<foxfsLine.Count; i++)
            {
                if (foxfsLine[i].Trim().StartsWith("<chunk id="))
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex >= 0)
            {
                if (foxfsLine[startIndex].Contains("a_chunk7.dat")) return; // Already exists
                
                // For GZ, we might need to be careful about IDs. 
                // We'll insert at the top (presumably ID 0 is safe or high priority?).
                // We won't remove existing chunks like the MGSV code did, to avoid breaking GZ chunks we don't know about.
                foxfsLine.Insert(startIndex, newChunkLine);
                
                // TODO: If we insert ID 0, and there is already ID 0, does it conflict?
                // MGSV mod manager re-indexed them. If we can't reliably re-index, we warn.
                // But for now, this is safer than crashing.
                Debug.LogLine("[ModManager] Inserted a_chunk7 into foxfs.dat");
            }
            else
            {
                Debug.LogLine("[ModManager] Could not find <chunk> tags in foxfs.dat. Appending to </foxfs> as fallback.");
                int endTag = foxfsLine.IndexOf("</foxfs>");
                if (endTag >= 0)
                {
                     foxfsLine.Insert(endTag, newChunkLine);
                }
            }
        }

        public static bool ModifyFoxfs() // edits the chunk/texture lines in foxfs.dat to accommodate a_chunk7 a_texture7, MGO and GZs data.
        {
            CleanupFolders();

            Debug.LogLine("[ModifyFoxfs] Beginning foxfs.dat check.", Debug.LogLevel.Debug);
            try
            {
                string foxfsInPath = "foxfs.dat";
                string foxfsOutPath = "_extr\\foxfs.dat";

                if (GzsLib.ExtractFile<QarFile>(GamePaths.chunk0Path, foxfsInPath, foxfsOutPath)) //extract foxfs alone, to save time if the changes are already made
                {
                    if (!File.ReadAllText(foxfsOutPath).Contains("a_chunk7.dat")) // checks if there's an indication that it's modified
                    {
                        Debug.LogLine("[ModifyFoxfs] foxfs.dat is unmodified, extracting chunk0.dat.", Debug.LogLevel.Debug);
                        List<string> chunk0Files = GzsLib.ExtractArchive<QarFile>(GamePaths.chunk0Path, "_extr"); //extract chunk0 into _extr
                        var foxfsLine = File.ReadAllLines(foxfsOutPath).ToList();   // read the file
                        Debug.LogLine("[ModifyFoxfs] Updating foxfs.dat", Debug.LogLevel.Debug);
                        AddChunks(ref foxfsLine); //ZIP: Retain additional chunk support
                        File.WriteAllLines(foxfsOutPath, foxfsLine); // write to file

                        //Build chunk0.dat.SB_Build with modified foxfs
                        Debug.LogLine("[ModifyFoxfs] repacking chunk0.dat", Debug.LogLevel.Debug);
                        GzsLib.WriteQarArchive(GamePaths.chunk0Path + GamePaths.build_ext, "_extr", chunk0Files, GzsLib.chunk0Flags);
                    }
                    else
                    {
                        Debug.LogLine("[ModifyFoxfs] foxfs.dat is already modified", Debug.LogLevel.Debug);
                    }
                }
                else
                {
                    MessageBox.Show(string.Format("Setup cancelled: SnakeBite failed to extract foxfs from data_02.g0s."), "foxfs check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.LogLine("[ModifyFoxfs] Process failed: could not check foxfs.dat", Debug.LogLevel.Debug);
                    CleanupFolders();

                    return false;
                }

                Debug.LogLine("[ModifyFoxfs] Archive modification complete.", Debug.LogLevel.Debug);
                CleanupFolders();

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("An error has occured while modifying foxfs in chunk0: {0}", e), "Exception Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.LogLine(string.Format("[ModifyFoxfs] Exception Occurred: {0}", e), Debug.LogLevel.Basic);
                Debug.LogLine("[ModifyFoxfs] SnakeBite has failed to modify foxfs in chunk0", Debug.LogLevel.Basic);

                return false;
            }
        }

        public static bool UpdateFoxfs( List<ModEntry> mods ) //Updates foxfs to accommodate wmvs
        {
            Debug.LogLine("[UpdateFoxfs] Beginning foxfs.dat check.", Debug.LogLevel.Debug);
            try
            {
                List<string> chunk0Files = GzsLib.ExtractArchive<QarFile>(GamePaths.chunk0Path, "_chunk0"); //ZIP: extract chunk0.dat into _chunk0
                string foxfsInPath = "foxfs.dat";
                string foxfsOutPath = "_chunk0\\foxfs.dat";
                if (GzsLib.ExtractFile<QarFile>(GamePaths.chunk0Path + ".original", foxfsInPath, foxfsOutPath)) {          
                    Debug.LogLine("[UpdateFoxfs] Updating foxfs.dat", Debug.LogLevel.Debug);
                    var foxfsLine = File.ReadAllLines(foxfsOutPath).ToList();   // read the file

                    //ZIP: Process all custom WMVs.
                    Debug.LogLine("[UpdateFoxfs] Checking installed mods", Debug.LogLevel.Debug);
                    HashSet<ulong> customWMVs = new HashSet<ulong>();
                    foreach (ModEntry mod in mods)
                    {
                        foreach (ModWmvEntry entry in mod.ModWmvEntries)
                        {
                            customWMVs.Add(entry.Hash);
                        }
                    }

                    if (customWMVs.Count > 0) //ZIP: If any custom WMVs are found, add them to safiles.
                    {           
                        Debug.LogLine("[UpdateFoxfs] Adding custom WMVs to foxfs.dat", Debug.LogLevel.Debug);
                        int wmvIndex = foxfsLine.IndexOf("	</safiles>"); // ZIP: Add custom wmvs to the end, for JP/EN compatibility
                        foxfsLine.RemoveAt(wmvIndex);
                        foreach (ulong wmvHash in customWMVs)
                        {
                            foxfsLine.Insert(wmvIndex, "		<file code=\"" + wmvHash + "\"/>");
                            wmvIndex += 1;
                        }
                        foxfsLine.Insert(wmvIndex, "	</safiles>");
                    }
                    else
                    {
                        Debug.LogLine("[UpdateFoxfs] No custom WMVs found", Debug.LogLevel.Debug);
                    }
                    AddChunks(ref foxfsLine); //ZIP: Retain additional chunk support
                    File.WriteAllLines(foxfsOutPath, foxfsLine); // write to file
                    GzsLib.WriteQarArchive(GamePaths.chunk0Path, "_chunk0", chunk0Files, GzsLib.chunk0Flags); //ZIP: Repacking chunk0.dat with updated foxfs.dat
                }
                else
                {
                    MessageBox.Show(string.Format("Setup cancelled: SnakeBite failed to extract foxfs from data_02.g0s."), "foxfs check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.LogLine("[UpdateFoxfs] Process failed: could not check foxfs.dat", Debug.LogLevel.Debug);
                    CleanupFolders("_chunk0");

                    return false;
                }

                Debug.LogLine("[UpdateFoxfs] Archive modification complete.", Debug.LogLevel.Debug);
                CleanupFolders("_chunk0");

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("An error has occured while updating foxfs in chunk0: {0}", e), "Exception Occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.LogLine(string.Format("[UpdateFoxfs] Exception Occurred: {0}", e), Debug.LogLevel.Basic);
                Debug.LogLine("[UpdateFoxfs] SnakeBite has failed to update foxfs in chunk0", Debug.LogLevel.Basic);

                return false;
            }
        }

        public static void PrepGameDirFiles()
        {
            CopyGameDirManagedFiles(GamePaths.GameDirSB_Build);
            CopyGameDirManagedFiles(GamePaths.GameDirBackup_Build);
        }

        public static void CopyGameDirManagedFiles(string destinationDir)
        {
            Debug.LogLine(String.Format("[SB_Build] Copying {0} to {1}", Path.GetDirectoryName(GamePaths.GameDir), Path.GetDirectoryName(destinationDir)), Debug.LogLevel.Basic);
            foreach (string externalFile in new SettingsManager(GamePaths.SnakeBiteSettings).GetModExternalFiles()) 
            {
                string fileModPath = Tools.ToWinPath(externalFile);
                string sourceFullPath = Path.Combine(GamePaths.GameDir, fileModPath);
                string destFullPath = Path.Combine(destinationDir, fileModPath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFullPath));
                if (File.Exists(sourceFullPath)) { File.Copy(sourceFullPath, destFullPath, true); }
            }
        }

        public static void PromoteGameDirFiles() // call this method BEFORE snakebite.xml.SB_Build is promoted, so it will reference the old snakebite.xml
        {
            Debug.LogLine("[SB_Build] Promoting SB_Build Game Directory", Debug.LogLevel.Basic);
            if (!Directory.Exists(GamePaths.GameDirSB_Build))
            {
                Debug.LogLine(String.Format("[SB_Build] Directory not found: {0}", GamePaths.GameDirSB_Build), Debug.LogLevel.Basic);
                return;
            }

            List<string> fileEntryDirs = new List<string>();
            foreach (string externalFile in new SettingsManager(GamePaths.SnakeBiteSettings).GetModExternalFiles())
            {
                string fileModPath = Tools.ToWinPath(externalFile);
                string sourceFullPath = Path.Combine(GamePaths.GameDir, fileModPath);

                string sourceDir = Path.GetDirectoryName(sourceFullPath);
                if (!fileEntryDirs.Contains(sourceDir)) fileEntryDirs.Add(sourceDir);

                if (File.Exists(sourceFullPath)) File.Delete(sourceFullPath); // deletes all of the old snakebite.xml's managed files (unmanaged files will be overwritten later or left alone)
                else Debug.LogLine(string.Format("[SB_Build] File not found: {0}", sourceFullPath), Debug.LogLevel.Basic);
            }

            Tools.DirectoryCopy(GamePaths.GameDirSB_Build, GamePaths.GameDir, true); // moves all gamedir_sb_build files over

            foreach (string fileEntryDir in fileEntryDirs)
            {
                if (Directory.Exists(fileEntryDir) && Directory.GetFiles(fileEntryDir).Length == 0)
                {
                    Debug.LogLine(String.Format("[SB_Build] deleting empty folder: {0}", fileEntryDir), Debug.LogLevel.All);
                    try
                    {
                        Directory.Delete(fileEntryDir);
                    }
                    catch
                    {
                        Console.WriteLine("[Uninstall] Could not delete: " + fileEntryDir);
                    }
                }
            }
        }

        public static void RestoreBackupGameDir(SettingsManager SBBuildSettings)
        {
            Debug.LogLine("[SB_Build] Promoting SB_Build Game Directory", Debug.LogLevel.Basic);
            if (!Directory.Exists(GamePaths.GameDirBackup_Build))
            {
                Debug.LogLine(String.Format("[SB_Build] Directory not found: {0}", GamePaths.GameDirBackup_Build), Debug.LogLevel.Basic);
                return;
            }

            List<string> fileEntryDirs = new List<string>();
            foreach (string externalBuildFiles in SBBuildSettings.GetModExternalFiles())
            {
                string fileModPath = Tools.ToWinPath(externalBuildFiles);
                string sourceFullPath = Path.Combine(GamePaths.GameDir, fileModPath);

                string sourceDir = Path.GetDirectoryName(sourceFullPath);
                if (!fileEntryDirs.Contains(sourceDir)) fileEntryDirs.Add(sourceDir);

                if (File.Exists(sourceFullPath)) File.Delete(sourceFullPath); // deletes all of the new snakebite.xml's managed files
                else Debug.LogLine(string.Format("[SB_Build] File not found: {0}", sourceFullPath), Debug.LogLevel.Basic);
            }

            Tools.DirectoryCopy(GamePaths.GameDirBackup_Build, GamePaths.GameDir, true); // moves all gamedir_backup_build files over

            foreach (string fileEntryDir in fileEntryDirs) //all the directories that have had files deleted within them
            {
                if (Directory.Exists(fileEntryDir) && Directory.GetFiles(fileEntryDir).Length == 0) // if the directory has not yet been deleted and there are no more files inside the directory
                {
                    Debug.LogLine(String.Format("[SB_Build] deleting empty folder: {0}", fileEntryDir), Debug.LogLevel.All);
                    try
                    {
                        Directory.Delete(fileEntryDir);
                    }
                    catch
                    {
                        Console.WriteLine("[Uninstall] Could not delete: " + fileEntryDir);
                    }
                }
            }
        }

        public static void PromoteBuildFiles(params string[] paths)
        {
            // Promote SB builds
            Debug.LogLine("[SB_Build] Promoting SB_Build files", Debug.LogLevel.Basic);
            foreach (string path in paths)
            {
                GzsLib.PromoteQarArchive(path + GamePaths.build_ext, path);
            }
            
            new SettingsManager(GamePaths.SnakeBiteSettings).UpdateG0sHash();
        }

        public static void ClearBuildFiles(params string[] paths)
        {
            Debug.LogLine("[SB_Build] Deleting SB_Build files", Debug.LogLevel.Basic);
            foreach (string path in paths)
            {
                if (File.Exists(path + GamePaths.build_ext))
                    File.Delete(path + GamePaths.build_ext);
            }
        }

        public static void ClearSBGameDir()
        {
            Debug.LogLine("[SB_Build] Deleting old SB_Build Game Directory", Debug.LogLevel.Basic);
            try
            {
                if(Directory.Exists(GamePaths.GameDirSB_Build))
                    Tools.DeleteDirectory(GamePaths.GameDirSB_Build);
                if (Directory.Exists(GamePaths.GameDirBackup_Build))
                    Tools.DeleteDirectory(GamePaths.GameDirBackup_Build);
            }
            catch (IOException e)
            {
                Console.WriteLine("[Cleanup] Could not delete Game Directory Content: " + e.ToString());
            }
        }

        /// <summary>
        /// Checks 00.dat files, indcluding fpk contents and adds the different mod entry types (if missing) to database (snakebite.xml)
        /// Slows down as number of fpks increase
        /// </summary>
        public static void CleanupDatabase()
        {
            Debug.LogLine("[Cleanup] Database cleanup started", Debug.LogLevel.Basic);

            // Retrieve installation data
            SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
            var mods = manager.GetInstalledMods();
            var game = manager.GetGameData();
            // GZ: data_00 is WMV. Use data_01 (OnePath) for cleanup checks.
            var oneFiles = GzsLib.ListArchiveContents<QarFile>(GamePaths.OnePath);

            //Should only happen if user manually mods 01.dat
            Debug.LogLine("[Cleanup] Removing duplicate file entries", Debug.LogLevel.Debug);
            // Remove duplicate file entries
            var cleanFiles = oneFiles.ToList();
            foreach (string file in oneFiles)
            {
                while (cleanFiles.Count(entry => entry == file) > 1)
                {
                    cleanFiles.Remove(file);
                    Debug.LogLine(String.Format("[Cleanup] Found duplicate file in 01.dat: {0}", file), Debug.LogLevel.Debug);
                }
            }

            Debug.LogLine("[Cleanup] Examining FPK archives", Debug.LogLevel.Debug);
            var GameFpks = game.GameFpkEntries.ToList();
            // Search for FPKs in game data
            var fpkFiles = cleanFiles.FindAll(entry => entry.EndsWith(".fpk") || entry.EndsWith(".fpkd"));
            foreach (string fpkFile in fpkFiles)
            {
                string fpkName = Path.GetFileName(fpkFile);
                // Extract FPK from archive
                Debug.LogLine(String.Format("[Cleanup] Examining {0}", fpkName));
                GzsLib.ExtractFile<QarFile>(GamePaths.OnePath, fpkFile, fpkName);

                // Read FPK contents
                var fpkContent = GzsLib.ListArchiveContents<FpkFile>(fpkName);

                // Add contents to game FPK list
                foreach (var c in fpkContent)
                {
                    if (GameFpks.Count(entry => Tools.CompareNames(entry.FilePath, c) && Tools.CompareHashes(entry.FpkFile, fpkFile)) == 0)
                    {
                        GameFpks.Add(new ModFpkEntry() { FpkFile = fpkFile, FilePath = c });
                    }
                }
                try
                {
                    File.Delete(fpkName);
                } catch
                {
                    Console.WriteLine("[Uninstall] Could not delete: " + fpkName);
                }
            }

            Debug.LogLine("[Cleanup] Checking installed mods", Debug.LogLevel.Debug);
            Debug.LogLine("[Cleanup] Removing all installed mod data from game data list", Debug.LogLevel.Debug);
            foreach (var mod in mods)
            {
                foreach (var qarEntry in mod.ModQarEntries)
                {
                    cleanFiles.RemoveAll(file => Tools.CompareHashes(file, qarEntry.FilePath));
                }

                foreach (var fpkEntry in mod.ModFpkEntries)
                {
                    GameFpks.RemoveAll(fpk => Tools.CompareHashes(fpk.FpkFile, fpkEntry.FpkFile) && Tools.ToQarPath(fpk.FilePath) == Tools.ToQarPath(fpkEntry.FilePath));
                }
            }

            Debug.LogLine("[Cleanup] Checking mod QAR files against game files", Debug.LogLevel.Debug);
            foreach (var s in cleanFiles)
            {
                if (game.GameQarEntries.Count(entry => Tools.CompareHashes(entry.FilePath, s)) == 0)
                {
                    Debug.LogLine(String.Format("[Cleanup] Adding missing {0}", s), Debug.LogLevel.Debug);
                    game.GameQarEntries.Add(new ModQarEntry() {
                        FilePath = Tools.ToQarPath(s),
                        SourceType = FileSource.System,
                        Hash = Tools.NameToHash(s)
                    });
                }
            }

            game.GameFpkEntries = GameFpks;
            manager.SetGameData(game);
        }

        internal static Version GetMGSVersion()
        {
            // Get MGSV executable version
            var versionInfo = FileVersionInfo.GetVersionInfo(GamePaths.GameDir + "\\MgsGroundZeroes.exe");
            if (versionInfo != null)
            {
                if (versionInfo.ProductVersion != null)
                {
                    return new Version(versionInfo.ProductVersion);
                }
            }
            return new Version(0,0,0,0);
        }

        internal static Version GetSBVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        internal static Version GetSBGZVersion()
        {
            return new Version(0, 0, 1);
        }

        private static List<string> cleanupFolders = new List<string> {
            "_working0",
            "_working1",
            "_working2",//LEGACY
            "_extr",
            "_build",
            "_gameFpk",
            "_modfpk",
            "_chunk0", //ZIP: WMV Support
        };

        public static void CleanupFolders(string selectedFolder = "") // deletes the work folders which contain extracted files from 00/01
        {
            Debug.LogLine("[Mod] Cleaning up snakebite work folders.");
            try
            {
                foreach (var folder in cleanupFolders)
                {
                    //ZIP: WMV Support
                    if (selectedFolder.Length > 0)
                    {
                        if (selectedFolder != folder)
                            continue;
                    }

                    if (Directory.Exists(folder))
                    {
                        Tools.DeleteDirectory(folder);

                    }
                }
            }
            catch (Exception e){ Debug.LogLine("[Mod] Exception occurred while attempting to remove SnakeBite work folders: " + e.ToString()); }
        }
    }//class ModManager
}//namespace SnakeBite