using System;
using System.IO;
using System.Threading.Tasks;

namespace BuildValidator
{
    class ResourceComparer : PreprocessingFileComparer
    {
        protected async override Task Preprocess(string infile, string outfile)
        {
            if (!File.Exists(Tools.Resedit))
            {
                return;
            }

            // Resedit doesn't recognize all PE file extensions so always rename to .exe
            // and also copy to a separate directory to avoid output file name conflicts
            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tmpPath);
            try
            {
                string tmpFile = Path.GetTempFileName();

                string tmpFileExe = Path.Combine(tmpPath, Path.GetFileNameWithoutExtension(infile) + ".exe");
                string tmpFileRc = Path.ChangeExtension(tmpFileExe, "rc");

                File.Copy(infile, tmpFileExe);
                FileAttributes attributes = File.GetAttributes(tmpFileExe);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(tmpFileExe, attributes & ~FileAttributes.ReadOnly);
                }

                var cmd1 = new NullSource()
                    .Pipe(new Command(Tools.Resedit, "-convert", tmpFileExe, tmpFileRc));
                await cmd1.Run();

                // normalize versions
                var cmd2 = new FileSource(tmpFileRc)
                    .Pipe(new Command(Tools.Sed, @"s/\([0-9]*[,\.][0-9]*[,\.][0-9]*[,\.]\)[0-9]*/\10/"))
                    .Pipe(new FileSink(outfile));
                await cmd2.Run();

                // process and delete other resource artifacts
                var dir = new DirectoryInfo(Path.GetDirectoryName(tmpFileRc));
                foreach (var file in dir.EnumerateFiles("manifest*.xml"))
                {
                    string manifest = File.ReadAllText(file.FullName);
                    File.AppendAllText(outfile, manifest);
                }
            }
            finally
            {
                Directory.Delete(tmpPath, true);
            }
        }
    }
}
