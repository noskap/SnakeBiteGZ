// SYNC to makebite
#define SNAKEBITE //TODO bad
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Fpk;
using GzsTool.Core.Qar;
using GzsTool.Core.Utility;
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
        public static uint zeroFlags = 3150304;
        public static uint oneFlags = 3150048;
        public static uint chunk0Flags = 3150304;
        public static uint chunk7Flags = 3150304;
        public static uint texture7Flags = 3150304;

        private const string GzsToolExe = "GzsTool.exe";
        private static Dictionary<string, List<string>> archiveExtensions = new Dictionary<string, List<string>> {
            {"dat",new List<string> { // TPP legacy
                "bnk", "dat", "ffnt", "fmtt", "fpk", "fpkd", "fsm", "fsop", "ftex", "ftexs",
                "json", "lua", "pftxs", "sbp", "subp", "wem", "xml"
            }},
            {"g0s",new List<string> { // GZ
                "bnk", "dat", "ffnt", "fmtt", "fpk", "fpkd", "fsm", "fsop", "ftex", "ftexs",
                "json", "lua", "pftxs", "sbp", "subp", "wem", "xml"
            }},
            {"fpk",new List<string> {
                "caar", "fnt", "atsh", "frig", "adm", "frt", "fpkl", "fsm", "ftdp", "geobv",
                "ftex", "geoms", "gimr", "gpfp", "grxla", "grxoc", "htre", "lba", "lpsh", "mog",
                "mtar", "nav2", "nta", "rdf", "ends", "sand", "mbl", "tcvp", "spch", "trap",
                "uigb", "uilb", "pcsp", "tre2", "fstb", "twpf", "fv2t", "fmdl", "geom", "gskl",
                "fcnp", "frdv", "fdes", "fclo", "uif", "uia", "subp", "sani", "ladb", "frl",
                "fv2", "obr", "lng2", "mtard", "obrb", "dfrm", "lani", "lad", "gani", "fova", "vfxdb", "xml"
            }},
            {"fpkd",new List<string> {
                "fox2", "evf", "parts", "vfxlb", "vfx", "vfxlf", "veh", "frld", "des", "bnd",
                "tgt", "phsd", "ph", "sim", "clo", "fsd", "sdf", "lua", "lng", "lani", "lad", "gani", "fova", "vfxdb", "xml"
            }},
        };

        static Dictionary<string, string> extensionToType = new Dictionary<string, string> {
            {"dat", "QarFile"},
            {"g0s", "GzsFile"},
            {"fpk", "FpkFile" },
            {"fpkd", "FpkFile" },
        };

        private static bool IsGzsArchive(string fileName)
        {
            // Use Contains instead of EndsWith because files may have suffixes like .g0s.SB_Build
            return fileName.IndexOf(".g0s", StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

                process.CancelOutputRead();
                process.CancelErrorRead();

                if (outputLines.Count > 0) Debug.LogLine(String.Format("[GzsTool] {0}", string.Join(Environment.NewLine, outputLines)));
                if (errorLines.Count > 0) Debug.LogLine(String.Format("[GzsTool Error] {0}", string.Join(Environment.NewLine, errorLines)));

                int exitCode = process.ExitCode;
                process.Close();
                return exitCode == 0;
            }
        }

        private static List<string> ExtractArchiveViaCli(string FileName, string OutputPath)
        {
            string fullPath = Path.GetFullPath(FileName);

            if (RunGzsTool(String.Format("\"{0}\"", fullPath)))
            {
                // GzsTool v0.2 output folder naming convention: [Filename]_[Extension w/o dot]
                string expectedDirName = Path.GetFileName(FileName).Replace(".", "_");
                string expectedDirNameAlt = Path.GetFileNameWithoutExtension(FileName);
                
                string sourceDir = Path.Combine(Path.GetDirectoryName(fullPath), expectedDirName);

                // Check paths
                if (!Directory.Exists(sourceDir))
                {
                    string sourceDirAlt = Path.Combine(Path.GetDirectoryName(fullPath), expectedDirNameAlt);
                    if (Directory.Exists(sourceDirAlt))
                    {
                        sourceDir = sourceDirAlt;
                    }
                    else
                    {
                        string baseDirSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expectedDirName);
                        if (Directory.Exists(baseDirSource))
                        {
                            sourceDir = baseDirSource;
                        }
                        else 
                        {
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
                    
                    // Preserve the XML manifest generated by GzsTool so WriteFpkArchive can reuse its string tables/hashes
                    string xmlSource = fullPath + ".xml";
                    if (!File.Exists(xmlSource)) 
                    {
                        xmlSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(fullPath) + ".xml");
                    }
                    if (File.Exists(xmlSource))
                    {
                        string manifestDest = OutputPath.TrimEnd('\\') + ".fpk_manifest.xml";
                        File.Copy(xmlSource, manifestDest, true);
                        File.Delete(xmlSource); // Clean up the leftover XML in the source dir
                    }

                    return Directory.GetFiles(OutputPath, "*", SearchOption.AllDirectories)
                                    .Select(f => f.Replace(OutputPath + "\\", "").Replace("\\", "/"))
                                    .ToList();
                }
                else
                {
                    Debug.LogLine(String.Format("[GzsLib] Expected output directory not found: {0} or {1}", expectedDirName, expectedDirNameAlt));
                }
            }
            
            return new List<string>();
        }

        // CLI-based QAR/G0S repacking
        private static void WriteQarArchiveViaCli(string FileName, string SourceDirectory, List<string> Files, uint Flags)
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
            string xsiType = "QarFile";

            if (FileName.Contains(".g0s")) xsiType = "GzsFile";

            string safeName = Path.GetFileName(FileName).Replace(".", "_");
            string destinationDir = Path.Combine(Path.GetDirectoryName(FileName), safeName);
            string xmlName = safeName + ".g0s";

            // Generate XML
            XElement entries = new XElement("Entries");
            foreach (string s in Files)
            {
                if (s.EndsWith("_unknown")) { continue; }
                bool compressed = (Path.GetExtension(s).EndsWith(".fpk") || Path.GetExtension(s).EndsWith(".fpkd") || Path.GetExtension(s).EndsWith(".g0s")); 
                entries.Add(new XElement("Entry", 
                    new XAttribute("FilePath", Tools.ToQarPath(s)),
                    new XAttribute("Compressed", compressed)
                ));
            }
            
            XDocument doc = new XDocument(
                new XElement("ArchiveFile", 
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                    new XAttribute("Name", xmlName),
                    new XAttribute(xsi + "type", xsiType),
                    new XAttribute("Flags", Flags),
                    entries
                )
            );
            
            string xmlPath = Path.Combine(Path.GetDirectoryName(FileName), safeName + ".xml");
            doc.Save(xmlPath);

            bool moved = false;
            try
            {
                if (Path.GetFullPath(SourceDirectory) != Path.GetFullPath(destinationDir))
                {
                    if (Directory.Exists(destinationDir)) Util.DeleteDirectory(destinationDir);
                    Util.MoveDirectory(SourceDirectory, destinationDir); 
                    moved = true;
                }
                
                string tempOutputPath = Path.Combine(Path.GetDirectoryName(FileName), xmlName);
                if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);

                RunGzsTool(String.Format("\"{0}\"", Path.GetFullPath(xmlPath)));

                if (File.Exists(xmlPath)) File.Delete(xmlPath);
                
                if (File.Exists(tempOutputPath))
                {
                    int retries = 40; // 40 * 500ms = 20 seconds to allow AV to finish scanning a 1.6GB file
                    while (retries > 0)
                    {
                        try
                        {
                            if (File.Exists(FileName)) File.Delete(FileName);
                            File.Move(tempOutputPath, FileName);
                            break;
                        }
                        catch (IOException)
                        {
                            try {
                                // Fallback to copy/delete if Move fails due to cross-volume issues
                                if (File.Exists(FileName)) File.Delete(FileName);
                                File.Copy(tempOutputPath, FileName, true);
                                File.Delete(tempOutputPath);
                                break;
                            } catch (Exception) {
                                retries--;
                                System.Threading.Thread.Sleep(500);
                                if (retries == 0) throw;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            retries--;
                            System.Threading.Thread.Sleep(500);
                            if (retries == 0) throw;
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

        public static List<string> ExtractArchive<T>(string FileName, string OutputPath) where T : ArchiveFile, new()
        {
            if (!File.Exists(FileName))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", FileName));
                throw new FileNotFoundException();
            }

            string name = Path.GetFileName(FileName);
            Debug.LogLine(String.Format("[GzsLib] Extracting {0} to {1} ({2} KB)", name, OutputPath, Tools.GetFileSizeKB(FileName)));

            Debug.LogLine(String.Format("[GzsLib] Using CLI extraction for {0}", name));
            return ExtractArchiveViaCli(FileName, OutputPath);
        }

        public static bool ExtractFile<T>(string SourceArchive, string FilePath, string OutputFile) where T : ArchiveFile, new()
        {
            if (!File.Exists(SourceArchive))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", SourceArchive));
                throw new FileNotFoundException();
            }

            Debug.LogLine(String.Format("[GzsLib] Extracting file {1}: {0} -> {2}", FilePath, SourceArchive, OutputFile));

            if (IsGzsArchive(SourceArchive))
            {
                string tempDir = Path.Combine(Path.GetDirectoryName(SourceArchive), "temp_extract_" + Guid.NewGuid());
                try
                {
                    ExtractArchiveViaCli(SourceArchive, tempDir);
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
                    if (Directory.Exists(tempDir)) Util.DeleteDirectory(tempDir);
                }
            }

            ulong fileHash = Tools.NameToHash(FilePath);
            using (FileStream archiveFile = new FileStream(SourceArchive, FileMode.Open))
            {
                T archive = new T();
                archive.Name = Path.GetFileName(SourceArchive);
                archive.Read(archiveFile);

                var outFile = archive.ExportFiles(archiveFile).FirstOrDefault(entry => Tools.NameToHash(entry.FileName) == fileHash);
                if (outFile != null)
                {
                    string path = Path.GetDirectoryName(Path.GetFullPath(OutputFile));
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    using (FileStream outStream = new FileStream(OutputFile, FileMode.Create))
                    {
                        outFile.DataStream().CopyTo(outStream);
                    }
                    return true;
                }
                return false;
            }
        }

        public static bool ExtractFileByHash<T>(string SourceArchive, ulong FileHash, string OutputFile) where T : ArchiveFile, new()
        {
            if (!File.Exists(SourceArchive))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", SourceArchive));
                throw new FileNotFoundException();
            }

            Debug.LogLine(String.Format("[GzsLib] Extracting file from {1}: hash {0} -> {2}", FileHash, SourceArchive, OutputFile));

            if (IsGzsArchive(SourceArchive))
            {
                string filePath;
                if (HashingExtended.TryGetFilePathFromHash(FileHash, out filePath))
                {
                    return ExtractFile<T>(SourceArchive, filePath, OutputFile);
                }
                return false;
            }

            using (FileStream archiveFile = new FileStream(SourceArchive, FileMode.Open))
            {
                T archive = new T();
                archive.Name = Path.GetFileName(SourceArchive);
                archive.Read(archiveFile);

                var outFile = archive.ExportFiles(archiveFile).FirstOrDefault(entry => Tools.NameToHash(entry.FileName) == FileHash);
                if (outFile != null)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(OutputFile))) Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
                    using (FileStream outStream = new FileStream(OutputFile, FileMode.Create))
                    {
                        outFile.DataStream().CopyTo(outStream);
                    }
                    return true;
                }
                return false;
            }
        }

        public static T ReadArchive<T>(string FileName) where T : ArchiveFile, new()
        {
            if (!File.Exists(FileName))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", FileName));
                throw new FileNotFoundException();
            }

            string name = Path.GetFileName(FileName);
            Debug.LogLine(String.Format("[GzsLib] Reading {0}", name));

            using (FileStream archiveFile = new FileStream(FileName, FileMode.Open))
            {
                T archive = new T();
                archive.Name = Path.GetFileName(FileName);
                archive.Read(archiveFile);
                return archive;
            }
        }

        public static List<string> GetFpkReferences(string fpkPath)
        {
            var fpkReferences = new List<string>();
            FpkFile fpkFile = ReadArchive<FpkFile>(fpkPath);
            foreach (var reference in fpkFile.References)
            {
                fpkReferences.Add(reference.FilePath);
            }
            Debug.LogLine(String.Format("[GzsLib] GetFpkReferences: found {0} in {1}", fpkReferences.Count, fpkPath));
            return fpkReferences;
        }

        public static Dictionary<ulong, GameFile> GetQarGameFiles(string qarPath)
        {
            var qarGameFiles = new Dictionary<ulong, GameFile>();

            string qarName = Path.GetFileName(qarPath);
            Debug.LogLine(String.Format("[GzsLib] Getting game files from: {0}", qarName));

            string tempDir = Path.Combine(Path.GetDirectoryName(qarPath), "temp_read_" + Guid.NewGuid());
            try
            {
                List<string> files;
                if (IsGzsArchive(qarPath))
                {
                    files = ExtractArchiveViaCli(qarPath, tempDir);
                }
                else
                {
                    files = ExtractArchive<QarFile>(qarPath, tempDir);
                }
                
                foreach (var file in files)
                {
                    ulong hash = Tools.NameToHash(Tools.ToQarPath(file));
                    qarGameFiles[hash] = new GameFile { FilePath = Tools.ToQarPath(file), FileHash = hash, QarFile = qarName };
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Util.DeleteDirectory(tempDir);
            }
            return qarGameFiles;
        }

        public static List<string> ListArchiveContents<T>(string ArchiveName) where T : ArchiveFile, new()
        {
            if (!File.Exists(ArchiveName))
            {
                Debug.LogLine(String.Format("[GzsLib] File not found: {0}", ArchiveName));
                throw new FileNotFoundException();
            }

            string name = Path.GetFileName(ArchiveName);
            Debug.LogLine(String.Format("[GzsLib] Reading archive contents: {0}", name));

            if (IsGzsArchive(ArchiveName))
            {
                string tempDir = Path.Combine(Path.GetDirectoryName(ArchiveName), "temp_read_" + Guid.NewGuid());
                try
                {
                    return ExtractArchiveViaCli(ArchiveName, tempDir);
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Util.DeleteDirectory(tempDir);
                }
            }

            using (FileStream archiveFile = new FileStream(ArchiveName, FileMode.Open))
            {
                List<string> archiveContents = new List<string>();
                T archive = new T();
                archive.Name = Path.GetFileName(ArchiveName);
                archive.Read(archiveFile);
                foreach (var x in archive.ExportFiles(archiveFile))
                {
                    archiveContents.Add(x.FileName.TrimStart('/'));
                }
                return archiveContents;
            }
        }

        public static void LoadDictionaries()
        {
            Debug.LogLine("[GzsLib] Loading base dictionaries");
            Hashing.ReadDictionary("qar_dictionary.txt");
            Hashing.ReadMd5Dictionary("fpk_dictionary.txt");
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
            HashingExtended.ReadDictionary("mod_qar_dict.txt");

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
                    var qarGameFiles = GetQarGameFiles(path);
                    baseDataFiles.Add(qarGameFiles);
                }
            }

            return baseDataFiles;
        }
