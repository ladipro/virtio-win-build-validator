using System;
using System.Threading.Tasks;

namespace BuildValidator
{
    class InfFileComparer : PreprocessingFileComparer
    {
        protected async override Task Preprocess(string infile, string outfile)
        {
            var cmd = new FileSource(infile)
                // remove comments, remove trailing whitespace, remove empty lines
                .Pipe(new Command(Tools.Sed, @"s/\(;.*\)$//;s/\(\s*\)$//;/^\s*$/d"))
                // normalize date/version
                .Pipe(new Command(Tools.Sed, @"s/DriverVer=[^,]*,\([^\.]*\.[^\.]*\.[^\.]*\)\..*$/DriverVer=00\/00\/0000,\1.0/"))
                .Pipe(new FileSink(outfile));
            await cmd.Run();
        }
    }
}
