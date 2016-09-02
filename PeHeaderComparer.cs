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
                .Pipe(new Command(Tools.Sed, "-n", "\"" + @"/machine/p;/characteristics/p;/subsystem/p;/size of image/p" + "\""))
                .Pipe(new FileSink(outfile));
            await cmd1.Run();

            string tmpFile = Path.GetTempFileName();
            var cmd2 = new NullSource()
                .Pipe(new Command(Tools.Dumpbin, "/IMPORTS", infile))
                .Pipe(new Command(Tools.Sed, "-r", "\"" + @"s/^      *[0-9A-F]+/          /g" + "\""))
                .Pipe(new Command(Tools.Sed, "\"" + @"/Import Address Table/d;/Import Name Table/d;/time date stamp/d;/Index of first forwarder reference/d" + "\""))
                .Pipe(new FileSink(tmpFile));
            await cmd2.Run();

            // sort imports by module and symbol for a nice diff
            string line;
            string module = null;
            SortedDictionary<string, List<string>> importDict = new SortedDictionary<string, List<string>>();
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