#endif

        public static void WriteFpkArchive(string FileName, string SourceDirectory, List<string> Files, List<string> references)
        {
            Debug.LogLine(String.Format("[GzsLib] Writing FPK archive (CLI): {0}", FileName));

            string fpkType = FileName.EndsWith(".fpkd") ? "fpkd" : "fpk";
            string fpkTypeAttr = FileName.EndsWith(".fpkd") ? "Fpkd" : "Fpk";

            string safeName = Path.GetFileName(FileName).Replace(".", "_");
            string fileDir = Path.GetDirectoryName(Path.GetFullPath(FileName));
            if (String.IsNullOrEmpty(fileDir)) fileDir = AppDomain.CurrentDomain.BaseDirectory;
            string xmlPath = Path.Combine(fileDir, safeName + ".xml");
            string destinationDir = Path.Combine(fileDir, safeName);
            string preservedXmlPath = SourceDirectory.TrimEnd('\\') + ".fpk_manifest.xml";
            XDocument doc = null;

            if (File.Exists(preservedXmlPath))
            {
                Debug.LogLine(String.Format("[GzsLib] Using preserved CLI XML manifest: {0}", preservedXmlPath));
                try
                {
                    doc = XDocument.Load(preservedXmlPath);
                    XElement archiveFile = doc.Root;
                    if (archiveFile != null)
                    {
                        XElement entries = archiveFile.Element("Entries");
                        if (entries != null)
                        {
                            HashSet<string> diskFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (string s in Files)
                            {
                                diskFiles.Add(Tools.ToQarPath(s).TrimStart('/'));
                            }
                            List<XElement> originalEntries = entries.Elements("Entry").ToList();
                            entries.RemoveNodes();

                            HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (XElement entry in originalEntries)
                            {
                                XAttribute filePathAttr = entry.Attribute("FilePath");
                                if (filePathAttr != null)
                                {
                                    string xmlPath2 = filePathAttr.Value.TrimStart('/');
                                    
                                    if (diskFiles.Contains(xmlPath2))
                                    {
                                        entries.Add(entry);
                                        processed.Add(xmlPath2);
                                    }
                                    else
                                    {
                                        string xmlFileName = Path.GetFileName(xmlPath2);
                                        string matchedDiskFile = null;
                                        foreach (string df in diskFiles)
                                        {
                                            if (!processed.Contains(df) && Path.GetFileName(df).Equals(xmlFileName, StringComparison.OrdinalIgnoreCase))
                                            {
                                                matchedDiskFile = df;
                                                break;
                                            }
                                        }
                                        if (matchedDiskFile != null)
                                        {
                                            entries.Add(entry);
                                            processed.Add(matchedDiskFile);
                                        }
                                    }
                                }
                            }
                            foreach (string s in Files)
                            {
                                string qPath = Tools.ToQarPath(s).TrimStart('/');
                                if (!processed.Contains(qPath))
                                {
                                    entries.Add(new XElement("Entry", new XAttribute("FilePath", "/" + qPath)));
                                    processed.Add(qPath);
                                    Debug.LogLine(String.Format("[GzsLib] New FPK entry (not in original XML): {0}", qPath));
                                }
                            }
                        }
                        XElement refsElement = archiveFile.Element("References");
                        if (refsElement != null) refsElement.RemoveNodes();
                        else refsElement = new XElement("References");
                        if (references != null)
                        {
                            foreach (string r in references)
                            {
                                refsElement.Add(new XElement("Reference", new XAttribute("FilePath", r)));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogLine(String.Format("[GzsLib] Failed to load preserved XML: {0}. Generating fresh.", ex.Message));
                    doc = null;
                }
            }
            if (doc == null)
            {
                Debug.LogLine("[GzsLib] No preserved CLI XML — generating fresh manifest");
                List<string> fpkFilesSorted = SortFpksFiles(fpkType, Files);

                XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
                XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

                XElement entries = new XElement("Entries");
                foreach (string s in fpkFilesSorted)
                {
                    string qPath = Tools.ToQarPath(s).TrimStart('/');
                    entries.Add(new XElement("Entry", new XAttribute("FilePath", "/" + qPath)));
                }

                XElement refs = new XElement("References");
                if (references != null)
                {
                    foreach (string r in references)
                    {
                        refs.Add(new XElement("Reference", new XAttribute("FilePath", r)));
                    }
                }

                doc = new XDocument(
                    new XElement("ArchiveFile",
                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                        new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                        new XAttribute("Name", Path.GetFileName(FileName)),
                        new XAttribute(xsi + "type", "FpkFile"),
                        new XAttribute("FpkType", fpkTypeAttr),
                        entries,
                        refs
                    )
                );
            }

            doc.Save(xmlPath);
            Debug.LogLine(String.Format("[GzsLib] FPK XML manifest saved: {0}", xmlPath));

            bool moved = false;
            try
            {
                string fullSourceDir = Path.GetFullPath(SourceDirectory);
                string fullDestDir = Path.GetFullPath(destinationDir);

                if (fullSourceDir.TrimEnd('\\') != fullDestDir.TrimEnd('\\'))
                {
                    if (Directory.Exists(destinationDir)) Util.DeleteDirectory(destinationDir);
                    Util.MoveDirectory(SourceDirectory, destinationDir);
                    moved = true;
                }
                if (!RunGzsTool(String.Format("\"{0}\"", Path.GetFullPath(xmlPath))))
                {
                    Debug.LogLine("[GzsLib] GzsTool.exe FPK repack failed!");
                }
            }
            finally
            {
                if (File.Exists(xmlPath)) File.Delete(xmlPath);
                if (moved)
                {
                    if (Directory.Exists(destinationDir))
                    {
                        Util.MoveDirectory(destinationDir, SourceDirectory);
                    }
                    else
                    {
                        Debug.LogLine("[GzsLib] Warning: FPK source directory disappeared after GzsTool run. Cannot restore SourceDirectory.");
                    }
                }
            }

            int entryCount = 0;
            if (doc.Root != null && doc.Root.Element("Entries") != null)
            {
                entryCount = doc.Root.Element("Entries").Elements("Entry").Count();
            }
            Debug.LogLine(String.Format("[GzsLib] FPK archive written (CLI): {0} ({1} entries, {2} references)",
                FileName, entryCount, references != null ? references.Count : 0));
        }

        // Export QAR/G0S archive — CLI-based for G0S (GzsTool.Core doesn't support GZ format)
        public static void WriteQarArchive(string FileName, string SourceDirectory, List<string> Files, uint Flags, string CustomXmlPath = null)
        {
            Debug.LogLine(String.Format("[GzsLib] Writing {0}", Path.GetFileName(FileName)));

            // G0S archives must use CLI
            if (IsGzsArchive(FileName))
            {
                Debug.LogLine("[GzsLib] G0S archive — using CLI repacking");
                WriteQarArchiveViaCli(FileName, SourceDirectory, Files, Flags);
                return;
            }

            // TPP .dat archives can use library (if ever needed)
            List<QarEntry> qarEntries = new List<QarEntry>();
            foreach (string s in Files)
            {
                if (s.EndsWith("_unknown")) { continue; }
                qarEntries.Add(new QarEntry() { FilePath = s, Hash = Tools.NameToHash(s), Compressed = (Path.GetExtension(s).EndsWith(".fpk") || Path.GetExtension(s).EndsWith(".fpkd")) ? true : false });
            }

            QarFile q = new QarFile() { Entries = qarEntries, Flags = Flags, Name = FileName };
            using (FileStream outFile = new FileStream(FileName, FileMode.Create))
            {
                IDirectory fileDirectory = new FileSystemDirectory(SourceDirectory);
                q.Write(outFile, fileDirectory);
            }
            Debug.LogLine(String.Format("[GzsLib] Archive written: {0} ({1} entries)", FileName, qarEntries.Count));
        }

        // ===== Utility Methods =====

        private static class Util
        {
            public static void DeleteDirectory(string target)
            {
                int retries = 5;
                while (retries > 0)
                {
                    try
                    {
                        if (Directory.Exists(target)) Directory.Delete(target, true);
                        return;
                    }
                    catch (IOException)
                    {
                        retries--;
                        System.Threading.Thread.Sleep(500);
                        if (retries == 0) throw;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        retries--;
                        System.Threading.Thread.Sleep(500);
                        if (retries == 0) throw;
                    }
                }
            }

            public static void MoveDirectory(string source, string dest)
            {
                int retries = 5;
                while (retries > 0)
                {
                    try
                    {
                        if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
                        
                        foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                        {
                            Directory.CreateDirectory(dirPath.Replace(source, dest));
                        }

                        foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(newPath, newPath.Replace(source, dest), true);
                        }

                        Util.DeleteDirectory(source);
                        return;
                    }
                    catch (IOException)
                    {
                        retries--;
                        System.Threading.Thread.Sleep(500);
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

        //SYNC: makebite
        //tex fpkds seem to require a specific order to their files.
        //entries are sorted alphanumeric ordinal - ascending for fpk, descending for fpkd
        public static List<string> SortFpksFiles(string FpkType, List<string> fpkFiles)
        {
            if (fpkFiles.Count <= 1) return fpkFiles;

            if (FpkType == "fpk") fpkFiles.Sort(StringComparer.Ordinal);
            else fpkFiles.Sort((a, b) => string.CompareOrdinal(b, a));

            var fpkFilesSorted = new List<string>();
            var sortedSet = new HashSet<string>();

            if (archiveExtensions.ContainsKey(FpkType))
            {
                foreach (var archiveExtension in archiveExtensions[FpkType])
                {
                    foreach (string fileName in fpkFiles)
                    {
                        var fileExtension = Path.GetExtension(fileName).TrimStart('.');
                        if (archiveExtension == fileExtension)
                        {
                            fpkFilesSorted.Add(fileName);
                            sortedSet.Add(fileName);
                        }
                    }
                }
            }

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
                if (archiveExtension == "g0s") archiveExtension = "dat"; 
                else return true; 
            }
            
            if (!archiveExtensions.ContainsKey(archiveExtension)) return true;

            var validExtensions = archiveExtensions[archiveExtension];
            var ext = Path.GetExtension(fileName).TrimStart('.');
            if (validExtensions.Contains(ext)) return true;

            if (ext.Equals("xml", StringComparison.OrdinalIgnoreCase) || 
                ext.Equals("txt", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals("bat", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals("inf", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals("log", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true; 
        }
    }
    
    // Hashing snippet to check outdated filenames
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
