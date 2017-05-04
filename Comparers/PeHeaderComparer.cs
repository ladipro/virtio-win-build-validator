using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildValidator
{
    class PeHeaderComparer : PreprocessingFileComparer
    {
        protected async override Task Preprocess(string infile, string outfile)
        {
            var cmd1 = new NullSource()
                .Pipe(new Command(Tools.Dumpbin, "/HEADERS", infile))
                .Pipe(new Command(Tools.Sed, "-n", Tools.Quote(@"/machine/p;/characteristics/p;/subsystem/p;/size of image/p;" +
                                                               @"s/^.*entry point (.*) \(.*\)$/entry point \1/p")))
                .Pipe(new FileSink(outfile));
            await cmd1.Run();

            SortedDictionary<string, List<string>> importDict = new SortedDictionary<string, List<string>>();

            string tmpFile = Path.GetTempFileName();
            try
            {
                var cmd2 = new NullSource()
                    .Pipe(new Command(Tools.Dumpbin, "/IMPORTS", infile))
                    .Pipe(new Command(Tools.Sed, "-r", Tools.Quote(@"s/^      *[0-9A-F]+/          /g")))
                    .Pipe(new Command(Tools.Sed, Tools.Quote(@"/Import Address Table/d;/Import Name Table/d;/time date stamp/d;/Index of first forwarder reference/d")))
                    .Pipe(new FileSink(tmpFile));
                await cmd2.Run();

                // sort imports by module and symbol for a nice diff
                string line;
                string module = null;
                using (StreamReader sr = new StreamReader(tmpFile))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("  Summary"))
                        {
                            // summary is not interesting, bail
                            break;
                        }

                        if (line.StartsWith("          "))
                        {
                            importDict[module].Add(line.Trim());
                        }
                        else if (line.StartsWith("    "))
                        {
                            module = line.Trim().ToUpper();
                            if (!importDict.ContainsKey(module))
                            {
                                importDict[module] = new List<string>();
                            }
                        }
                    }
                }
            }
            finally
            {
                File.Delete(tmpFile);
            }

            using (StreamWriter sw = new StreamWriter(outfile, true))
            {
                foreach (var pair in importDict)
                {
                    var imports = pair.Value;
                    imports.Sort();

                    foreach (var import in imports)
                    {
                        sw.WriteLine("  " + pair.Key + ": " + import);
                    }
                }
            }
        }
    }
}
