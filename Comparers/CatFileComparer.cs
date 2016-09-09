using System;
using System.IO;
using System.Threading.Tasks;

namespace BuildValidator
{
    class CatFileComparer : PreprocessingFileComparer
    {
        protected async override Task Preprocess(string infile, string outfile)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(infile));

            var cmd = new NullSource()
                .Pipe(new Command(Tools.Sigcheck, "-d", infile))
                .Pipe(new Command(Tools.Sed, "-n", @"/HWID/p;/OS:/p"))
                .Pipe(new FileSink(outfile));
            await cmd.Run();
        }
    }
}
