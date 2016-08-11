using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BuildValidator
{
    class Program
    {
        static void ProcessOrphanedOldFiles(List<Regex> excludedSpecs, string rootOld, HashSet<string> processedOldFiles)
        {
            DirectoryInfo root = new DirectoryInfo(rootOld);

            FileInfo[] files = root.GetFiles();
            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    if (Tools.FileMatchesSpecs(excludedSpecs, fi))
                    {
                        continue;
                    }

                    if (!processedOldFiles.Contains(fi.FullName))
                    {
                        Console.WriteLine("Old file without new counterpart: " + fi.FullName);
                        Console.WriteLine();
                    }
                }

                // Now find all the subdirectories under this directory.
                DirectoryInfo[] subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    ProcessOrphanedOldFiles(excludedSpecs, dirInfo.FullName, processedOldFiles);
                }
            }
        }

        static void Main(string[] args)
        {
            const string excludePrefix = "/exclude:";

            if (args.Length < 2 || args.Length > 3 ||
                (args.Length == 3 && !args[0].StartsWith(excludePrefix)))
            {
                Console.WriteLine("Usage: BuildValidator [/exclude:filespec1;filespec2;...] <old_dir> <new_dir>");
                return;
            }

            if (!File.Exists(Tools.Sed))
            {
                Console.WriteLine("Error: Sed tool not found in '{0}'", Tools.Sed);
                return;
            }
            if (!File.Exists(Tools.Diff))
            {
                Console.WriteLine("Error: Diff tool not found in '{0}'", Tools.Diff);
                return;
            }
            if (!File.Exists(Tools.Sigcheck))
            {
                Console.WriteLine("Error: Sigcheck tool not found in '{0}'", Tools.Sigcheck);
                return;
            }
            if (!File.Exists(Tools.Resedit))
            {
                Console.WriteLine("Resedit tool not found in '{0}', embedded resources will not be compared", Tools.Resedit);
                // just a warning, we don't bail
            }
            if (!File.Exists(Tools.Dumpbin))
            {
                Console.WriteLine("Error: Dumpbin tool not found in '{0}'", Tools.Dumpbin);
                return;
            }

            List<Regex> excludedSpecs = new List<Regex>();
            string oldDir;
            string newDir;

            if (args.Length == 3)
            {
                foreach (string spec in args[0].Substring(excludePrefix.Length).Split(',', ';'))
                {
                    Regex mask = new Regex('^' + spec
                        .Replace(".", "[.]")
                        .Replace("*", ".*")
                        .Replace("?", ".") + '$', RegexOptions.IgnoreCase);
                    excludedSpecs.Add(mask);
                }
                oldDir = args[1];
                newDir = args[2];
            }
            else
            {
                oldDir = args[0];
                newDir = args[1];
            }

            oldDir = Path.GetFullPath(oldDir);
            newDir = Path.GetFullPath(newDir);

            DiffProcessor processor = new DiffProcessor(oldDir, newDir, excludedSpecs);
            HashSet<string> processedOldFiles = processor.Process();

            ProcessOrphanedOldFiles(excludedSpecs, oldDir, processedOldFiles);
        }
    }
}
