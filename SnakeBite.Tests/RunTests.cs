using System;
using System.Reflection;
using SnakeBite.Tests; 

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Tests...");
            var testObj = new ModInstallTests();
            try
            {
                Console.WriteLine("Setup...");
                testObj.Setup();
                
                Console.WriteLine("Running InstallUninstallMod_ShouldModifyAndRevertChecksum...");
                testObj.InstallUninstallMod_ShouldModifyAndRevertChecksum();
                
                Console.WriteLine("All tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Test Failed: {0}", ex));
                Console.ResetColor();
                Environment.Exit(1);
            }
            finally
            {
                Console.WriteLine("Cleanup...");
                try { testObj.Cleanup(); } catch { }
            }
        }
    }
}
