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
            {"g0s", "GzsFile"}, // Treat g0s as GzsFile
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
                string expectedDirNameAlt = Path.GetFileNameWithoutExtension(FileName);
                
                string sourceDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);

                // Check paths
                if (!Directory.Exists(sourceDir))
                {
                    // Check alt name in source dir
                    string sourceDirAlt = Path.Combine(Path.GetDirectoryName(FileName), expectedDirNameAlt);
                    if (Directory.Exists(sourceDirAlt))
                    {
                        sourceDir = sourceDirAlt;
                    }
                    else
                    {
                        // Check BaseDirectory with original name
                        string baseDirSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expectedDirName);
                        if (Directory.Exists(baseDirSource))
                        {
                            sourceDir = baseDirSource;
                        }
                        else 
                        {
                             // Check BaseDirectory with alt name
                            string baseDirSourceAlt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expectedDirNameAlt);
                            if (Directory.Exists(baseDirSourceAlt))
                            {
                                sourceDir = baseDirSourceAlt;
                            }
                        }
                    }
                }

                if (Directory.Exists(sourceDir))
                {
                    if (Path.GetFullPath(sourceDir).TrimEnd('\\') != Path.GetFullPath(OutputPath).TrimEnd('\\'))
                    {
                        Util.MoveDirectory(sourceDir, OutputPath);
                    }
                    
                    // List all files
                    return Directory.GetFiles(OutputPath, "*", SearchOption.AllDirectories)
                                    .Select(f => f.Replace(OutputPath + "\\", "").Replace("\\", "/")) // Relative paths
                                    .ToList();
                }
                else
                {
                    Debug.LogLine(String.Format("[GzsLib] Expected output directory not found: {0} or {1}", expectedDirName, expectedDirNameAlt));
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
                    ulong hash = HashingExtended.HashFileName(Tools.ToQarPath(file));
                    result[hash] = new GameFile { FilePath = Tools.ToQarPath(file), FileHash = hash, QarFile = Path.GetFileName(qarPath) };
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
            
            // GZ: Merge into qar_dictionary.txt so GzsTool can resolve mod paths during extraction
            MergeDictionaries("qar_dictionary.txt", "mod_qar_dict.txt");
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

            // GZ: Merge into qar_dictionary.txt so GzsTool can resolve mod paths during extraction
            MergeDictionaries("qar_dictionary.txt", "mod_qar_dict.txt");
        }

        private static void MergeDictionaries(string mainDict, string modDict)
        {
            try
            {
                if (!File.Exists(mainDict)) File.Create(mainDict).Close();
                if (!File.Exists(modDict)) return;

                var mainLines = new HashSet<string>(File.ReadAllLines(mainDict));
                var modLines = File.ReadAllLines(modDict);
                bool changed = false;

                foreach (var line in modLines)
                {
                    if (!mainLines.Contains(line))
                    {
                        mainLines.Add(line);
                        changed = true;
                    }
                }

                if (changed)
                {
                    File.WriteAllLines(mainDict, mainLines);
                    Debug.LogLine(string.Format("[GzsLib] Updated {0} with mod paths.", mainDict));
                }
            }
            catch (Exception ex)
            {
                Debug.LogLine(string.Format("[GzsLib] Error merging dictionaries: {0}", ex.Message));
            }
        }

        public static List<Dictionary<ulong, GameFile>> ReadBaseData(bool read00 = false, bool read01 = false, bool read02 = false)
        {
            Debug.LogLine("[GzsLib] Acquiring base game data");

            // Updated for GZ
            var baseDataFiles = new List<Dictionary<ulong, GameFile>>();
            string dataDir = GamePaths.GameDir; // GZ data is in root, not master

            var qarFileNames = new List<string>();
            // if(read00) qarFileNames.Add("data_00.g0s");
            if(read01) qarFileNames.Add("data_01.g0s");
            if(read02) qarFileNames.Add("data_02.g0s");

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
             
             string fpkType = FileName.EndsWith(".fpkd") ? "Fpkd" : "Fpk"; // Type enum string match
             string xsiType = "FpkFile";

             Files = SortFpksFiles(fpkType.ToLower(), Files);

             XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
             XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

             XElement entries = new XElement("Entries");
             foreach(string s in Files)
             {
                 entries.Add(new XElement("Entry", new XAttribute("FilePath", Tools.ToQarPath(s).TrimStart('/'))));
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
                 new XElement("ArchiveFile",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                    new XAttribute("Name", Path.GetFileName(FileName)),
                    new XAttribute(xsi + "type", xsiType),
                    new XAttribute("FpkType", fpkType),
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
             
             // Prepare Folder
             bool moved = false;
             try
             {
                 if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
                 {
                     if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
                     // Directory.Move can be finicky across volumes, but here should be same.
                     // Copying might be safer or symlink? Let's Move.
                     Util.MoveDirectory(SourceDirectory, destinationDir); 
                     moved = true;
                 }
                 
                 // Run Repack
                 RunGzsTool(String.Format("\"{0}\"", xmlPath));
             }
             finally
             {
                 // Cleanup XML
                 if(File.Exists(xmlPath)) File.Delete(xmlPath);
                 
                 // Move directory back
                 if (moved)
                 {
                     if (Directory.Exists(destinationDir))
                     {
                         Util.MoveDirectory(destinationDir, SourceDirectory);
                     }
                     else
                     {
                         // If destination dir is gone, but we moved it there, we have strict data loss involved or GzsTool consumed it.
                         // But we should try to restore if possible.
                         Debug.LogLine("[GzsLib] Warning: Input directory disappeared after GzsTool run. Cannot restore SourceDirectory.");
                     }
                 }
             }
        }

        // Repack QAR/G0S archive
        public static void WriteQarArchive(string FileName, string SourceDirectory, List<string> Files, uint Flags, string CustomXmlPath = null)
        {
             Debug.LogLine(String.Format("[GzsLib] Writing archive: {0}", FileName));
             
             XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
             XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
             string xsiType = "QarFile";

             if(FileName.Contains(".g0s")) xsiType = "GzsFile"; // Update for GZ compatibility

             // Use safe name for XML and Directory to avoid collisions (e.g. with data_02.g0s file)
             string safeName = Path.GetFileName(FileName).Replace(".", "_");

             // Prepare Folder Name
             string expectedDirName = safeName;
             string destinationDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);

             string folderName = safeName;
             string xmlName = safeName + ".g0s";

             // Generate XML
             XElement entries = new XElement("Entries");
             foreach(string s in Files)
             {
                 if (s.EndsWith("_unknown")) { continue; }
                 bool compressed = (Path.GetExtension(s).EndsWith(".fpk") || Path.GetExtension(s).EndsWith(".fpkd") || Path.GetExtension(s).EndsWith(".g0s")); 
                 entries.Add(new XElement("Entry", 
                    new XAttribute("FilePath", Tools.ToQarPath(s)),
                    new XAttribute("Compressed", compressed)
                 ));
             }

             // Merge Custom XML Entries
             if (!string.IsNullOrEmpty(CustomXmlPath) && File.Exists(CustomXmlPath))
             {
                 Debug.LogLine(String.Format("[GzsLib] Merging custom XML entries from: {0}", CustomXmlPath));
                 try 
                 {
                     XDocument customDoc = XDocument.Load(CustomXmlPath);
                     if (customDoc.Root != null)
                     {
                         // Support both raw list of Entries or full ArchiveFile structure
                         var customEntries = customDoc.Descendants("Entry"); 
                         foreach(var entry in customEntries)
                         {
                             // Deduplication logic could go here, but GzsTool might handle overrides.
                             // For safety, let's remove existing entry if path matches.
                             string path = (string)entry.Attribute("FilePath");
                             if(path != null)
                             {
                                 var existing = entries.Elements("Entry").FirstOrDefault(e => (string)e.Attribute("FilePath") == path);
                                 if(existing != null) existing.Remove();
                                 
                                 entries.Add(entry);
                             }
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     Debug.LogLine(String.Format("[GzsLib] Error merging custom XML: {0}", ex.Message));
                 }
             }
             
             XDocument doc = new XDocument(
                 new XElement("ArchiveFile", 
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                    new XAttribute("Name", xmlName), // Output File Name
                    new XAttribute(xsi + "type", xsiType),
                    new XAttribute("Flags", Flags),
                    entries
                 )
             );
             
             string xmlPath = Path.Combine(Path.GetDirectoryName(FileName), safeName + ".xml");
             
             doc.Save(xmlPath);

             // Prepare Folder
             bool moved = false;
             try
             {
                 if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
                 {
                     if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
                     Util.MoveDirectory(SourceDirectory, destinationDir); 
                     moved = true;
                 }
                 
                 string tempOutputPath = Path.Combine(Path.GetDirectoryName(FileName), xmlName);
                 if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);

                 // Run Repack
                 RunGzsTool(String.Format("\"{0}\"", xmlPath));

                 if(File.Exists(xmlPath)) File.Delete(xmlPath);
                 
                 // Output should be xmlName (safeName.g0s). Rename to FileName.
                 if (File.Exists(tempOutputPath))
                 {
                     int retries = 5;
                     while (retries > 0)
                     {
                         try
                         {
                             if (File.Exists(FileName)) File.Delete(FileName);
                             File.Move(tempOutputPath, FileName);
                             break;
                         }
                         catch (Exception)
                         {
                             retries--;
                             System.Threading.Thread.Sleep(500);
                             if (retries == 0) throw;
                         }
                     }
                 }
             }
             finally
             {
                 if (moved)
                 {
                     if (Directory.Exists(destinationDir))
                     {
                         Util.MoveDirectory(destinationDir, SourceDirectory);
                     }
                     else
                     {
                         Debug.LogLine("[GzsLib] Warning: Input directory disappeared after GzsTool run. Cannot restore SourceDirectory.");
                     }
                 }
             }


        }
        
        // Helper since Directory.Move has limitations (e.g. existing dest)
        private static class Util 
        {
             public static void MoveDirectory(string source, string dest)
             {
                 int retries = 5;
                 while (retries > 0)
                 {
                     try
                     {
                         if (Directory.Exists(dest)) Directory.Delete(dest, true);
                         Directory.Move(source, dest);
                         return;
                     }
                     catch (IOException)
                     {
                         retries--;
                         System.Threading.Thread.Sleep(200);
                         if (retries == 0) throw;
                     }
                 }
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
            var sortedSet = new HashSet<string>();

            if (archiveExtensions.ContainsKey(FpkType))
            {
                foreach (var archiveExtension in archiveExtensions[FpkType]) {
                    foreach (string fileName in fpkFiles) {
                        var fileExtension = Path.GetExtension(fileName).TrimStart('.');
                        if (archiveExtension == fileExtension) {
                            fpkFilesSorted.Add(fileName);
                            sortedSet.Add(fileName);
                        }
                    }
                }
            }
            
            // GZ: Add remaining files that weren't in the sort list
            foreach (var fileName in fpkFiles)
            {
                if (!sortedSet.Contains(fileName))
                {
                    fpkFilesSorted.Add(fileName);
                }
            }

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
