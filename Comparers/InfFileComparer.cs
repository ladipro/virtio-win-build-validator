using System;
using System.IO;
using System.Threading.Tasks;

namespace BuildValidator
{
    class InfFileComparer : PreprocessingFileComparer
    {
        protected async override Task Preprocess(string infile, string outfile)
        {
            // determine/guess whether the file uses Unix (LF) or Windows (CR, LF) line endings
            char[] buffer = new char[1024];
            char ending = '\0';
            using (StreamReader sr = new StreamReader(infile))
            {
                int read;
                do
                {
                    read = sr.ReadBlock(buffer, 0, buffer.Length);
                    ending = Array.Find(buffer, ch => (ch == '\r' || ch == '\n'));
                    if (ending != '\0')
                    {
                        break;
                    }
                }
                while (read == buffer.Length);
            }

            using (StreamWriter sw = new StreamWriter(outfile))
            {
                switch (ending)
                {
                    case '\r': sw.WriteLine("File uses CR LF line endings."); break;
                    case '\n': sw.WriteLine("File uses LF line endings."); break;
                    default: sw.WriteLine("Failed to detect line endings in this file."); break;
                }
                sw.WriteLine();
            }

            var cmd = new FileSource(infile)
                // remove comments, remove trailing whitespace, remove empty lines
                .Pipe(new Command(Tools.Sed, Tools.Quote(@"s/;.*$//;s/\s*$//;/^\s*$/d")))
                // normalize date/version
                .Pipe(new Command(Tools.Sed, Tools.Quote(@"s#DriverVer=[^,]*,\([^\.]*\.[^\.]*\.[^\.]*\)\..*$#DriverVer=00/00/0000,\1.0#")))
                .Pipe(new FileSink(outfile, FileMode.Append));
            await cmd.Run();
        }
    }
}
