using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildValidator
{
    abstract class CommandBase
    {
        protected Task<Stream> inputTask;

        public async Task<Stream> Run()
        {
            try
            {
                return await Execute();
            }
            finally
            {
                if (inputTask != null)
                {
                    Stream input = await inputTask;
                    if (input != null)
                    {
                        input.Close();
                    }
                }
            }
        }

        protected abstract Task<Stream> Execute();

        public CommandBase Pipe(CommandBase into)
        {
            into.inputTask = Run();
            return into;
        }
    }

    // Provides an empty output stream, does nothing
    class NullSource : CommandBase
    {
        protected override Task<Stream> Execute()
        {
            return Task.FromResult(Stream.Null);
        }
    }

    // Provides a file stream to pipe to other commands
    class FileSource : CommandBase
    {
        private readonly string file;

        public FileSource(string file)
        {
            this.file = file;
        }

        protected override Task<Stream> Execute()
        {
            return Task.FromResult<Stream>(new FileStream(file, FileMode.Open, FileAccess.Read));
        }
    }

    // Writes the input stream to a file, opposite to FileSource
    class FileSink : CommandBase
    {
        private readonly string file;
        private readonly FileMode mode;

        public FileSink(string file, FileMode mode)
        {
            this.file = file;
            this.mode = mode;
        }

        public FileSink(string file)
            : this(file, FileMode.Create)
        { }

        protected async override Task<Stream> Execute()
        {
            using (Stream output = new FileStream(file, mode))
            {
                Stream input = await inputTask;
                await input.CopyToAsync(output);
            }
            return null;
        }

    }

    // Writes the input stream to a StringBuilder
    class StringSink : CommandBase
    {
        private readonly StringBuilder sb;

        public StringSink(StringBuilder sb)
        {
            this.sb = sb;
        }

        protected async override Task<Stream> Execute()
        {
            Stream input = await inputTask;
            using (StreamReader sr = new StreamReader(input))
            {
                sb.Append(await sr.ReadToEndAsync());
            }
            return null;
        }
    }

    // Runs a command, feeding the input stream into its stdin and returning its stdout
    // Output stream is fully buffered in memory for simplicity
    class Command : CommandBase
    {
        private readonly string cmd;
        private readonly string[] args;

        public Command(string cmd, params string[] args)
        {
            this.cmd = cmd;
            this.args = args;
        }

        protected async override Task<Stream> Execute()
        {
            Stream input = await inputTask;

            ProcessStartInfo startInfo = new ProcessStartInfo(cmd, String.Join(" ", args))
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using (Process p = new Process())
            {
                p.StartInfo = startInfo;

                Task processTask = p.WaitForExitAsync();
                p.Start();

                // asynchronously sink the output of the command to memory
                MemoryStream output = new MemoryStream();
                Thread thread = new Thread(() => p.StandardOutput.BaseStream.CopyTo(output));
                thread.Start();

                // feed the input
                input.CopyTo(p.StandardInput.BaseStream);
                p.StandardInput.Close();

                // wrap up
                await processTask;
                thread.Join();

                output.Seek(0, SeekOrigin.Begin);
                return output;
            }
        }
    }
}
