using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using GzsTool.Core.Fpk;
using SnakeBite.GzsTool;

namespace SnakeBite.Tests
{
    public class ArchiveRoundTripTests
    {
        private const string PackRoot = @"D:\dev\snakebite_gz\gz\data_02_out\Assets\tpp\pack\location\gntn\pack_common";
        private const string UnknownHash = "162307802193295888";
        private const string UnknownFile = "a2160e017a10.lua";

        public void RunAll()
        {
            string root = Path.Combine(Path.GetTempPath(), "snakebite_archive_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                RoundTripFpkd(root);
                RoundTripFpk(root);
                RoundTripPftxs(root);
                RoundTripG0s(root);
                RefuseG0sUnknownWithoutArchive(root);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(root))
                        Directory.Delete(root, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cleanup warning: " + ex.Message);
                }
            }
        }

        private void RoundTripFpkd(string root)
        {
            Console.WriteLine("[Archive] fpkd");
            string source = Path.Combine(PackRoot, "gntn_common.fpkd");
            Require(source);
            string work = Path.Combine(root, "fpkd");
            string original = CopyTo(source, Path.Combine(work, "in"));
            string extract = Path.Combine(work, "extract");
            List<string> files = GzsLib.ExtractArchive<FpkFile>(original, extract);
            List<string> references = GzsLib.GetFpkReferences(original);

            string withDir = Path.Combine(work, "with");
            Directory.CreateDirectory(withDir);
            string withManifest = Path.Combine(withDir, Path.GetFileName(original));
            GzsLib.WriteFpkArchive(withManifest, extract, files, references);
            AssertSameFile(original, withManifest, "fpkd with manifest");

            DeleteManifest(extract);
            string fromArchive = WriteCopy(original, Path.Combine(work, "from"));
            MarkOld(fromArchive);
            GzsLib.WriteFpkArchive(fromArchive, extract, files, references);
            AssertRewritten(fromArchive);
            AssertSameFile(original, fromArchive, "fpkd read from archive");

            DeleteManifest(extract);
            string refused = Path.Combine(work, "refused", "gntn_common.fpkd");
            ExpectRefuse(delegate
            {
                GzsLib.WriteFpkArchive(refused, extract, files, references);
            }, "nad0_main0_def.parts");
            if (File.Exists(refused))
                throw new Exception("fpkd refusal wrote an archive");

            DeleteManifest(extract);
            string renamed = RenameOne(extract, files, "nad0_main0_def.parts");
            string renamedOut = WriteCopy(original, Path.Combine(work, "renamed"));
            string before = Md5(renamedOut);
            ExpectRefuse(delegate
            {
                GzsLib.WriteFpkArchive(renamedOut, extract, files, references);
            }, Path.GetFileName(renamed));
            if (Md5(renamedOut) != before)
                throw new Exception("fpkd rename refusal changed the archive");
        }

        private void RoundTripFpk(string root)
        {
            Console.WriteLine("[Archive] fpk");
            string source = Path.Combine(PackRoot, "gntn_common.fpk");
            Require(source);
            string work = Path.Combine(root, "fpk");
            string original = CopyTo(source, Path.Combine(work, "in"));
            string extract = Path.Combine(work, "extract");
            List<string> files = GzsLib.ExtractArchive<FpkFile>(original, extract);
            List<string> references = GzsLib.GetFpkReferences(original);

            string withDir = Path.Combine(work, "with");
            Directory.CreateDirectory(withDir);
            string withManifest = Path.Combine(withDir, Path.GetFileName(original));
            GzsLib.WriteFpkArchive(withManifest, extract, files, references);
            AssertSameFile(original, withManifest, "fpk with manifest");

            DeleteManifest(extract);
            string fromArchive = WriteCopy(original, Path.Combine(work, "from"));
            MarkOld(fromArchive);
            GzsLib.WriteFpkArchive(fromArchive, extract, files, references);
            AssertRewritten(fromArchive);
            AssertSameFile(original, fromArchive, "fpk read from archive");
        }

        private void RoundTripPftxs(string root)
        {
            Console.WriteLine("[Archive] pftxs");
            string source = Path.Combine(PackRoot, "gntn_common.pftxs");
            Require(source);
            string work = Path.Combine(root, "pftxs");
            string original = CopyTo(source, Path.Combine(work, "in"));
            string extract = Path.Combine(work, "extract");
            GzsLib.ExtractArchive<FpkFile>(original, extract);
            string manifest = extract.TrimEnd('\\') + ".fpk_manifest.xml";
            if (!File.Exists(manifest))
                throw new Exception("pftxs extract did not keep a manifest");

            string outDir = Path.Combine(work, "out");
            Directory.CreateDirectory(outDir);
            string xml = Path.Combine(outDir, "gntn_common.pftxs.xml");
            File.Copy(manifest, xml, true);
            string packedFolder = Path.Combine(outDir, "gntn_common_pftxs");
            Directory.Move(extract, packedFolder);
            RunGzs(outDir, "\"" + xml + "\"");
            AssertSameFile(original, Path.Combine(outDir, "gntn_common.pftxs"), "pftxs");
        }

