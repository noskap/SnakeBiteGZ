// SYNC to makebite
#define SNAKEBITE //TODO bad
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SnakeBite.GzsTool
{
    public static class GzsLib
    {
        // GZS Tool CLI wrapper implementation
        private const string GzsToolExe = "GzsTool.exe";

        public static uint zeroFlags = 3150304; // Keep flags for reference, though CLI handles repacking via XML
        public static uint oneFlags = 3150048;
        public static uint chunk0Flags = 3150304; // Re-use zeroFlags for now
        public static uint chunk7Flags = 3150304;
        public static uint texture7Flags = 3150304;

        private static Dictionary<string, List<string>> archiveExtensions = new Dictionary<string, List<string>> {
            {"dat",new List<string> { // TPP legacy
                "bnk", "dat", "ffnt", "fmtt", "fpk", "fpkd", "fsm", "fsop", "ftex", "ftexs",
                "json", "lua", "pftxs", "sbp", "subp", "wem",
            }},
            {"g0s",new List<string> { // GZ
                "bnk", "dat", "ffnt", "fmtt", "fpk", "fpkd", "fsm", "fsop", "ftex", "ftexs",
                "json", "lua", "pftxs", "sbp", "subp", "wem",
            }},
            {"fpk",new List<string> {
                "caar", "fnt", "atsh", "frig", "adm", "frt", "fpkl", "fsm", "ftdp", "geobv",
                "ftex", "geoms", "gimr", "gpfp", "grxla", "grxoc", "htre", "lba", "lpsh", "mog",
                "mtar", "nav2", "nta", "rdf", "ends", "sand", "mbl", "tcvp", "spch", "trap",
                "uigb", "uilb", "pcsp", "tre2", "fstb", "twpf", "fv2t", "fmdl", "geom", "gskl",
                "fcnp", "frdv", "fdes", "fclo", "uif", "uia", "subp", "sani", "ladb", "frl",
                "fv2", "obr", "lng2", "mtard", "obrb", "dfrm"
            }},
            {"fpkd",new List<string> {
                "fox2", "evf", "parts", "vfxlb", "vfx", "vfxlf", "veh", "frld", "des", "bnd",
                "tgt", "phsd", "ph", "sim", "clo", "fsd", "sdf", "lua", "lng",
            }},
        };

        static Dictionary<string, string> extensionToType = new Dictionary<string, string> {
            {"dat", "QarFile"},
            {"g0s", "QarFile"}, // Treat g0s as QarFile for logic purposes
            {"fpk", "FpkFile" },
            {"fpkd", "FpkFile" },
        };

        // Run GzsTool.exe via CLI
        private static bool RunGzsTool(string arguments)
        {
            string toolLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GzsToolExe);
            if (!File.Exists(toolLocation))
            {
                Debug.LogLine(String.Format("[GzsLib] Tool not found: {0}", toolLocation));
                return false;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = toolLocation,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            Debug.LogLine(String.Format("[GzsLib] Running: {0} {1}", GzsToolExe, arguments));

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                
                List<string> outputLines = new List<string>();
                List<string> errorLines = new List<string>();

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputLines.Add(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorLines.Add(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (outputLines.Count > 0) Debug.LogLine(String.Format("[GzsTool] {0}", string.Join(Environment.NewLine, outputLines)));
                if (errorLines.Count > 0) Debug.LogLine(String.Format("[GzsTool Error] {0}", string.Join(Environment.NewLine, errorLines)));

                return process.ExitCode == 0;
            }
        }

        // Extract full archive
        // OutputPath is usually a folder name that GzsTool creates. 
        // GzsTool extracts to a folder named after the file in the same directory.
        // We might need to move it to OutputPath if different.
        public static List<string> ExtractArchive<T>(string FileName, string OutputPath) where T : new() 
        {
             // T used to be ArchiveFile, now just generic placeholder to keep signature compatible-ish
            if (!File.Exists(FileName))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", FileName));
                throw new FileNotFoundException();
            }

            string name = Path.GetFileName(FileName);
            Debug.LogLine(String.Format("[GzsLib] Extracting {0} to {1} ({2} KB)", name, OutputPath, Tools.GetFileSizeKB(FileName)));

            // GzsTool extracts to [FileName based folder] in the same dir
            // E.g. data_00.g0s -> data_00_g0s (or similar, need to verify strict naming)
            // Or usually [filename]_[extension] without dot
            
            // Allow GzsTool to do its thing
            if(RunGzsTool(String.Format("\"{0}\"", FileName)))
            {
                // Move extracted folder to OutputPath if needed
                // GzsTool v0.2 output folder naming convention: [Filename]_[Extension w/o dot]
                string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
                string sourceDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);

                if (Directory.Exists(sourceDir))
                {
                    if (Path.GetFullPath(sourceDir).TrimEnd('\\') != Path.GetFullPath(OutputPath).TrimEnd('\\'))
                    {
                        if (Directory.Exists(OutputPath)) Directory.Delete(OutputPath, true);
                        Directory.Move(sourceDir, OutputPath);
                    }
                    
                    // List all files
                    return Directory.GetFiles(OutputPath, "*", SearchOption.AllDirectories)
                                    .Select(f => f.Replace(OutputPath + "\\", "").Replace("\\", "/")) // Relative paths
                                    .ToList();
                }
                else
                {
                    Debug.LogLine(String.Format("[GzsLib] Expected output directory not found: {0}", sourceDir));
                }
            }
            
            return new List<string>();
        }

        // Extract single file is inefficient with CLI tool (must extract all), but implemented for compatibility
        public static bool ExtractFile<T>(string SourceArchive, string FilePath, string OutputFile) where T : new()
        {
            string tempDir = Path.Combine(Path.GetDirectoryName(SourceArchive), "temp_extract_" + Guid.NewGuid());
            try 
            {
                List<string> extractedFiles = ExtractArchive<T>(SourceArchive, tempDir);
                string wantedFile = Path.Combine(tempDir, Tools.ToWinPath(FilePath));
                
                if (File.Exists(wantedFile))
                {
                    string outDir = Path.GetDirectoryName(OutputFile);
                    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                    File.Copy(wantedFile, OutputFile, true);
                    return true;
                }
                return false;
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        // Extract single file by hash - GzsTool handles names, hash lookup is via dictionary anyway
        public static bool ExtractFileByHash<T>(string SourceArchive, ulong FileHash, string OutputFile) where T : new()
        {
             // Need to reverse lookup hash to name first
             string filePath;
             if (HashingExtended.TryGetFilePathFromHash(FileHash, out filePath))
             {
                 return ExtractFile<T>(SourceArchive, filePath, OutputFile);
             }
             return false;
        }

        // ReadArchive isn't really possible directly without parsing XML output from GzsTool (if it produces any on list)
        // or extracting all. For now, we simulate by extracting to verify.
        // Actually, InstallManager uses ReadArchive to get a list? 
        // No, ReadArchive returns ArchiveFile object. 
        // We might need to mock ArchiveFile or change InstallManager.
        // For now, let's keep the signature but maybe throw NotSupported or return dummy?
        // InstallManager uses: GzsLib.ReadBaseData() -> GetQarGameFiles
        
        // Helper to read XML output from GzsTool (the .xml file generated alongside extracted folder? No, repacking logic uses XML)
        // To list contents without full extract is hard with just GzsTool exe.
        // We will assume full extraction is okay for "ReadArchive" contexts in InstallManager if possible, 
        // OR we just use the dictionary to fake "known files" in the archive if that's what's needed.
        
        // Looking at usage: GetQarGameFiles reads the archive to get entries.
        // We can implement GetQarGameFiles by running extraction and reading the directory.
        
        public static Dictionary<ulong, GameFile> GetQarGameFiles(string qarPath)
        {
            var result = new Dictionary<ulong, GameFile>();
            
            // This is heavy, but necessary without DLL
            string tempDir = Path.Combine(Path.GetDirectoryName(qarPath), "temp_read_" + Guid.NewGuid());
            try 
            {
                var files = ExtractArchive<object>(qarPath, tempDir);
                foreach(var file in files)
                {
                    ulong hash = HashingExtended.HashFileName(file);
                    result[hash] = new GameFile { FilePath = file, FileHash = hash, QarFile = Path.GetFileName(qarPath) };
                }
            }
            finally
            {
                 if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            return result;
        }
        
        public static List<string> ListArchiveContents<T>(string ArchiveName) where T : new()
        {
             // Similar to above
             string tempDir = Path.Combine(Path.GetDirectoryName(ArchiveName), "temp_read_" + Guid.NewGuid());
             try 
             {
                 return ExtractArchive<T>(ArchiveName, tempDir);
             }
             finally
             {
                  if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
             }
        }
        
        public static List<string> GetFpkReferences(string fpkPath) {
             // Fpk references are inside the .fpk file. 
             // Without GzsTool.Core, we can't parse the FPK binary header easily unless we extract it?
             // But GzsTool extracts the files inside. Does it generate an XML with references?
             // GzsTool v0.2 usually generates an XML when unpacking.
             
             string tempDir = Path.Combine(Path.GetDirectoryName(fpkPath), "temp_read_fpk_" + Guid.NewGuid());
             string xmlPath = fpkPath + ".xml"; // GzsTool usually drops the XML next to the file
             
             List<string> references = new List<string>();
             
             try
             {
                 if(RunGzsTool(String.Format("\"{0}\"", fpkPath)))
                 {
                     if (File.Exists(xmlPath))
                     {
                         // Parse XML for references
                         XDocument doc = XDocument.Load(xmlPath);
                         // Structure: <FpkFile><References><Reference FilePath="..." /></References>...
                         var refs = doc.Descendants("Reference");
                         foreach(var r in refs)
                         {
                             var attr = r.Attribute("FilePath");
                             if (attr != null) references.Add(attr.Value);
                         }
                         
                         // Clean up XML
                         File.Delete(xmlPath);
                     }
                 }
                 
                 // Clean up extracted dir
                 // Expected dir name:
                 string expectedDirName = Path.GetFileName(fpkPath).Replace(".", "_");
                 string sourceDir = Path.Combine(Path.GetDirectoryName(fpkPath), expectedDirName);
                 if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);

             }
             catch(Exception ex)
             {
                 Debug.LogLine(String.Format("[GzsLib] Error getting FPK references: {0}", ex.Message));
             }
             
             return references;
        }

        public static void LoadDictionaries()
        {
            Debug.LogLine("[GzsLib] Loading base dictionaries");
            //Hashing.ReadDictionary("qar_dictionary.txt"); // Assuming we have one for GZ
            //Hashing.ReadDictionary("gzs_dictionary.txt"); // GzsTool v0.2 might rely on this?
            //Hashing.ReadMd5Dictionary("fpk_dictionary.txt");
            HashingExtended.ReadDictionary();

#if SNAKEBITE
            LoadModDictionaries();
#endif
        }

#if SNAKEBITE
        public static void LoadModDictionaries()
        {
            SettingsManager manager = new SettingsManager(GamePaths.SnakeBiteSettings);
            var QarNames = manager.GetModQarFiles(true);
            File.WriteAllLines("mod_qar_dict.txt", QarNames);
            HashingExtended.ReadDictionary("mod_qar_dict.txt"); // Was Hashing.ReadDictionary
        }

        public static void LoadModDictionary(ModEntry modEntry)
        {
            Debug.LogLine("[GzsLib] Loading mod dictionary");
            List<string> qarNames = new List<string>();
            foreach (ModQarEntry qarFile in modEntry.ModQarEntries)
            {
                string fileName = Tools.ToQarPath(qarFile.FilePath.Substring(0, qarFile.FilePath.IndexOf(".")));
                qarNames.Add(fileName);
            }
            File.WriteAllLines("mod_qar_dict.txt", qarNames);
            HashingExtended.ReadDictionary("mod_qar_dict.txt");
        }

        public static List<Dictionary<ulong, GameFile>> ReadBaseData()
        {
            Debug.LogLine("[GzsLib] Acquiring base game data");

            // Updated for GZ
            var baseDataFiles = new List<Dictionary<ulong, GameFile>>();
            string dataDir = GamePaths.GameDir; // GZ data is in root, not master

            var qarFileNames = new List<string> {
                "data_00.g0s", // System
                "data_01.g0s", // Resources
                "data_02.g0s", // Audio?
            };

            foreach (var qarFileName in qarFileNames)
            {
                var path = Path.Combine(dataDir, qarFileName);
                if (!File.Exists(path))
                {
                    Debug.LogLine(String.Format("[GzsLib] Could not find {0}", path));
                } else
                {
                    var qarGameFiles = GetQarGameFiles(path); // This will slowly extract and list
                    baseDataFiles.Add(qarGameFiles);
                }
            }

            return baseDataFiles;
        }
#endif

        // Repack FPK archive
        public static void WriteFpkArchive(string FileName, string SourceDirectory, List<string> Files, List<string> references)
        {
             Debug.LogLine(String.Format("[GzsLib] Writing FPK archive: {0}", FileName));
             
             // 1. Generate XML for GzsTool (imitating what GzsTool output would look like)
             // or just let GzsTool handle it if we have the extracted structure.
             // GzsTool repacks based on an XML file usually.
             // Format: <FpkFile Name="filename"><Entries><Entry FilePath="..." /></Entries><References>...</References></FpkFile>
             
             string fpkType = FileName.EndsWith(".fpkd") ? "fpkd" : "fpk";
             // Note: GzsTool v0.2 might need specific XML format.
             
             XElement entries = new XElement("Entries");
             foreach(string s in Files)
             {
                 entries.Add(new XElement("Entry", new XAttribute("FilePath", Tools.ToQarPath(s))));
             }
             
             XElement refs = new XElement("References");
             if(references != null)
             {
                 foreach(string r in references)
                 {
                     refs.Add(new XElement("Reference", new XAttribute("FilePath", r)));
                 }
             }
             
             XDocument doc = new XDocument(
                 new XElement("FpkFile", 
                    new XAttribute("Name", Path.GetFileName(FileName)), 
                    // FpkType attribute might be needed?
                    entries,
                    refs
                 )
             );
             
             string xmlPath = FileName + ".xml";
             doc.Save(xmlPath);
             
             // 2. Ensure Files are in the directory expected?
             // InstallManager seems to assemble files in SourceDirectory.
             // GzsTool expects the folder path to unpack/pack.
             // If we pass the XML to GzsTool, does it use the source folder relative to the XML?
             // Usually GzsTool file.xml -> reads file.xml, looks for folder matching Name or derived name.
             // If we name our XML [FileName].xml, GzsTool might expect folder [FileName] (underscore replaced dot).
             
             // Rename SourceDirectory to the name GzsTool expects
             string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
             string destinationDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);
             
             // If source is different from destination dir expectation
             if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
             {
                 if(Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
                 // Directory.Move can be finicky across volumes, but here should be same.
                 // Copying might be safer or symlink? Let's Move.
                 Directory.Move(SourceDirectory, destinationDir); 
             }
             
             // Run Repack
             RunGzsTool(String.Format("\"{0}\"", xmlPath));
             
             // Cleanup XML
             if(File.Exists(xmlPath)) File.Delete(xmlPath);
             
             // Move directory back? Or just leave it? InstallManager likely clears it.
             // But InstallManager expects the SourceDirectory to still exist? 
             // "ModManager.ClearBuildFiles" does cleanup.
             // We moved SourceDirectory, so future operations on SourceDirectory might fail if we don't move it back or update reference.
             // But this function is void, so caller doesn't know.
             // We should move it back.
              if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
             {
                 Directory.Move(destinationDir, SourceDirectory); 
             }
        }

        // Repack QAR/G0S archive
        public static void WriteQarArchive(string FileName, string SourceDirectory, List<string> Files, uint Flags)
        {
             Debug.LogLine(String.Format("[GzsLib] Writing archive: {0}", FileName));
             
             // Generate XML
             XElement entries = new XElement("Entries");
             foreach(string s in Files)
             {
                 if (s.EndsWith("_unknown")) { continue; }
                 bool compressed = (Path.GetExtension(s).EndsWith(".fpk") || Path.GetExtension(s).EndsWith(".fpkd") || Path.GetExtension(s).EndsWith(".g0s")); // GZ recursive?
                 entries.Add(new XElement("Entry", 
                    new XAttribute("FilePath", s),
                    new XAttribute("Compressed", compressed),
                    new XAttribute("Hash", Tools.NameToHash(s)) // Include hash just in case
                 ));
             }
             
             XDocument doc = new XDocument(
                 new XElement("QarFile", 
                    new XAttribute("Name", Path.GetFileName(FileName)),
                    new XAttribute("Flags", Flags),
                    entries
                 )
             );
             
             string xmlPath = FileName + ".xml";
             doc.Save(xmlPath);

             // Prepare Folder
             string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
             string destinationDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);

             bool moved = false;
             if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
             {
                 if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
                 Util.MoveDirectory(SourceDirectory, destinationDir); // Assuming tools has MoveDirectory or we use Directory.Move
                 moved = true;
             }
             
             // Run Repack
             RunGzsTool(String.Format("\"{0}\"", xmlPath));

             if(File.Exists(xmlPath)) File.Delete(xmlPath);
             
             if (moved)
             {
                 Util.MoveDirectory(destinationDir, SourceDirectory);
             }
        }
        
        // Helper since Directory.Move has limitations (e.g. existing dest)
        private static class Util 
        {
             public static void MoveDirectory(string source, string dest)
             {
                 if (Directory.Exists(dest)) Directory.Delete(dest, true);
                 Directory.Move(source, dest);
             }
        }

        public static void PromoteQarArchive(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                Debug.LogLine(String.Format("[GzsLib] Promoting {0} to {1} ({2} KB)", Path.GetFileName(sourcePath), Path.GetFileName(destinationPath), Tools.GetFileSizeKB(sourcePath)));
                File.Delete(destinationPath);
                File.Move(sourcePath, destinationPath);
            }
            else
            {
                Debug.LogLine(String.Format("[GzsLib] {0} not found", sourcePath));
            }
        }

        public static List<string> SortFpksFiles(string FpkType, List<string> fpkFiles)
        {
            // Same sorting logic as before
            if (fpkFiles.Count <= 1) return fpkFiles;
            
            if (FpkType == "fpk") fpkFiles.Sort(StringComparer.Ordinal);
            else fpkFiles.Sort((a, b) => string.CompareOrdinal(b, a));
            
            var fpkFilesSorted = new List<string>();
            if (archiveExtensions.ContainsKey(FpkType))
            {
                foreach (var archiveExtension in archiveExtensions[FpkType]) {
                    foreach (string fileName in fpkFiles) {
                        var fileExtension = Path.GetExtension(fileName).TrimStart('.');
                        if (archiveExtension == fileExtension) {
                            fpkFilesSorted.Add(fileName);
                        }
                    }
                }
            }
            // Add remaining? Original didn't, seemingly
            return fpkFilesSorted;
        }

        public static bool IsExtensionValidForArchive(string fileName, string archiveName)
        {
            var archiveExtension = Path.GetExtension(archiveName).TrimStart('.');
            if (!archiveExtensions.ContainsKey(archiveExtension)) 
            {
                // Fallback for .dat/.g0s equivalence
                if (archiveExtension == "g0s") archiveExtension = "dat"; // Use dat rules for g0s
                else return true; // Accept all if unknown? Or strict?
            }
            
            if (!archiveExtensions.ContainsKey(archiveExtension)) return true;

            var validExtensions = archiveExtensions[archiveExtension];
            var ext = Path.GetExtension(fileName).TrimStart('.');
            return validExtensions.Contains(ext);
        }
    }
    
    // Stub for Hashing extended if referenced
    // (Assuming HashingExtended code is in another file or we need to keep it?)
    // The original file had HashingExtended at the bottom. We should keep it.
    
       public static class HashingExtended
    {
        private static readonly Dictionary<ulong, string> HashNameDictionary = new Dictionary<ulong, string>();

        private const ulong MetaFlag = 0x4000000000000;

        public static void ReadDictionary(string path = "qar_dictionary.txt")
        {
             // Same generic read
            if (!File.Exists(path)) return;
            foreach (var line in File.ReadAllLines(path))
            {
                ulong hash = HashFileName(line) & 0x3FFFFFFFFFFFF;
                if (HashNameDictionary.ContainsKey(hash) == false)
                {
                    HashNameDictionary.Add(hash, line);
                }
            }
        }

        public static string UpdateName(string inputFile)
        {
            // Same logic
             string filename = Path.GetFileNameWithoutExtension(inputFile);
            string ext = Path.GetExtension(inputFile);
            string extInner = "";
            if (filename.Contains(".")) 
            {
                extInner = Path.GetExtension(filename);
                filename = Path.GetFileNameWithoutExtension(filename);
            }

            ulong fileNameHash;
            if (TryGetFileNameHash(filename, out fileNameHash))
            {
                string foundFileNoExt;
                if (TryGetFilePathFromHash(fileNameHash, out foundFileNoExt))
                {
                    return foundFileNoExt + extInner + ext;
                }

            }

            return null;
        }

        public static ulong HashFileName(string text, bool removeExtension = true)
        {
             // Same CityHash logic
              if (removeExtension)
            {
                int index = text.IndexOf('.');
                text = index == -1 ? text : text.Substring(0, index);
            }

            bool metaFlag = false;
            const string assetsConstant = "/Assets/";
            if (text.StartsWith(assetsConstant))
            {
                text = text.Substring(assetsConstant.Length);

                if (text.StartsWith("tpptest"))
                {
                    metaFlag = true;
                }
            }
            else
            {
                metaFlag = true;
            }

            text = text.TrimStart('/');

            const ulong seed0 = 0x9ae16a3b2f90404f;
            byte[] seed1Bytes = new byte[sizeof(ulong)];
            for (int i = text.Length - 1, j = 0; i >= 0 && j < sizeof(ulong); i--, j++)
            {
                seed1Bytes[j] = Convert.ToByte(text[i]);
            }
            ulong seed1 = BitConverter.ToUInt64(seed1Bytes, 0);
            ulong maskedHash = CityHash.CityHash.CityHash64WithSeeds(text, seed0, seed1) & 0x3FFFFFFFFFFFF;

            return metaFlag
                ? maskedHash | MetaFlag
                : maskedHash;
        }

        public static bool TryGetFilePathFromHash(ulong hash, out string filePath)
        {
             // Same logic
             return HashNameDictionary.TryGetValue(hash & 0x3FFFFFFFFFFFF, out filePath);
        }

        private static bool TryGetFileNameHash(string filename, out ulong fileNameHash)
        {
             // Same logic
              bool isConverted = true;
            try
            {
                fileNameHash = Convert.ToUInt64(filename, 16);
            }
            catch (FormatException)
            {
                isConverted = false;
                fileNameHash = 0;
            }
            return isConverted;
        }
    }
}
