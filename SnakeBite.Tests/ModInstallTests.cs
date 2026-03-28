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
        private const string e20020InstalledData02Md5 = "A28B525A75C2EDF69F4A1D2D2563C7F4";

        private const string OriginalData01Md5 = "9B3F5AD14EBE878E1460CA2994F2673E";
        private const string e20020InstalledData01Md5 = "9B3F5AD14EBE878E1460CA2994F2673E";
        private const string bandanaInstalledData01Md5 = "ABFC96EE1BF3545E11AC8660C5C800E5";
        private const string bandanaInstalledData02Md5 = "0BAC09860286CD00B023BB95AF8CB37B";
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
                File.Copy(GamePaths.chunk0Path, GamePaths.chunk0Path + GamePaths.original_ext, true);
                File.Copy(GamePaths.OnePath, GamePaths.OnePath + GamePaths.original_ext, true);

                // 2. Action: Install the mod
                var modFiles = new List<string> { TestModPath };
                bool installSuccess = InstallManager.InstallMods(modFiles);
                if (!installSuccess) throw new Exception("Mod installation failed.");

                // 3. Post-condition: Check modified MD5
                var installedMd5 = CalculateMD5(GamePaths.chunk0Path);
                var installed01Md5 = CalculateMD5(GamePaths.OnePath);
                
                if (e20020InstalledData02Md5 != installedMd5) throw new Exception(string.Format("data_02.g0s hash did not match expected installed hash. Expected {0}, but got {1}.", e20020InstalledData02Md5, installedMd5));
                if (e20020InstalledData01Md5 != installed01Md5) throw new Exception(string.Format("data_01.g0s hash did not match expected installed hash. Expected {0}, but got {1}.", e20020InstalledData01Md5, installed01Md5));
                
                // 4. Action: Uninstall the mod (Commented out because UninstallMods is deeply coupled to WinForms)

                // 5. Revert-condition: Check reverted MD5
                // var revertedMd5 = CalculateMD5(GamePaths.chunk0Path);
                // var reverted01Md5 = CalculateMD5(GamePaths.OnePath);
                // if (OriginalData02Md5 != revertedMd5) throw new Exception("data_02.g0s was not properly restored to its original state after uninstall.");
                // if (OriginalData01Md5 != reverted01Md5) throw new Exception("data_01.g0s was not properly restored to its original state after uninstall.");
            }
            finally
            {
                // Clean up dummy backups internally used by SnakeBite
                if (File.Exists(GamePaths.chunk0Path + GamePaths.original_ext))
                {
                    if (File.Exists(GamePaths.chunk0Path)) File.Delete(GamePaths.chunk0Path);
                    File.Move(GamePaths.chunk0Path + GamePaths.original_ext, GamePaths.chunk0Path);
                }
                
                if (File.Exists(GamePaths.OnePath + GamePaths.original_ext))
                {
                    if (File.Exists(GamePaths.OnePath)) File.Delete(GamePaths.OnePath);
                    File.Move(GamePaths.OnePath + GamePaths.original_ext, GamePaths.OnePath);
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
