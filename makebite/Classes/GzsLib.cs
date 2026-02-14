// SYNC to makebite
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
            {"g0s", "QarFile"},
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
        public static List<string> ExtractArchive<T>(string FileName, string OutputPath) where T : new() 
        {
            if (!File.Exists(FileName))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", FileName));
                throw new FileNotFoundException();
            }

            string name = Path.GetFileName(FileName);
            Debug.LogLine(String.Format("[GzsLib] Extracting {0} to {1} ({2} KB)", name, OutputPath, Tools.GetFileSizeKB(FileName)));

            if(RunGzsTool(String.Format("\"{0}\"", FileName)))
            {
                string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
                string sourceDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);

                if (Directory.Exists(sourceDir))
                {
                    if (Path.GetFullPath(sourceDir).TrimEnd('\\') != Path.GetFullPath(OutputPath).TrimEnd('\\'))
                    {
                        if (Directory.Exists(OutputPath)) Directory.Delete(OutputPath, true);
                        Directory.Move(sourceDir, OutputPath);
                    }
                    
                    return Directory.GetFiles(OutputPath, "*", SearchOption.AllDirectories)
                                    .Select(f => f.Replace(OutputPath + "\\", "").Replace("\\", "/")) 
                                    .ToList();
                }
                else
                {
                    Debug.LogLine(String.Format("[GzsLib] Expected output directory not found: {0}", sourceDir));
                }
            }
            
            return new List<string>();
        }

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

        public static bool ExtractFileByHash<T>(string SourceArchive, ulong FileHash, string OutputFile) where T : new()
        {
             string filePath;
             if (HashingExtended.TryGetFilePathFromHash(FileHash, out filePath))
             {
                 return ExtractFile<T>(SourceArchive, filePath, OutputFile);
             }
             return false;
        }

        public static Dictionary<ulong, GameFile> GetQarGameFiles(string qarPath)
        {
            var result = new Dictionary<ulong, GameFile>();
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
             string tempDir = Path.Combine(Path.GetDirectoryName(fpkPath), "temp_read_fpk_" + Guid.NewGuid());
             string xmlPath = fpkPath + ".xml"; 
             
             List<string> references = new List<string>();
             
             try
             {
                 if(RunGzsTool(String.Format("\"{0}\"", fpkPath)))
                 {
                     if (File.Exists(xmlPath))
                     {
                         XDocument doc = XDocument.Load(xmlPath);
                         var refs = doc.Descendants("Reference");
                         foreach(var r in refs)
                         {
                             var attr = r.Attribute("FilePath");
                             if (attr != null) references.Add(attr.Value);
                         }
                         File.Delete(xmlPath);
                     }
                 }
                 
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
                // "data_00.g0s",  // tpp trailer, wmv file 
                "data_01.g0s", 
                "data_02.g0s",
            };

            foreach (var qarFileName in qarFileNames)
            {
                var path = Path.Combine(dataDir, qarFileName);
                if (!File.Exists(path))
                {
                    Debug.LogLine(String.Format("[GzsLib] Could not find {0}", path));
                } else
                {
                    var qarGameFiles = GetQarGameFiles(path); 
                    baseDataFiles.Add(qarGameFiles);
                }
            }

            return baseDataFiles;
        }
#endif

        public static void WriteFpkArchive(string FileName, string SourceDirectory, List<string> Files, List<string> references)
        {
             Debug.LogLine(String.Format("[GzsLib] Writing FPK archive: {0}", FileName));
             
             string fpkType = FileName.EndsWith(".fpkd") ? "fpkd" : "fpk";
             
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
                    entries,
                    refs
                 )
             );
             
             string xmlPath = FileName + ".xml";
             doc.Save(xmlPath);
             
             string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
             string destinationDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);
             
             if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
             {
                 if(Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
                 Directory.Move(SourceDirectory, destinationDir); 
             }
             
             RunGzsTool(String.Format("\"{0}\"", xmlPath));
             
             if(File.Exists(xmlPath)) File.Delete(xmlPath);
             
              if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
             {
                 Directory.Move(destinationDir, SourceDirectory); 
             }
        }

        public static void WriteQarArchive(string FileName, string SourceDirectory, List<string> Files, uint Flags)
        {
             Debug.LogLine(String.Format("[GzsLib] Writing archive: {0}", FileName));
             
             XElement entries = new XElement("Entries");
             foreach(string s in Files)
             {
                 if (s.EndsWith("_unknown")) { continue; }
                 bool compressed = (Path.GetExtension(s).EndsWith(".fpk") || Path.GetExtension(s).EndsWith(".fpkd") || Path.GetExtension(s).EndsWith(".g0s")); 
                 entries.Add(new XElement("Entry", 
                    new XAttribute("FilePath", s),
                    new XAttribute("Compressed", compressed),
                    new XAttribute("Hash", Tools.NameToHash(s)) 
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

             string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
             string destinationDir = Path.Combine(Path.GetDirectoryName(FileName), expectedDirName);

             bool moved = false;
             if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
             {
                 if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
                 Util.MoveDirectory(SourceDirectory, destinationDir); 
                 moved = true;
             }
             
             RunGzsTool(String.Format("\"{0}\"", xmlPath));

             if(File.Exists(xmlPath)) File.Delete(xmlPath);
             
             if (moved)
             {
                 Util.MoveDirectory(destinationDir, SourceDirectory);
             }
        }
        
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
            return fpkFilesSorted;
        }

        public static bool IsExtensionValidForArchive(string fileName, string archiveName)
        {
            var archiveExtension = Path.GetExtension(archiveName).TrimStart('.');
            if (!archiveExtensions.ContainsKey(archiveExtension)) 
            {
                if (archiveExtension == "g0s") archiveExtension = "dat"; 
                else return true; 
            }
            
            if (!archiveExtensions.ContainsKey(archiveExtension)) return true;

            var validExtensions = archiveExtensions[archiveExtension];
            var ext = Path.GetExtension(fileName).TrimStart('.');
            return validExtensions.Contains(ext);
        }
    }
    
       public static class HashingExtended
    {
        private static readonly Dictionary<ulong, string> HashNameDictionary = new Dictionary<ulong, string>();

        private const ulong MetaFlag = 0x4000000000000;

        public static void ReadDictionary(string path = "qar_dictionary.txt")
        {
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
             return HashNameDictionary.TryGetValue(hash & 0x3FFFFFFFFFFFF, out filePath);
        }

        private static bool TryGetFileNameHash(string filename, out ulong fileNameHash)
        {
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