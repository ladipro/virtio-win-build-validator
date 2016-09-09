using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildValidator
{
    // Dumps files in a directory
    class DumpProcessor : Processor
    {
        public DumpProcessor(string root, List<Regex> excludedSpecs)
            : base(root, excludedSpecs)
        {
        }

        protected override Task GetTask(FileInfo fi, HashSet<string> filesSeen)
        {
            return DumpFile(fi.FullName);
        }

        static async Task<string> DumpFile(string fi)
        {
            FileComparer cmp = GetComparerForFile(fi);
            string dump = await cmp.Dump(fi);

            if (!String.IsNullOrWhiteSpace(dump))
            {
                StringBuilder output = new StringBuilder();
                output.AppendLine("Dump of " + fi + ":");
                output.AppendLine(dump);
                return output.ToString();
            }
            return String.Empty;
        }
    }
}
