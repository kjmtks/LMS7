using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ALMS.App.Models.Contents;
using ALMS.App.Models.Entities;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Models
{
    public interface ISandbox
    {
        public abstract string Name { get; set; }

        public Task DoOnSandboxAsync(string username, string commands,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<int> doneCallback = null,
                ActivityLimits limit = null);
        public Task DoOnSandboxWithCmdAsync(Entities.User user, string commands,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<string> cmdCallback = null,
                Action<int> doneCallback = null,
                ActivityLimits limit = null);
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
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<int> doneCallback = null)
        {
            await DoAsync($@"debootstrap stretch {DirectoryPath} http://http.debian.net/debian;",
                    stdoutCallback, stderrCallback);
            if (!string.IsNullOrWhiteSpace(buildCommands))
            {
                await DoAsync(buildCommands, stdoutCallback, stderrCallback, doneCallback, null, "chroot", $"{DirectoryPath} /bin/bash");
            }
        }

        private string MakeUlimitCommand(ActivityLimits limit, string midstgring="")
        {
            var sb = new StringBuilder();
            bool useCpuTime = false;

            if (limit.CpuTime > 0)
            {
                sb.Append($"ulimit -t {limit.CpuTime};");
                useCpuTime = true;
            }

            if (limit.Pids > 0)
            {
                sb.Append($"ulimit -u {limit.Pids};");
                useCpuTime = true;
            }

            if (!string.IsNullOrWhiteSpace(limit.Memory))
            {
                var regex = new Regex("^(?<dec>[0-9.]+)(?<uni>(|K|M|G))$");
                var m = regex.Match(limit.Memory);
                if(!m.Groups["dec"].Success || !decimal.TryParse(m.Groups["dec"].Value, out var value))
                {
                    value = 0;
                }
                var unit = m.Groups["uni"].Success ? m.Groups["uni"].Value?.ToLower() : "";
                if (unit == "k")
                {
                    value = value * 1024;
                }
                if (unit == "m")
                {
                    value = value * 1024 * 1024;
                }
                if (unit == "g")
                {
                    value = value * 1024 * 1024 * 1024;
                }
                sb.Append($"ulimit -m {value}; ulimit -v {value};");
            }

            return useCpuTime ? $"{sb.ToString()} {midstgring} timeout {limit.CpuTime} " : $"{sb.ToString()} {midstgring}";
        }

        public Task DoOnSandboxAsync(string username, string commands,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<int> doneCallback = null,
                ActivityLimits limit = null)
        {
            string args;

            if(limit != null)
            {
                args = $"-c \"{MakeUlimitCommand(limit, $"HOME=/home/{username}")} chroot --userspec {username}:{username} {DirectoryPath} /bin/sh\"";
            }
            else
            {
                args = $"-c \"HOME=/home/{username} chroot --userspec {username}:{username} {DirectoryPath} /bin/sh\"";
            }
            return DoAsync(commands, stdoutCallback, stderrCallback, doneCallback, limit, "/bin/sh", args);
        }

        public async Task DoOnSandboxWithCmdAsync(Entities.User user, string commands,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<string> cmdCallback = null,
                Action<int> doneCallback = null,
                ActivityLimits limit = null)
        {
            string args;
            if (limit != null)
            {
                args = $"-c \"{MakeUlimitCommand(limit, $"HOME=/home/{user.Account}")} chroot --userspec {user.Account}:{user.Account} {DirectoryPath} /bin/sh\"";
            }
            else
            {
                args = $"-c \"HOME=/home/{user.Account} chroot --userspec {user.Account}:{user.Account} {DirectoryPath} /bin/sh\"";
            }

            await DoWithCmdAsync(commands, user, stdoutCallback, stderrCallback, cmdCallback, doneCallback, limit, "/bin/bash", args);
        }


        protected async Task DoAsync(string commands,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<int> doneCallback = null,
                ActivityLimits limit = null,
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

                if (stdoutCallback != null)
                {
                    if (limit?.StdoutLength != null && limit.StdoutLength > 0)
                    {
                        var remaind = limit.StdoutLength;
                        proc.OutputDataReceived += (o, e) =>
                        {
                            if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                            {
                                if (e.Data.Length <= remaind)
                                {
                                    stdoutCallback(e.Data);
                                    remaind -= (uint)e.Data.Length;
                                }
                                else
                                {
                                    stdoutCallback(e.Data.Substring(0, (int)remaind));
                                    remaind = 0;
                                }
                            }
                        };
                    }
                    else
                    {
                        proc.OutputDataReceived += (o, e) => { stdoutCallback(e.Data); };
                    }
                }
                if (stderrCallback != null)
                {
                    if (limit?.StderrLength != null && limit.StderrLength > 0)
                    {
                        var remaind = limit.StderrLength;
                        proc.ErrorDataReceived += (o, e) =>
                        {
                            if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                            {
                                if (e.Data.Length <= remaind)
                                {
                                    stderrCallback(e.Data);
                                    remaind -= (uint)e.Data.Length;
                                }
                                else
                                {
                                    stderrCallback(e.Data.Substring(0, (int)remaind));
                                    remaind = 0;
                                }
                            }
                        };
                    }
                    else
                    {
                        proc.ErrorDataReceived += (o, e) => { stdoutCallback(e.Data); };
                    }
                }


                proc.Start();
                proc.StandardInput.WriteLine(commands);
                proc.StandardInput.Close();
                if (stdoutCallback != null) { proc.BeginOutputReadLine(); }
                if (stderrCallback != null) { proc.BeginErrorReadLine(); }
                proc.WaitForExit();
                doneCallback?.Invoke(proc.ExitCode); proc.Close();
            });
        }

        protected async Task DoWithCmdAsync(string commands, Entities.User user,
                Action<string> stdoutCallback = null,
                Action<string> stderrCallback = null,
                Action<string> cmdCallback = null,
                Action<int> doneCallback = null,
                ActivityLimits limit = null,
                string program = "/bin/sh", string args = "")
        {
            await Task.Run(() => {
                var fifoname = Guid.NewGuid().ToString("N").Substring(0, 32);
                Process.Start("mkfifo", $"{DirectoryPath}/var/tmp/{fifoname}").WaitForExit();
                Process.Start("chown", $"{user.Id + 1000} {DirectoryPath}/var/tmp/{fifoname}").WaitForExit();

var mainProc = Task.Run(async () => {
                    var proc = new Process();
                    proc.StartInfo.FileName = program;
                    proc.StartInfo.Arguments = args;
                    proc.StartInfo.Environment["CMD"] = $"/var/tmp/{fifoname}";

                    proc.StartInfo.RedirectStandardInput = true;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;

                    var errorClosed = new ManualResetEvent(false);
                    var outputClosed = new ManualResetEvent(false);
                    ManualResetEvent[] waits = { outputClosed, errorClosed };
                    errorClosed.Reset();
                    outputClosed.Reset();

                    if (stdoutCallback != null)
                    {
                        if (limit?.StdoutLength != null && limit.StdoutLength > 0)
                        {
                            var remaind = limit.StdoutLength;
                            proc.OutputDataReceived += (o, e) =>
                            {
                                if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                {
                                    if (e.Data.Length <= remaind)
                                    {
                                        stdoutCallback(e.Data);
                                        remaind -= (uint)e.Data.Length;
                                    }
                                    else
                                    {
                                        stdoutCallback(e.Data.Substring(0, (int)remaind));
                                        remaind = 0;
                                    }
                                }
                                outputClosed.Set();
                            };
                        }
                        else
                        {
                            proc.OutputDataReceived += (o, e) => { stdoutCallback(e.Data); };
                        }
                    }
                    if (stderrCallback != null)
                    {
                        if (limit?.StderrLength != null && limit.StderrLength > 0)
                        {
                            var remaind = limit.StderrLength;
                            proc.ErrorDataReceived += (o, e) =>
                            {
                                if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                {
                                    if (e.Data.Length <= remaind)
                                    {
                                        stderrCallback(e.Data);
                                        remaind -= (uint)e.Data.Length;
                                    }
                                    else
                                    {
                                        stderrCallback(e.Data.Substring(0, (int)remaind));
                                        remaind = 0;
                                    }
                                }
                                errorClosed.Set();
                            };
                        }
                        else
                        {
                            proc.ErrorDataReceived += (o, e) => { stderrCallback(e.Data); };
                        }
                    }

                    proc.Start();
                    proc.StandardInput.WriteLine(commands);
                    proc.StandardInput.Close();
                    if (stdoutCallback != null) { proc.BeginOutputReadLine(); }
                    if (stderrCallback != null) { proc.BeginErrorReadLine(); }
                    if(limit != null)
                    {
                        proc.WaitForExit((int)(limit.CpuTime + 5) * 1000);
                    }
                    else
                    {
                        proc.WaitForExit();
                    }
                    if (!ManualResetEvent.WaitAll(waits, 10000))
                    {
                        Console.Error.WriteLine($"ERROR: STDOUT/ERR wait timeout");
                    }
                    doneCallback?.Invoke(proc.ExitCode); proc.Close();
                });
/*
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

                    if (stdoutCallback != null)
                    {
                        if (limit?.StdoutLength != null && limit.StdoutLength > 0)
                        {
                            var remaind = limit.StdoutLength;
                            proc.OutputDataReceived += (o, e) =>
                            {
                                if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                {
                                    if (e.Data.Length <= remaind)
                                    {
                                        // Console.WriteLine($"OUT: {e.Data}");
                                        stdoutCallback(e.Data);
                                        remaind -= (uint)e.Data.Length;
                                    }
                                    else
                                    {
                                        // Console.WriteLine($"OUT: {e.Data.Substring(0, (int)remaind)}");
                                        stdoutCallback(e.Data.Substring(0, (int)remaind));
                                        remaind = 0;
                                    }
                                }
                            };
                        }
                        else
                        {
                            proc.OutputDataReceived += (o, e) => {
                              // Console.WriteLine($"OUT: {e.Data}");
                              stdoutCallback(e.Data);
                            };
                        }
                    }
                    if (stderrCallback != null)
                    {
                        if (limit?.StderrLength != null && limit.StderrLength > 0)
                        {
                            var remaind = limit.StderrLength;
                            proc.ErrorDataReceived += (o, e) =>
                            {
                                if (remaind > 0 && !string.IsNullOrEmpty(e.Data))
                                {
                                    if (e.Data.Length <= remaind)
                                    {
                                        stderrCallback(e.Data);
                                        remaind -= (uint)e.Data.Length;
                                    }
                                    else
                                    {
                                        stderrCallback(e.Data.Substring(0, (int)remaind));
                                        remaind = 0;
                                    }
                                }
                            };
                        }
                        else
                        {
                            proc.ErrorDataReceived += (o, e) => { stderrCallback(e.Data); };
                        }
                    }

                    proc.Start();
                    proc.StandardInput.WriteLine(commands);
                    proc.StandardInput.Close();
                    if (stdoutCallback != null) { proc.BeginOutputReadLine(); }
                    if (stderrCallback != null) { proc.BeginErrorReadLine(); }
                    if(limit != null)
                    {
                        proc.WaitForExit((int)(limit.CpuTime + 5) * 1000);
                    }
                    else
                    {
                        proc.WaitForExit();
                    }
                    doneCallback?.Invoke(proc.ExitCode); proc.Close();
                });
*/
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
                Process.Start("rm", $"{DirectoryPath}/var/tmp/{fifoname}").WaitForExit();
            });
        }



        public abstract void CreateDirectory(DatabaseContext context, IConfiguration config);
        public abstract void UpdateDirectory(DatabaseContext context, IConfiguration config, SANDBOX previous);
        public abstract void RemoveDirectory(DatabaseContext context, IConfiguration config);
    }
}
