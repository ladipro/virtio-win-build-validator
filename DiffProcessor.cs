using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildValidator
{
    class DiffProcessor
    {
        static Dictionary<string, string> substitutions = new Dictionary<string, string>()
        {
            { "Win8.1", "Win8" },
            { "Win10", "Win8" }
        };

        static FileComparer nullComparer = new NullFileComparer();
        static FileComparer infFileComparer = new InfFileComparer();
        static FileComparer catFileComparer = new CatFileComparer();
        static FileComparer peFileComparer = new PeFileComparer();

        static Dictionary<string, FileComparer> comparers = new Dictionary<string, FileComparer>(StringComparer.OrdinalIgnoreCase)
        {
            { ".inf", infFileComparer },
            { ".cat", catFileComparer },
            { ".sys", peFileComparer },
            { ".dll", peFileComparer },
            { ".exe", peFileComparer },
        };

        private string rootOld;
        private string rootNew;
        List<Regex> excludedSpecs;

        public DiffProcessor(string rootOld, string rootNew, List<Regex> excludedSpecs)
        {
            this.rootOld = rootOld;
            this.rootNew = rootNew;
            this.excludedSpecs = excludedSpecs;
        }

        // Walks the old and new directories and diffs files using a simple parallelism logic
        public HashSet<string> Process()
        {
            HashSet<string> oldFilesSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<Task> tasks = new List<Task>();

            ProcessWorker(rootNew, tasks, oldFilesSeen);

            // task rundown
            WaitForTasks(tasks, 0);
            foreach (Task<string> task in tasks)
            {
                if (!String.IsNullOrEmpty(task.Result))
                {
                    Console.WriteLine(task.Result);
                }
            }

            return oldFilesSeen;
        }

        private void ProcessWorker(string currentNew, List<Task> tasks, HashSet<string> oldFilesSeen)
        {
            DirectoryInfo current = new DirectoryInfo(currentNew);

            FileInfo[] files = current.GetFiles();
            if (files != null)
            {
                foreach (FileInfo nfi in files)
                {
                    if (Tools.FileMatchesSpecs(excludedSpecs, nfi))
                    {
                        continue;
                    }

                    bool usedSubst;
                    string ofi = FindOldFile(nfi, rootOld, rootNew, out usedSubst);
                    if (ofi != null)
                    {
                        // cap concurrently running diffs at the number of CPUs
                        WaitForTasks(tasks, Environment.ProcessorCount);

                        oldFilesSeen.Add(ofi);
                        tasks.Add(CompareFiles(ofi, nfi.FullName, usedSubst));
                    }
                    else
                    {
                        tasks.Add(Task<string>.FromResult("New file without old counterpart: " +
                            nfi.FullName + Environment.NewLine));
                    }
                }

                // now recurse
                DirectoryInfo[] subDirs = current.GetDirectories();
                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    ProcessWorker(dirInfo.FullName, tasks, oldFilesSeen);
                }
            }
        }

        static void WaitForTasks(List<Task> tasks, int maxInFlight)
        {
            while (true)
            {
                List<Task> activeTasks = new List<Task>();
                foreach (Task task in tasks)
                {
                    if (!task.IsCompleted)
                    {
                        activeTasks.Add(task);
                    }
                }

                if (activeTasks.Count < maxInFlight)
                {
                    return;
                }

                Task.WaitAny(activeTasks.ToArray());
                if (activeTasks.Count == maxInFlight)
                {
                    // we know for sure that one task completed
                    return;
                }
            }
        }

        static async Task<string> CompareFiles(string ofi, string nfi, bool usedSubst)
        {
            FileComparer cmp = nullComparer;

            string ext = Path.GetExtension(ofi);
            if (comparers.ContainsKey(ext))
            {
                cmp = comparers[ext];
            }

            string diff = await cmp.Compare(ofi, nfi);

            if (!String.IsNullOrWhiteSpace(diff))
            {
                StringBuilder output = new StringBuilder();
                output.AppendLine("Diff " + ofi + " vs " + nfi + ":");
                if (usedSubst)
                {
                    output.AppendLine("(NOTE: New file has no exact counterpart in old tree, using a substitution)");
                }
                output.AppendLine(diff);
                return output.ToString();
            }
            return String.Empty;
        }

        static string FindOldFile(FileInfo nfi, string rootOld, string rootNew, out bool usedSubst)
        {
            string nameOnly = nfi.FullName.Substring(rootNew.Length + 1);

            // try exact match first
            string ofi = Path.Combine(rootOld, nameOnly);
            if (File.Exists(ofi))
            {
                usedSubst = false;
                return ofi;
            }

            // try a substitution if exact match failed
            foreach (var subst in substitutions)
            {
                if (nameOnly.ToLower().StartsWith(subst.Key.ToLower()))
                {
                    string substNameOnly = subst.Value + nameOnly.Substring(subst.Key.Length);
                    ofi = Path.Combine(rootOld, substNameOnly);
                    if (File.Exists(ofi))
                    {
                        usedSubst = true;
                        return ofi;
                    }
                }
            }

            // give up, there's no good old file to diff this new file against
            usedSubst = false;
            return null;
        }
    }
}
