using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;


namespace SnakeBite
{
    public static class BackupManager
    {

        public static bool OriginalsExist()
        {
            return (File.Exists(GamePaths.OnePath + GamePaths.original_ext) && File.Exists(GamePaths.chunk0Path + GamePaths.original_ext));
        }

        public static bool OriginalZeroOneExist()
        {
            return (File.Exists(GamePaths.OnePath + GamePaths.original_ext));
        }

        public static bool c7t7Exist()
        {
            //return (File.Exists(GamePaths.t7Path) && File.Exists(GamePaths.c7Path));
            return false;
        }

        public static bool ModsDisabled()
        {
            return (File.Exists(GamePaths.OnePath + GamePaths.modded_ext));
        }

        public static void RestoreOriginals()
        {
            // delete existing data
            File.Delete(GamePaths.OnePath);
            File.Delete(GamePaths.chunk0Path);
            //File.Delete(GamePaths.c7Path);
            //File.Delete(GamePaths.t7Path);

            // delete mod data
            File.Delete(GamePaths.OnePath + GamePaths.modded_ext);

            // delete GameDir data
            List<string> fileEntryDirs = new List<string>();
            foreach (string externalFile in new SettingsManager(GamePaths.SnakeBiteSettings).GetModExternalFiles())
            {
                string fileModPath = Tools.ToWinPath(externalFile);
                string sourceFullPath = Path.Combine(GamePaths.GameDir, fileModPath);
                string fullPathDir = Path.GetDirectoryName(sourceFullPath);

                try
                {
                    if (File.Exists(sourceFullPath))
                    {
                        File.Delete(sourceFullPath);
                        if (!fileEntryDirs.Contains(fullPathDir)) fileEntryDirs.Add(fullPathDir);
                    }
                }
                catch
                {
                    Debug.LogLine("[Uninstall] Could not delete " + fileModPath);
                }
                
            }

            foreach (string fileEntryDir in fileEntryDirs)
            {
                if (Directory.Exists(fileEntryDir))
                {
                    try
                    {
                        if (Directory.GetFiles(fileEntryDir).Length == 0)
                        {
                            Directory.Delete(fileEntryDir);
                        }
                    }

                    catch
                    {
                        Debug.LogLine("[Uninstall] Could not delete: " + fileEntryDir);
                    }
                }
            }

            // restore backups
            bool fileExists = true;
            while (fileExists)
            {
                Thread.Sleep(100);
                fileExists = false;
                if (File.Exists(GamePaths.OnePath)) fileExists = true;
                if (File.Exists(GamePaths.chunk0Path)) fileExists = true;
                //if (File.Exists(GamePaths.c7Path)) fileExists = true;
                //if (File.Exists(GamePaths.t7Path)) fileExists = true;
            }

            File.Move(GamePaths.ZeroPath + GamePaths.original_ext, GamePaths.ZeroPath);
            File.Move(GamePaths.OnePath + GamePaths.original_ext, GamePaths.OnePath);
            File.Move(GamePaths.chunk0Path + GamePaths.original_ext, GamePaths.chunk0Path);
        }

        public static void DeleteOriginals()
        {
            // delete backups
            File.Delete(GamePaths.OnePath + GamePaths.original_ext);
            File.Delete(GamePaths.chunk0Path + GamePaths.original_ext);
        }

        public static void SwitchToOriginal()
        {
            if (OriginalZeroOneExist())
            {
                // copy mod files to backup
                File.Copy(GamePaths.OnePath, GamePaths.OnePath + GamePaths.modded_ext, true);

                // copy original files
                File.Copy(GamePaths.OnePath + GamePaths.original_ext, GamePaths.OnePath, true);

                SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
                manager.UpdateG0sHash();
            }
        }

        public static void SwitchToMods()
        {
            if (ModsDisabled())
            {
                // restore mod backup
                File.Copy(GamePaths.OnePath + GamePaths.modded_ext, GamePaths.OnePath, true);

                // delete mod backup
                File.Delete(GamePaths.OnePath + GamePaths.modded_ext);
                SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
                manager.UpdateG0sHash();
            }
            if (ModsDisabled())
            {

            }
        }

        /// <summary>
        /// Back up dat files
        /// as DoWorkEventHandler
        /// </summary>
        public static void backgroundWorker_CopyBackupFiles(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backupProcessor = (BackgroundWorker)sender;

            object param = Path.GetFileName(GamePaths.OnePath);
            backupProcessor.ReportProgress(0, string.Format("{0:n0}", String.Format("{0} ({1} KB)", param, Tools.GetFileSizeKB(GamePaths.OnePath))));
            File.Copy(GamePaths.OnePath, GamePaths.OnePath + GamePaths.original_ext, true);

            param = Path.GetFileName(GamePaths.chunk0Path);
            backupProcessor.ReportProgress(0, string.Format("{0:n0}", String.Format("{0} ({1} KB)", param, Tools.GetFileSizeKB(GamePaths.chunk0Path))));
            File.Copy(GamePaths.chunk0Path, GamePaths.chunk0Path + GamePaths.original_ext, true);
        }

        public static bool BackupExists()
        {
            return (OriginalsExist());
        }

        public static bool GameFilesExist()
        {
            return (File.Exists(GamePaths.OnePath) && File.Exists(GamePaths.chunk0Path));
        }
        
    }

    public enum BackupState
    {
        Unknown,
        ModActive,
        DefaultActive,
    }
}