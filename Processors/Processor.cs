using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildValidator
{
    abstract class Processor
    {
        protected static FileComparer nullComparer = new NullFileComparer();
        protected static FileComparer infFileComparer = new InfFileComparer();
        protected static FileComparer catFileComparer = new CatFileComparer();
        protected static FileComparer peFileComparer = new PeFileComparer();

        protected static Dictionary<string, FileComparer> comparers = new Dictionary<string, FileComparer>(StringComparer.OrdinalIgnoreCase)
        {
            { ".inf", infFileComparer },
            { ".cat", catFileComparer },
            { ".sys", peFileComparer },
            { ".dll", peFileComparer },
            { ".exe", peFileComparer },
        };

        protected List<Regex> excludedSpecs;
        protected string root;

        public Processor(string root, List<Regex> excludedSpecs)
        {
            this.root = Tools.GetProperDirectoryCapitalization(root);
            this.excludedSpecs = excludedSpecs;
        }

        protected static void WaitForTasks(List<Task> tasks, int maxInFlight)
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

        protected static FileComparer GetComparerForFile(string fi)
        {
            FileComparer cmp = nullComparer;

            string ext = Path.GetExtension(fi);
            if (comparers.ContainsKey(ext))
            {
                cmp = comparers[ext];
            }

            return cmp;
        }

        // Walks the directory tree and performs subclass-specific actions using a simple parallelism logic
        public HashSet<string> Process()
        {
            HashSet<string> filesSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<Task> tasks = new List<Task>();

            ProcessWorker(root, tasks, filesSeen);

            // task rundown
            WaitForTasks(tasks, 0);
            foreach (Task<string> task in tasks)
            {
                if (!String.IsNullOrEmpty(task.Result))
                {
                    Console.WriteLine(task.Result);
                }
            }

            return filesSeen;
        }
        
        protected void ProcessWorker(string currentNew, List<Task> tasks, HashSet<string> filesSeen)
        {
            DirectoryInfo current = new DirectoryInfo(currentNew);

            FileInfo[] files = current.GetFiles();
            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    if (Tools.FileMatchesSpecs(excludedSpecs, fi))
                    {
                        continue;
                    }

                    // cap concurrently running diffs at the number of CPUs
                    WaitForTasks(tasks, Environment.ProcessorCount);

                    tasks.Add(GetTask(fi, filesSeen));
                }

                // now recurse
                DirectoryInfo[] subDirs = current.GetDirectories();
                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    ProcessWorker(dirInfo.FullName, tasks, filesSeen);
                }
            }
        }

        // Returns subclass-specific task to perform on the file
        protected abstract Task GetTask(FileInfo fi, HashSet<string> filesSeen);
    }
}
