using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        public Task DoOnSandboxWithCmdAsync(User user, string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<string> cmdCallback = null,
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


        public Task DoOnSandboxAsync(string username, string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<int> doneCallback = null)
        {
            string args;
            int? CpuTimeLimit = null; // TODO
            if (CpuTimeLimit != null)
            {
                args = $"-c \"ulimit -t {CpuTimeLimit}; HOME=/home/{username} timeout {CpuTimeLimit} chroot --userspec {username}:{username} {DirectoryPath} /bin/sh\"";
            }
            else
            {
                args = $"-c \"HOME=/home/{username} chroot --userspec {username}:{username} {DirectoryPath} /bin/sh\"";
            }
            return DoAsync(commands, stdoutCallback, stderrCallback, doneCallback, "/bin/sh", args);
        }

        public async Task DoOnSandboxWithCmdAsync(User user, string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<string> cmdCallback = null,
                Action<int> doneCallback = null)
        {
            string args;
            int? CpuTimeLimit = null; // TODO
            if (CpuTimeLimit != null)
            {
                args = $"-c \"ulimit -t {CpuTimeLimit}; HOME=/home/{user.Account} timeout {CpuTimeLimit} chroot --userspec {user.Account}:{user.Account} {DirectoryPath} /bin/sh\"";
            }
            else
            {
                args = $"-c \"HOME=/home/{user.Account} chroot --userspec {user.Account}:{user.Account} {DirectoryPath} /bin/sh\"";
            }

            await DoWithCmdAsync(commands, user, stdoutCallback, stderrCallback, cmdCallback, doneCallback, "/bin/sh", args);
        }


        public async Task DoAsync(string commands,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<int> doneCallback = null,
                string program = "/bin/sh", string args = "")
        {
            await Task.Run(() => {
                var proc = new Process();
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
                doneCallback?.Invoke(proc.ExitCode); proc.Close();
            });
        }

        public async Task DoWithCmdAsync(string commands, User user,
                DataReceivedEventHandler stdoutCallback = null,
                DataReceivedEventHandler stderrCallback = null,
                Action<string> cmdCallback = null,
                Action<int> doneCallback = null,
                string program = "/bin/sh", string args = "")
        {
            await Task.Run(() => {
                var fifoname = Guid.NewGuid().ToString("N").Substring(0, 32);
                Console.WriteLine($"Fifo Name: {fifoname}");
                Process.Start("mkfifo", $"{DirectoryPath}/var/tmp/{fifoname}").WaitForExit();
                Process.Start("chown", $"{user.Id + 1000} {DirectoryPath}/var/tmp/{fifoname}").WaitForExit();
                Console.WriteLine("start process");
                var mainProc = Task.Run(() => {
                    var proc = new Process();
                    proc.StartInfo.FileName = program;
                    proc.StartInfo.Arguments = args;
                    proc.StartInfo.Environment["CMD"] = $"/var/tmp/{fifoname}";

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
                    doneCallback?.Invoke(proc.ExitCode); proc.Close();
                });
                Console.WriteLine("start observe");
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;
                var observer = Task.Run(() => {
                    using (var r = new StreamReader($"{DirectoryPath}/var/tmp/{fifoname}"))
                    {
                        while (!token.IsCancellationRequested)
                        {
                            if (!r.EndOfStream)
                            {
                                var t = r.ReadLineAsync();
                                t.Wait(token);
                                if(t.Result !=null)
                                {
                                    cmdCallback(t.Result);
                                }
                            }
                        }
                    }
                }, token);
                mainProc.Wait();
                tokenSource.Cancel();
                Console.WriteLine("end observe");
                Process.Start("rm", $"{DirectoryPath}/var/tmp/{fifoname}").WaitForExit();
            });
        }

        public abstract void CreateDirectory(DatabaseContext context, IConfiguration config);
        public abstract void UpdateDirectory(DatabaseContext context, IConfiguration config, SANDBOX previous);
        public abstract void RemoveDirectory(DatabaseContext context, IConfiguration config);
    }
}
