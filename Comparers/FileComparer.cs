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
            StringBuilder sb = new StringBuilder();

            string ofi_temp = Path.GetTempFileName();
            try
            {
                string nfi_temp = Path.GetTempFileName();
                try
                {
                    await Preprocess(ofi, ofi_temp);
                    await Preprocess(nfi, nfi_temp);

                    var cmd = new NullSource()
                        .Pipe(new Command(Tools.Diff, ofi_temp, nfi_temp))
                        .Pipe(new StringSink(sb));
                    await cmd.Run();
                }
                finally
                {
                    File.Delete(nfi_temp);
                }
            }
            finally
            {
                File.Delete(ofi_temp);
            }

            return sb.ToString();
        }

        public async override Task<string> Dump(string fi)
        {
            StringBuilder sb = new StringBuilder();

            string fi_temp = Path.GetTempFileName();
            try
            {
                await Preprocess(fi, fi_temp);

                var cmd = new FileSource(fi_temp)
                    .Pipe(new StringSink(sb));
                await cmd.Run();
            }
            finally
            {
                File.Delete(fi_temp);
            }

            return sb.ToString();
        }
    }
}
