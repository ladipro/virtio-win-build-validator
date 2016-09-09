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
            bool haveExclude = (args.Length > 0 && args[0].StartsWith(excludePrefix));

            int dirArgsLength = args.Length - (haveExclude ? 1 : 0);
            if (dirArgsLength < 1 || dirArgsLength > 2)
            {
                Console.WriteLine("Usage: BuildValidator [/exclude:filespec1;filespec2;...] [<old_dir>] <new_dir>");
                Console.WriteLine();
                Console.WriteLine("       If both <old_dir> and <new_dir> are specified the tool diffs the two.");
                Console.WriteLine("       If only <new_dir> is specified the tool dumps its contents.");
                return;
            }

            if (!File.Exists(Tools.Sed))
            {
                Console.WriteLine("Error: Sed tool not found in '{0}'", Tools.Sed);
                return;
            }
            if (dirArgsLength == 2 && !File.Exists(Tools.Diff))
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

            if (haveExclude)
            {
                foreach (string spec in args[0].Substring(excludePrefix.Length).Split(',', ';'))
                {
                    Regex mask = new Regex('^' + spec
                        .Replace(".", "[.]")
                        .Replace("*", ".*")
                        .Replace("?", ".") + '$', RegexOptions.IgnoreCase);
                    excludedSpecs.Add(mask);
                }
            }

            if (dirArgsLength == 2)
            {
                // diff mode
                string oldDir = Path.GetFullPath(args[haveExclude ? 1 : 0]);
                string newDir = Path.GetFullPath(args[haveExclude ? 2 : 1]);

                DiffProcessor processor = new DiffProcessor(oldDir, newDir, excludedSpecs);
                HashSet<string> processedOldFiles = processor.Process();

                ProcessOrphanedOldFiles(excludedSpecs, oldDir, processedOldFiles);
            }
            else if (dirArgsLength == 1)
            {
                // dump mode
                string dir = Path.GetFullPath(args[haveExclude ? 1 : 0]);

                DumpProcessor processor = new DumpProcessor(dir, excludedSpecs);
                processor.Process();
            }
        }
    }
}
