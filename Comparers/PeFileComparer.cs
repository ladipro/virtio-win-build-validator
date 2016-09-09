using System;
using System.Text;
using System.Threading.Tasks;

namespace BuildValidator
{
    class PeFileComparer : FileComparer
    {
        private PeHeaderComparer peHeaderComparer = new PeHeaderComparer();
        private ResourceComparer resourceComparer = new ResourceComparer();

        public async override Task<string> Compare(string ofi, string nfi)
        {
            StringBuilder diff = new StringBuilder();

            // invoke PE header comparer
            string diffHeader = await peHeaderComparer.Compare(ofi, nfi);
            if (!String.IsNullOrWhiteSpace(diffHeader))
            {
                diff.AppendLine("PE Header diff");
                diff.Append(diffHeader);
            }

            // invoke resource comparer and combine the diffs
            if (!String.IsNullOrWhiteSpace(Tools.Resedit))
            {
                string diffResources = await resourceComparer.Compare(ofi, nfi);
                if (!String.IsNullOrWhiteSpace(diffResources))
                {
                    diff.AppendLine();
                    diff.AppendLine("Resource diff");
                    diff.Append(diffResources);
                }
            }

            return diff.ToString();
        }

        public async override Task<string> Dump(string fi)
        {
            StringBuilder dump = new StringBuilder();

            // invoke PE header comparer
            string dumpHeader = await peHeaderComparer.Dump(fi);
            if (!String.IsNullOrWhiteSpace(dumpHeader))
            {
                dump.AppendLine("PE Header dump");
                dump.Append(dumpHeader);
            }

            // invoke resource comparer and combine the dumps
            if (!String.IsNullOrWhiteSpace(Tools.Resedit))
            {
                string dumpResources = await resourceComparer.Dump(fi);
                if (!String.IsNullOrWhiteSpace(dumpResources))
                {
                    dump.AppendLine();
                    dump.AppendLine("Resource dump");
                    dump.Append(dumpResources);
                }
            }

            return dump.ToString();
        }
    }
}
