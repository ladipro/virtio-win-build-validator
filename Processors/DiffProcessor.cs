using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildValidator
{
    // Diffs files in two directories, Preprocess returns files seen in the old one
    class DiffProcessor : Processor
    {
        static Dictionary<string, string> substitutions = new Dictionary<string, string>()
        {
            { "Win8.1", "Win8" },
            { "Win10", "Win8" }
        };

        private string rootOld;

        public DiffProcessor(string rootOld, string rootNew, List<Regex> excludedSpecs)
            : base(rootNew, excludedSpecs)
        {
            this.rootOld = rootOld;
        }

        protected override Task GetTask(FileInfo fi, HashSet<string> filesSeen)
        {
            bool usedSubst;
            string ofi = FindOldFile(fi, rootOld, root, out usedSubst);
            if (ofi != null)
            {
                filesSeen.Add(ofi);
                return CompareFiles(ofi, fi.FullName, usedSubst);
            }
            else
            {
                return Task<string>.FromResult("New file without old counterpart: " +
                    fi.FullName + Environment.NewLine);
            }
        }

        static async Task<string> CompareFiles(string ofi, string nfi, bool usedSubst)
        {
            FileComparer cmp = GetComparerForFile(ofi);

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
