using System;
using System.Reflection;
using SnakeBite.Tests; 

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            bool named = args != null && args.Length > 0;
            bool runArchives = !named || Has(args, "archives") || Has(args, "all");
            bool runMods = !named || Has(args, "mods") || Has(args, "all");

            Console.WriteLine("Starting Tests...");
            try
            {
                if (runArchives)
                {
                    Console.WriteLine("Running archive round-trip tests...");
                    new ArchiveRoundTripTests().RunAll();
                }

                if (runMods)
                {
                    var testObj = new ModInstallTests();
                    try
                    {
                        Console.WriteLine("Setup...");
                        testObj.Setup();
                        Console.WriteLine("Running mod install tests...");
                        testObj.RunAllTests();
                    }
                    finally
                    {
                        Console.WriteLine("Cleanup...");
                        try { testObj.Cleanup(); } catch { }
                    }
                }

                Console.WriteLine("All tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Test Failed: {0}", ex));
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static bool Has(string[] args, string name)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
