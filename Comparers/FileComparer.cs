using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BuildValidator
{
    abstract class FileComparer
    {
        public abstract Task<string> Compare(string ofi, string nfi);

        public abstract Task<string> Dump(string fi);
    }

    class NullFileComparer : FileComparer
    {
        public override Task<string> Compare(string ofi, string nfi)
        {
            return Task.FromResult<string>(null);
        }

        public override Task<string> Dump(string fi)
        {
            return Task.FromResult<string>(null);
        }
    }

    abstract class PreprocessingFileComparer : FileComparer
    {
        protected abstract Task Preprocess(string infile, string outfile);

        public async override Task<string> Compare(string ofi, string nfi)
        {
            string ofi_temp = Path.GetTempFileName();
            string nfi_temp = Path.GetTempFileName();

            await Preprocess(ofi, ofi_temp);
            await Preprocess(nfi, nfi_temp);

            StringBuilder sb = new StringBuilder();

            var cmd = new NullSource()
                .Pipe(new Command(Tools.Diff, ofi_temp, nfi_temp))
                .Pipe(new StringSink(sb));
            await cmd.Run();

            File.Delete(ofi_temp);
            File.Delete(nfi_temp);

            return sb.ToString();
        }

        public async override Task<string> Dump(string fi)
        {
            string fi_temp = Path.GetTempFileName();

            await Preprocess(fi, fi_temp);

            StringBuilder sb = new StringBuilder();

            var cmd = new FileSource(fi_temp)
                .Pipe(new StringSink(sb));
            await cmd.Run();

            File.Delete(fi_temp);

            return sb.ToString();
        }
    }
}
