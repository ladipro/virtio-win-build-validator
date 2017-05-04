using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BuildValidator
{
    class Tools
    {
        public static string Sed { get { return ConfigurationManager.AppSettings["SedPath"]; } }
        public static string Diff { get { return ConfigurationManager.AppSettings["DiffPath"]; } }
        public static string Sigcheck { get { return ConfigurationManager.AppSettings["SigcheckPath"]; } }
        public static string Resedit { get { return ConfigurationManager.AppSettings["ReseditPath"]; } }
        public static string Dumpbin { get { return ConfigurationManager.AppSettings["DumpbinPath"]; } }

        public static bool FileMatchesSpecs(List<Regex> specs, FileInfo fi)
        {
            foreach (Regex spec in specs)
            {
                if (spec.IsMatch(fi.Name))
                {
                    return true;
                }
            }
            return false;
        }

        public static string Quote(string arg)
        {
            return String.Format("\"{0}\"", arg);
        }

        public static string GetProperDirectoryCapitalization(string dirPath)
        {
            return GetProperDirectoryCapitalization(new DirectoryInfo(dirPath));
        }

        public static string GetProperDirectoryCapitalization(DirectoryInfo dirInfo)
        {
            DirectoryInfo parentDirInfo = dirInfo.Parent;
            if (null == parentDirInfo)
                return dirInfo.Name;
            return Path.Combine(GetProperDirectoryCapitalization(parentDirInfo),
                                parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
        }

        public static string GetProperFilePathCapitalization(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            DirectoryInfo dirInfo = fileInfo.Directory;
            return Path.Combine(GetProperDirectoryCapitalization(dirInfo),
                                dirInfo.GetFiles(fileInfo.Name)[0].Name);
        }
    }

    static class Extensions
    {
        public static Task WaitForExitAsync(this Process process,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);

            return tcs.Task;
        }
    }
}
