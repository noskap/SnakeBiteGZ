using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace SnakeBite
{
    static class PresetManager
    {

        /// <summary>
        /// Creates a .MGSVPreset file for the mods that are currently installed
        /// </summary>
        public static bool SavePreset(string presetFilePath)
        {
            bool success = false;
            Directory.CreateDirectory("_build\\master\\0");
            SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
            string presetName = Path.GetFileName(presetFilePath);
            Debug.LogLine(String.Format("[SavePreset] Saving {0}...", presetName), Debug.LogLevel.Basic);
            try
            {
                foreach (string gameFile in manager.GetModExternalFiles())
                {
                    string sourcePath = Path.Combine(GamePaths.GameDir, Tools.ToWinPath(gameFile));

                    string DestDir = "_build\\" + Path.GetDirectoryName(gameFile);
                    string fileName = Path.GetFileName(gameFile);

                    Directory.CreateDirectory(DestDir);
                    if (File.Exists(sourcePath)) { Debug.LogLine(string.Format("[SavePreset] Copying to build directory: {0}", gameFile), Debug.LogLevel.Basic);  File.Copy(sourcePath, Path.Combine(DestDir, fileName), true); }
                    else Debug.LogLine(string.Format("[SavePreset] File not found: {0}", sourcePath), Debug.LogLevel.Basic);
                }


                Debug.LogLine("[SavePreset] Copying to build directory: 01.dat", Debug.LogLevel.Basic);
                File.Copy(GamePaths.OnePath, "_build\\master\\0\\01.dat", true);

                Debug.LogLine("[SavePreset] Copying to build directory: snakebite.xml", Debug.LogLevel.Basic);
                File.Copy(GamePaths.SnakeBiteSettings, "_build\\snakebite.xml", true);

                if (presetFilePath == GamePaths.SavePresetPath + GamePaths.build_ext)
                {
                    Debug.LogLine(String.Format("Note: '{0}' can be disabled in the Settings menu to save time during installation and uninstallation.", Path.GetFileNameWithoutExtension(presetName)), Debug.LogLevel.Basic);
                }

                FastZip zipper = new FastZip();
                Debug.LogLine(string.Format("[SavePreset] Writing {0}...", presetName), Debug.LogLevel.Basic);
                zipper.CreateZip(presetFilePath, "_build", true, "(.*?)");
                Debug.LogLine("[SavePreset] Write Complete", Debug.LogLevel.Basic);
                success = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("An error has occurred and the preset was not saved.\nException: " + e);
            }
            finally
            {
                ModManager.CleanupFolders();
            }

            return success;
        }

        /// <summary>
        /// overwrites existing mods with the set of mods stored in the .MGSVPreset file
        /// </summary>
        public static bool LoadPreset(string presetFilePath)
        {
            bool panicMode = (!File.Exists(GamePaths.ZeroPath) || !File.Exists(GamePaths.OnePath) || !File.Exists(GamePaths.SnakeBiteSettings)); 
            bool success = false;
            ModManager.CleanupFolders();
            SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
            List<string> existingExternalFiles = new List<string>();
            List<string> fileEntryDirs = new List<string>();
            try
            {
                existingExternalFiles = manager.GetModExternalFiles();
            }
            catch
            {
                panicMode = true;
            }
            try
            {
                if (!panicMode)
                {
                    Debug.LogLine("[LoadPreset] Storing backups of existing files...", Debug.LogLevel.Basic);
                    foreach (string gameFile in existingExternalFiles)
                    {
                        string gameFilePath = Path.Combine(GamePaths.GameDir, Tools.ToWinPath(gameFile));
                        if (File.Exists(gameFilePath)) // only stores backups of managed files
                        {
                            Debug.LogLine(string.Format("[LoadPreset] Storing backup: {0}", gameFile), Debug.LogLevel.Basic);
                            fileEntryDirs.Add(Path.GetDirectoryName(gameFilePath));
                            if (File.Exists(gameFilePath + GamePaths.build_ext)) File.Delete(gameFilePath + GamePaths.build_ext);
                            File.Move(gameFilePath, gameFilePath + GamePaths.build_ext);
                        }
                    }


                    Debug.LogLine("[LoadPreset] Storing backup: 01.dat", Debug.LogLevel.Basic);
                    File.Copy(GamePaths.OnePath, GamePaths.OnePath + GamePaths.build_ext, true);

                    Debug.LogLine("[LoadPreset] Storing backup: snakebite.xml", Debug.LogLevel.Basic);
                    File.Copy(GamePaths.SnakeBiteSettings, GamePaths.SnakeBiteSettings + GamePaths.build_ext, true);
                }
                else
                {
                    Debug.LogLine("[LoadPreset] Critical file(s) are disfunctional or not found, skipping backup procedure", Debug.LogLevel.Basic);
                }

                Debug.LogLine("[LoadPreset] Importing preset files", Debug.LogLevel.Basic);
                FastZip unzipper = new FastZip();
                unzipper.ExtractZip(presetFilePath, GamePaths.GameDir, "(.*?)");

                Debug.LogLine("[LoadPreset] Import Complete", Debug.LogLevel.Basic);
                success = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("An error has occurred and the preset was not imported.\nException: " + e);
                if (!panicMode)
                {
                    Debug.LogLine("[LoadPreset] Restoring backup files", Debug.LogLevel.Basic);


                    File.Copy(GamePaths.OnePath + GamePaths.build_ext, GamePaths.OnePath, true);
                    File.Copy(GamePaths.SnakeBiteSettings + GamePaths.build_ext, GamePaths.SnakeBiteSettings, true);

                    foreach (string gameFile in existingExternalFiles)
                    {
                        string gameFilePath = Path.Combine(GamePaths.GameDir, Tools.ToWinPath(gameFile));
                        if (File.Exists(gameFilePath + GamePaths.build_ext))
                            File.Copy(gameFilePath + GamePaths.build_ext, gameFilePath, true);
                    }
                }
            }
            finally
            {
                if (!panicMode)
                {
                    Debug.LogLine("[LoadPreset] Removing backup files", Debug.LogLevel.Basic);
                    foreach (string gameFile in existingExternalFiles)
                    {
                        string gameFilePath = Path.Combine(GamePaths.GameDir, Tools.ToWinPath(gameFile));
                        if (File.Exists(gameFilePath)) File.Delete(gameFilePath + GamePaths.build_ext);
                    }

                    foreach (string fileEntryDir in fileEntryDirs)
                    {
                        if (Directory.Exists(fileEntryDir))
                        {
                            try
                            {
                                if (Directory.GetFiles(fileEntryDir).Length == 0)
                                {
                                    Debug.LogLine(String.Format("[SB_Build] deleting empty folder: {0}", fileEntryDir), Debug.LogLevel.All);
                                    Directory.Delete(fileEntryDir);
                                }
                            }

                            catch
                            {
                                Debug.LogLine("[Uninstall] Could not delete: " + fileEntryDir);
                            }
                        }
                    }

                    File.Delete(GamePaths.OnePath + GamePaths.build_ext);
                    File.Delete(GamePaths.SnakeBiteSettings + GamePaths.build_ext);
                }
            }

            return success;
        }

        public static bool isPresetUpToDate(Settings presetSettings)
        {
                var presetVersion = presetSettings.MGSVersion.AsVersion();
                var MGSVersion = ModManager.GetMGSVersion();
                return (presetVersion == MGSVersion);
        }

        public static Settings ReadSnakeBiteSettings(string PresetFilePath)
        {
            if (!File.Exists(PresetFilePath)) return null;

            try
            {
                using (FileStream streamPreset = new FileStream(PresetFilePath, FileMode.Open))
                using (ZipFile zipMod = new ZipFile(streamPreset))
                {
                    var sbIndex = zipMod.FindEntry("snakebite.xml", true);
                    if (sbIndex == -1) return null;
                    using (StreamReader sbReader = new StreamReader(zipMod.GetInputStream(sbIndex)))
                    {
                        XmlSerializer x = new XmlSerializer(typeof(Settings));
                        var settings = (Settings)x.Deserialize(sbReader);
                        return settings;
                    }
                }
            }
            catch { return null; }

        }

    }
}