        private void RoundTripG0s(string root)
        {
            Console.WriteLine("[Archive] g0s");
            string fixtureDir = Path.Combine(root, "g0s_fixture");
            Directory.CreateDirectory(Path.Combine(fixtureDir, "mini"));
            File.WriteAllText(Path.Combine(fixtureDir, "mini", UnknownFile), "return 2");
            File.WriteAllText(Path.Combine(fixtureDir, "mini_g0s.xml"),
                "<?xml version=\"1.0\"?>\r\n" +
                "<ArchiveFile xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:type=\"GzsFile\" Name=\"mini.g0s\" Flags=\"3150304\">\r\n" +
                "  <Entries>\r\n" +
                "    <Entry Hash=\"" + UnknownHash + "\" FilePath=\"" + UnknownFile + "\" Compressed=\"false\" />\r\n" +
                "  </Entries>\r\n" +
                "</ArchiveFile>\r\n");
            RunGzs(fixtureDir, "\"mini_g0s.xml\"");
            string original = Path.Combine(fixtureDir, "mini.g0s");
            if (!File.Exists(original))
                throw new Exception("fixture g0s was not written");

            string work = Path.Combine(root, "g0s");
            string archive = CopyTo(original, Path.Combine(work, "in"));
            string extract = Path.Combine(work, "extract");
            List<string> files = GzsLib.ExtractArchive<FpkFile>(archive, extract);
            DeleteManifest(extract);
            string rewritten = WriteCopy(archive, Path.Combine(work, "out"));
            MarkOld(rewritten);
            GzsLib.WriteQarArchive(rewritten, extract, files, GzsLib.chunk0Flags);
            AssertRewritten(rewritten);

            string check = Path.Combine(work, "check");
            GzsLib.ExtractArchive<FpkFile>(rewritten, check);
            string manifest = check.TrimEnd('\\') + ".fpk_manifest.xml";
            XDocument doc = XDocument.Load(manifest);
            bool found = false;
            foreach (XElement entry in doc.Root.Element("Entries").Elements("Entry"))
            {
                XAttribute hash = entry.Attribute("Hash");
                XAttribute path = entry.Attribute("FilePath");
                if (hash != null && hash.Value == UnknownHash && path != null && path.Value == UnknownFile)
                    found = true;
            }
            if (!found)
                throw new Exception("g0s round trip dropped hash " + UnknownHash + " for " + UnknownFile);

            string payload = Path.Combine(check, UnknownFile);
            if (!File.Exists(payload))
                throw new Exception("g0s round trip dropped payload " + UnknownFile);
            if (File.ReadAllText(payload) != "return 2")
                throw new Exception("g0s payload bytes changed");
        }

        private void RefuseG0sUnknownWithoutArchive(string root)
        {
            Console.WriteLine("[Archive] g0s refuses an unknown name with no archive");
            string work = Path.Combine(root, "g0s_refuse");
            string extract = Path.Combine(work, "extract");
            Directory.CreateDirectory(extract);
            File.WriteAllText(Path.Combine(extract, "deadbeef_unknown"), "x");
            var files = new List<string> { "deadbeef_unknown" };
            string output = Path.Combine(work, "out", "mini.g0s");
            ExpectRefuse(delegate
            {
                GzsLib.WriteQarArchive(output, extract, files, GzsLib.chunk0Flags);
            }, "deadbeef_unknown");
            if (File.Exists(output))
                throw new Exception("g0s refusal wrote an archive");
        }

        private static void ExpectRefuse(Action action, string expectedName)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedName, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Refusal did not name " + expectedName + ":\r\n" + ex.Message);
                return;
            }
            throw new Exception("Expected a refusal naming " + expectedName);
        }

        private static void AssertSameFile(string expected, string actual, string label)
        {
            string a = Md5(expected);
            string b = Md5(actual);
            if (a != b)
                throw new Exception(label + " bytes changed. Expected " + a + ", got " + b);
            Console.WriteLine("  " + label + " " + a);
        }

        private static readonly DateTime OldStamp = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static void MarkOld(string path)
        {
            File.SetLastWriteTimeUtc(path, OldStamp);
        }

        private static void AssertRewritten(string path)
        {
            if (!File.Exists(path))
                throw new Exception(path + " was not written");
            if (File.GetLastWriteTimeUtc(path) <= OldStamp)
                throw new Exception(path + " was not rewritten");
        }

        private static string RenameOne(string extractDir, List<string> files, string suffix)
        {
            string relative = null;
            foreach (string file in files)
            {
                if (file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    relative = file;
                    break;
                }
            }
            if (relative == null)
                throw new Exception("Could not find " + suffix + " to rename");
            string full = Path.Combine(extractDir, relative.Replace('/', '\\'));
            string moved = full + ".moved";
            File.Move(full, moved);
            files.Remove(relative);
            files.Add(relative + ".moved");
            return relative + ".moved";
        }

        private static void DeleteManifest(string extractDir)
        {
            string manifest = extractDir.TrimEnd('\\') + ".fpk_manifest.xml";
            if (File.Exists(manifest))
                File.Delete(manifest);
        }

        private static string CopyTo(string source, string directory)
        {
            Directory.CreateDirectory(directory);
            string dest = Path.Combine(directory, Path.GetFileName(source));
            File.Copy(source, dest, true);
            return dest;
        }

        private static string WriteCopy(string source, string directory)
        {
            return CopyTo(source, directory);
        }

        private static void Require(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Archive fixture not found", path);
        }

        private static string Md5(string path)
        {
            using (MD5 md5 = MD5.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        private static void RunGzs(string workingDirectory, string arguments)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] tools = new string[]
            {
                "GzsTool.exe", "GzsTool.exe.config", "GzsTool.Core.dll", "CityHash.dll",
                "fpk_dictionary.txt", "gzs_dictionary.txt", "qar_dictionary.txt"
            };
            foreach (string file in tools)
            {
                string dest = Path.Combine(workingDirectory, file);
                if (!File.Exists(dest))
                    File.Copy(Path.Combine(baseDir, file), dest, true);
            }

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = Path.Combine(workingDirectory, "GzsTool.exe");
            start.Arguments = arguments;
            start.WorkingDirectory = workingDirectory;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            using (Process process = Process.Start(start))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception("GzsTool failed (" + process.ExitCode + ")\r\n" + stdout + "\r\n" + stderr);
            }
        }
    }
}
