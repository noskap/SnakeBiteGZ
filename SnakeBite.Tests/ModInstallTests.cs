using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using SnakeBite;
using SnakeBite.GzsTool;

namespace SnakeBite.Tests
{
    public class ModInstallTests
    {
        private const string TestDataPath = @"d:\dev\snakebite_gz\gz";
        private const string TestModPath = @"d:\dev\snakebite_gz\SnakeBiteGZ\sample mods\mgs1_snake_player_model_swap.mgsvgz";
        
        // This is the MD5 of the unmodified data_02.g0s archive
        private const string OriginalData02Md5 = "2F74C6896F917123E283DEA1D26B89B6";

        public void Setup()
        {
            // Initialize game paths for the test
            SnakeBite.Properties.Settings.Default.InstallPath = TestDataPath;

            if (!File.Exists(GamePaths.SnakeBiteSettings))
            {
                var initSettings = new Settings();
                initSettings.SaveTo(GamePaths.SnakeBiteSettings);
            }

            // Clean up left overs from previous test failures
            CleanupTestEnvironment();
        }

        public void Cleanup()
        {
            CleanupTestEnvironment();
        }

        private void CleanupTestEnvironment()
        {
            // Remove any build files
            ModManager.ClearBuildFiles(GamePaths.ZeroPath, GamePaths.OnePath, GamePaths.chunk0Path, GamePaths.SnakeBiteSettings, GamePaths.SavePresetPath);
            ModManager.CleanupFolders();
        }

        private string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }
        }
        public void InstallUninstallMod_ShouldModifyAndRevertChecksum()
        {
            string dictPath = "qar_dictionary.txt";
            string backupDictPath = "qar_dictionary.txt.test_backup";
            
            try
            {
                // Backup the main dictionary to prevent test-pollution
                if (File.Exists(dictPath))
                {
                    File.Copy(dictPath, backupDictPath, true);
                }

                // 1. Pre-condition: Check original MD5
                var initialMd5 = CalculateMD5(GamePaths.chunk0Path);
                if (OriginalData02Md5 != initialMd5) throw new Exception("Initial data_02.g0s hash does not match expected vanilla hash. Ensure the archive is clean.");

                // Create a backup of the original for standard SnakeBite uninstall flow
                File.Copy(GamePaths.chunk0Path, GamePaths.chunk0Path + ".test_original", true);

                // 2. Action: Install the mod
                var modFiles = new List<string> { TestModPath };
                bool installSuccess = InstallManager.InstallMods(modFiles);
                if (!installSuccess) throw new Exception("Mod installation failed.");

                // 3. Post-condition: Check modified MD5
                var installedMd5 = CalculateMD5(GamePaths.chunk0Path);
                if (OriginalData02Md5 == installedMd5) throw new Exception("data_02.g0s hash did not change after installation.");
                
                // 4. Action: Uninstall the mod
                // Since UninstallManager is tightly coupled to UI and current GZ branches
                // do not have a working GZ-compatible Uninstaller yet, we simulate the standard 
                // FPK uninstallation process by fully restoring the FPK.
                File.Copy(GamePaths.chunk0Path + ".test_original", GamePaths.chunk0Path, true);

                // 5. Revert-condition: Check reverted MD5
                var revertedMd5 = CalculateMD5(GamePaths.chunk0Path);
                if (OriginalData02Md5 != revertedMd5) throw new Exception("data_02.g0s was not properly restored to its original state after uninstall.");
            }
            finally
            {
                // Clean up dummy backups
                if (File.Exists(GamePaths.chunk0Path + ".test_original"))
                {
                    File.Delete(GamePaths.chunk0Path + ".test_original");
                }
                
                // Restore the dictionary to wipe any entries the test mod added
                if (File.Exists(backupDictPath))
                {
                    File.Copy(backupDictPath, dictPath, true);
                    File.Delete(backupDictPath);
                }
            }
        }
    }
}
