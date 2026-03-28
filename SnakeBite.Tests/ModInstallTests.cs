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
        private const string TestModPath = @"d:\dev\snakebite_gz\SnakeBiteGZ\sample mods\e20020-renegade-threat-weather-time-test.mgsvgz";
        
        // This is the MD5 of the unmodified data_02.g0s archive
        private const string OriginalData02Md5 = "2F74C6896F917123E283DEA1D26B89B6";

        // This is the MD5 of the expected FPK extraction and repacking pipeline output
        private const string InstalledData02Md5 = "37B584C969EB0754304B6993A75442FE";
        private const string OriginalData01Md5 = "9B3F5AD14EBE878E1460CA2994F2673E";

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
                var initial01Md5 = CalculateMD5(GamePaths.OnePath);
                Console.WriteLine("Original Data02 MD5: " + initialMd5);
                Console.WriteLine("Original Data01 MD5: " + initial01Md5);

                // Create a backup of the original for standard SnakeBite uninstall flow
                File.Copy(GamePaths.chunk0Path, GamePaths.chunk0Path + ".test_original", true);
                File.Copy(GamePaths.OnePath, GamePaths.OnePath + ".test_original", true);

                // 2. Action: Install the mod
                var modFiles = new List<string> { TestModPath };
                bool installSuccess = InstallManager.InstallMods(modFiles);
                if (!installSuccess) throw new Exception("Mod installation failed.");

                // 3. Post-condition: Check modified MD5
                var installedMd5 = CalculateMD5(GamePaths.chunk0Path);
                var installed01Md5 = CalculateMD5(GamePaths.OnePath);
                Console.WriteLine("Installed Data02 MD5: " + installedMd5);
                Console.WriteLine("Installed Data01 MD5: " + installed01Md5);
                
                throw new Exception(string.Format("HASHES: Data02 Initial: {0}, Installed: {1} | Data01 Initial: {2}, Installed: {3}", initialMd5, installedMd5, initial01Md5, installed01Md5));
            }
            finally
            {
                // Clean up dummy backups
                if (File.Exists(GamePaths.chunk0Path + ".test_original"))
                {
                    File.Delete(GamePaths.chunk0Path + ".test_original");
                }
                if (File.Exists(GamePaths.OnePath + ".test_original"))
                {
                    File.Delete(GamePaths.OnePath + ".test_original");
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
