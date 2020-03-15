using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ALMS.App.Models.Entities;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Models
{
    public interface ISandbox
    {
        public abstract string Name { get; set; }

        public Task DoOnSandboxAsync(string username, string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<int> doneCallback = null);
    }

    public abstract class SandboxBase<SANDBOX> : IDirectoryMountedEntity<SANDBOX>, ISandbox
        where SANDBOX : SandboxBase<SANDBOX>
    {
        public abstract string Name { get; set; }

        public abstract string DirectoryPath { get; }

        public abstract void CreateNew(DatabaseContext context, IConfiguration config);

        public abstract void Update(DatabaseContext context, IConfiguration config, SANDBOX previous);

        public abstract void Remove(DatabaseContext context, IConfiguration config);


        public async Task BuildAsync(string buildCommands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<int> doneCallback = null)
        {
            await DoAsync($@"debootstrap stretch {DirectoryPath} http://http.debian.net/debian;",
                    stdoutCallback, stderrCallback);
            if (!string.IsNullOrWhiteSpace(buildCommands))
            {
                await DoAsync(buildCommands, stdoutCallback, stderrCallback, doneCallback, "chroot", $"{DirectoryPath} /bin/sh");
            }
        }


        public async Task DoOnSandboxAsync(string username, string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<int> doneCallback = null)
        {
            string cmd;
            int? CpuTimeLimit = null; // TODO
            if (CpuTimeLimit != null)
            {
                cmd = $"-c \"ulimit -t {CpuTimeLimit}; HOME=/home/{username} timeout {CpuTimeLimit} chroot --userspec {username}:{username} {DirectoryPath} /bin/sh\"";
            }
            else
            {
                cmd = $"-c \"HOME=/home/{username} chroot --userspec {username}:{username} {DirectoryPath} /bin/sh\"";
            }

            await DoAsync(commands, stdoutCallback, stderrCallback, doneCallback, "/bin/sh", cmd);
        }

        public async Task DoAsync(string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<int> doneCallback = null,
                string program = "/bin/sh", string args = "")
        {
            await Task.Run(() => {
                var proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = program;
                proc.StartInfo.Arguments = args;

                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;

                if (stdoutCallback != null) { proc.OutputDataReceived += stdoutCallback; }
                if (stderrCallback != null) { proc.ErrorDataReceived += stderrCallback; }

                proc.Start();
                proc.StandardInput.WriteLine(commands);
                proc.StandardInput.Close();
                if (stdoutCallback != null) { proc.BeginOutputReadLine(); }
                if (stderrCallback != null) { proc.BeginErrorReadLine(); }


                proc.WaitForExit();

                if (doneCallback != null) { doneCallback(proc.ExitCode); }
                proc.Close();
            });
        }

        public abstract void CreateDirectory(DatabaseContext context, IConfiguration config);
        public abstract void UpdateDirectory(DatabaseContext context, IConfiguration config, SANDBOX previous);
        public abstract void RemoveDirectory(DatabaseContext context, IConfiguration config);
    }
}
