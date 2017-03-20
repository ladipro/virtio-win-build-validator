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
        enum PathMatchLevel
        {
            None,
            FullMatch,             // we found the corresponding file at the exact same path
            CaseInsensitiveMatch,  // Windows thinks it's the same path but it differs in case
            SubstitutionMatch,     // we had to use a substitution
        }

        static Dictionary<string, string> substitutions = new Dictionary<string, string>()
        {
            { "Win8.1", "Win8" },
            { "Win10", "Win8" },
            { "w8.1", "w8" },
            { "Wxp", "XP" },
            { "Wnet", "XP" },
            { "Wlh", "Vista" },
        };

        private string rootOld;

        public DiffProcessor(string rootOld, string rootNew, List<Regex> excludedSpecs)
            : base(rootNew, excludedSpecs)
        {
            this.rootOld = Tools.GetProperDirectoryCapitalization(rootOld);
        }

        protected override Task GetTask(FileInfo fi, HashSet<string> filesSeen)
        {
            PathMatchLevel matchLevel;
            string ofi = FindOldFile(fi, rootOld, root, out matchLevel);
            if (ofi != null)
            {
                filesSeen.Add(ofi);
                return CompareFiles(ofi, fi.FullName, matchLevel);
            }
            else
            {
                return Task<string>.FromResult("New file without old counterpart: " +
                    fi.FullName + Environment.NewLine);
            }
        }

        static async Task<string> CompareFiles(string ofi, string nfi, PathMatchLevel matchLevel)
        {
            FileComparer cmp = GetComparerForFile(ofi);

            string diff = await cmp.Compare(ofi, nfi);

            if (!String.IsNullOrWhiteSpace(diff) || matchLevel != PathMatchLevel.FullMatch)
            {
                StringBuilder output = new StringBuilder();
                output.AppendLine("Diff " + ofi + " vs " + nfi + ":");
                switch (matchLevel)
                {
                    case PathMatchLevel.CaseInsensitiveMatch:
                        output.AppendLine("(NOTE: File paths differ in case)");
                        break;

                    case PathMatchLevel.SubstitutionMatch:
                        output.AppendLine("(NOTE: New file has no exact counterpart in old tree, using a substitution)");
                        break;
                }
                output.AppendLine(diff);
                return output.ToString();
            }
            return String.Empty;
        }

        static string FindOldFile(FileInfo nfi, string rootOld, string rootNew, out PathMatchLevel matchLevel)
        {
            int rootLength = rootNew.Length;
            if (!rootNew.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                rootLength++;
            }
            string nameOnly = nfi.FullName.Substring(rootLength);

            // try exact match first
            string ofi = Path.Combine(rootOld, nameOnly);
            if (File.Exists(ofi))
            {
                string exactName = Tools.GetProperFilePathCapitalization(ofi);
                if (ofi == exactName)
                {
                    matchLevel = PathMatchLevel.FullMatch;
                }
                else
                {
                    matchLevel = PathMatchLevel.CaseInsensitiveMatch;
                }
                return exactName;
            }

            // try a substitution if exact/case-insensitive match failed
            foreach (var subst in substitutions)
            {
                if (nameOnly.ToLower().StartsWith(subst.Key.ToLower()))
                {
                    string substNameOnly = subst.Value + nameOnly.Substring(subst.Key.Length);
                    ofi = Path.Combine(rootOld, substNameOnly);
                    if (File.Exists(ofi))
                    {
                        matchLevel = PathMatchLevel.SubstitutionMatch;
                        return ofi;
                    }
                }
            }

            // give up, there's no good old file to diff this new file against
            matchLevel = PathMatchLevel.None;
            return null;
        }
    }
}
