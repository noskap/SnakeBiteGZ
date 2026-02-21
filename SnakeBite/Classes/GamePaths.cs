using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeBite
{
    static class GamePaths
    {
        internal static string GameDir { get { return Properties.Settings.Default.InstallPath; } }
        internal static string SBInstallDir { get { return AppDomain.CurrentDomain.BaseDirectory; } }
        
        // GZ Paths
        internal static string chunk0Path { get { return Properties.Settings.Default.InstallPath + "\\data_02.g0s"; } } // Assuming data_02 is chunk0 equivalent
        internal static string OnePath { get { return Properties.Settings.Default.InstallPath + "\\data_01.g0s"; } }
        internal static string ZeroPath { get { return Properties.Settings.Default.InstallPath + "\\data_00.g0s"; } }
        
        internal static string SnakeBiteSettings { get { return Properties.Settings.Default.InstallPath + "\\snakebite.xml"; } }
        internal static string GameDirSB_Build { get { return Properties.Settings.Default.InstallPath + "\\GameDir_SB_Build"; } }
        internal static string GameDirBackup_Build { get { return Properties.Settings.Default.InstallPath + "\\GameDir_Backup_Build"; } }
        internal static string SavePresetPath { get { return Properties.Settings.Default.InstallPath + "\\RevertChanges.GZPreset"; } }

        internal static string build_ext = ".SB_Build";
        internal static string original_ext = ".original";
        internal static string modded_ext = ".modded";

        internal static string NexusURLPath = "https://www.nexusmods.com/games/metalgearsolidvgz";
        internal static string SBWMSearchURLPath = "https://www.nexusmods.com/games/metalgearsolidvgz/search/?search_description=SBWM";
        internal static string SBWMBugURLPath = "https://www.nexusmods.com/games/metalgearsolidvgz/mods/106"; // TODO: Update if GZ has specific bug page
        internal static string WikiURLPath = "https://metalgearmodding.wikia.com/wiki/";
    }
}
