using ALMS.App.Models.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Models
{
    public abstract class RepositoryBase<ENTITY, PAIR, SHARED, CLONED> :
        IEntityRepository<ENTITY, PAIR, SHARED, CLONED>
        where ENTITY : IRepositoriesMountedEntity<ENTITY, PAIR, SHARED, CLONED>
        where PAIR : IEntityRepositoryPair<PAIR, SHARED, CLONED, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, PAIR, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, PAIR, ENTITY>
    {
        public abstract ENTITY Entity { get; }
        public abstract string DirectoryPath { get; }

        public (byte[], int) ExecuteWithExitCode(string arguments, byte[] input = null, string program = "git", string working_dir = null)
        {
            var psinfo = new ProcessStartInfo();
            psinfo.FileName = program;
            psinfo.Arguments = arguments;
            psinfo.WorkingDirectory = working_dir == null ? DirectoryPath : working_dir;
            psinfo.CreateNoWindow = true;
            psinfo.UseShellExecute = false;
            psinfo.RedirectStandardInput = input != null;
            psinfo.RedirectStandardOutput = true;

            var proc = Process.Start(psinfo);
            if (input != null)
            {
                proc.StandardInput.BaseStream.Write(input, 0, input.Length);
                proc.StandardInput.Close();
            }
            var msw = new MemoryStream();
            proc.StandardOutput.BaseStream.CopyTo(msw);
            proc.StandardOutput.Close();
            var buff = msw.ToArray();
            msw.Close();
            proc.WaitForExit();
            int code = proc.ExitCode;
            proc.Close();
            return (buff, code);
        }
        public byte[] Execute(string arguments, byte[] input = null, string program = "git", string working_dir = null)
        {
            return ExecuteWithExitCode(arguments, input, program, working_dir).Item1;
        }
    }

    public abstract class SharedRepositoryBase<ME, CLONED, PAIR, ENTITY> :
        RepositoryBase<ENTITY, PAIR, ME, CLONED>, IEntitySharedRepository<ME, CLONED, PAIR, ENTITY>
        where ENTITY : IRepositoriesMountedEntity<ENTITY, PAIR, ME, CLONED>
        where PAIR : IEntityRepositoryPair<PAIR, ME, CLONED, ENTITY>
        where ME : IEntitySharedRepository<ME, CLONED, PAIR, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, ME, PAIR, ENTITY>
    {
        public string Pack(PackService pack_service)
        {
            var service = pack_service switch
            {
                PackService.GitReceivePack => "git-receive-pack",
                _ => "git-upload-pack",
            };
            var output = Execute($"--advertise-refs \"{DirectoryPath}\"", null, service);
            var result = System.Text.Encoding.UTF8.GetString(output);
            var head = $"# service={service}\n0000";
            return $"{head.Length.ToString("x04")}{head}{result}";
        }

        public async Task<byte[]> Pack(PackService pack_service, Stream body)
        {
            var service = pack_service switch
            {
                PackService.GitReceivePack => "git-receive-pack",
                _ => "git-upload-pack",
            };
            using (var msr = new MemoryStream())
            {
                await body.CopyToAsync(msr);
                var input = msr.ToArray();
                return Execute($"--stateless-rpc \"{DirectoryPath}\"", input, service);
            }
        }
    }


    public abstract class ClonedRepositoryBase<ME, SHARED, PAIR, ENTITY> :
        RepositoryBase<ENTITY, PAIR, SHARED, ME>, IEntityClonedRepository<ME, SHARED, PAIR, ENTITY>
        where ENTITY : IRepositoriesMountedEntity<ENTITY, PAIR, SHARED, ME>
        where PAIR : IEntityRepositoryPair<PAIR, SHARED, ME, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, ME, PAIR, ENTITY>
        where ME : IEntityClonedRepository<ME, SHARED, PAIR, ENTITY>
    {

        public string CommitChanges(string message, string author, string email)
        {
            Execute($"add .");
            var (result, code) = ExecuteWithExitCode($"-c user.name=\"{author}\" -c user.email=\"{email}\" commit --allow-empty -F -",
                System.Text.Encoding.UTF8.GetBytes(message));
            return System.Text.Encoding.UTF8.GetString(result);
        }

        public string Push(string remote = "origin", string branch = "master")
        {
            var result = Execute($"push {remote} {branch}");
            return System.Text.Encoding.UTF8.GetString(result);
        }

        public void Synchronize(string remote = "origin", string branch = "master")
        {
            Execute($"fetch {remote}");
            Execute($"reset --hard {remote}/{branch}");
            Execute("clean -fdx");
        }
    }

    public abstract class RepositoryPairBase<ME, SHARED, CLONED, ENTITY> :
        IEntityRepositoryPair<ME, SHARED, CLONED, ENTITY>, IApiedRepository
        where ENTITY : IRepositoriesMountedEntity<ENTITY, ME, SHARED, CLONED>
        where ME : IEntityRepositoryPair<ME, SHARED, CLONED, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, ME, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, ME, ENTITY>
    {
        public abstract SHARED SharedRepository { get; }

        public abstract CLONED ClonedRepository { get; }

        public abstract ENTITY Entity { get; }

        ISharedRepository IRepositoryPair.SharedRepository => SharedRepository;

        IClonedRepository IRepositoryPair.ClonedRepository => ClonedRepository;

        public abstract string ApiUrl { get; }
        public abstract bool CanPull(User user);
        public abstract bool CanPush(User user);


        public IEnumerable<string> GetBranches()
        {
            return System.Text.Encoding.UTF8.GetString(SharedRepository.Execute("branch --format=\"%(refname:short)\"")).Trim().Split().Where(x => !string.IsNullOrWhiteSpace(x));
        }
        public IEnumerable<string> ReadFileList(string path, string branch = "master")
        {
            if (!string.IsNullOrWhiteSpace(path)) path = $"-- \"{path}\"";
            return System.Text.Encoding.UTF8.GetString(SharedRepository.Execute($"ls-tree --full-tree -r --name-only \"{branch}\" {path}")).Trim().Split().Where(x => !string.IsNullOrWhiteSpace(x));
        }

        public CommitInfo ReadCommitInfo(string path, string branch = "master")
        {
            if (!string.IsNullOrWhiteSpace(path)) path = $"-- \"{path}\"";
            var (data, code) = SharedRepository.ExecuteWithExitCode($"log -1 --pretty=\"%s\" \"{branch}\" {path}");
            if (code != 0) return null;
            var message = System.Text.Encoding.UTF8.GetString(data).Trim();
            var hashes = System.Text.Encoding.UTF8.GetString(SharedRepository.Execute($"log -1 --pretty=\"%H %h\" \"{branch}\" -- {path}")).Trim().Split(" ");
            var authorName = System.Text.Encoding.UTF8.GetString(SharedRepository.Execute($"log -1 --pretty=\"%an\" \"{branch}\" -- {path}")).Trim();
            var authorEmail = System.Text.Encoding.UTF8.GetString(SharedRepository.Execute($"log -1 --pretty=\"%ae\" \"{branch}\" -- {path}")).Trim();
            var datestring = System.Text.Encoding.UTF8.GetString(SharedRepository.Execute($"log -1 --pretty=\"%ad\" \"{branch}\" -- {path}")).Trim();
            DateTime date;
            DateTime.TryParseExact(datestring,
                "ddd MMM d HH:mm:ss yyyy K",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out date);
            var shortHash = hashes.Count() >= 2 ? hashes[1] : "";
            return new CommitInfo()
            {
                Message = message,
                Hash = hashes[0],
                ShortHash = shortHash,
                AuthorName = authorName,
                AuthorEmail = authorEmail,
                Date = date
            };
        }

        public (MemoryStream, IRepositoryPair.FileType) ReadFile(string path, string branch = "master")
        {
            var (data, code) = SharedRepository.ExecuteWithExitCode($"show \"{branch}\":\"{path}\"");
            if (code != 0) throw new FileNotFoundException($"File not found `{branch}:{path}'");
            if (!string.IsNullOrWhiteSpace(path)) path = $"-- \"{path}\"";
            var log = SharedRepository.Execute($"log -1 --pretty=\"\" \"{branch}\" --numstat {path}");
            using (var r = new StreamReader(new MemoryStream(log)))
            {
                if (r.ReadLine()[0] == '-')
                {
                    return (new MemoryStream(data), IRepositoryPair.FileType.Binary);
                }
                else
                {
                    return (new MemoryStream(data), IRepositoryPair.FileType.Text);
                }
            }
        }

        public MemoryStream ReadFileWithoutTypeCheck(string path, string branch = "master")
        {
            var (data, code) = SharedRepository.ExecuteWithExitCode($"show --binary \"{branch}\":\"{path}\"");
            if (code != 0) throw new FileNotFoundException($"File not found `{branch}:{path}'");
            return new MemoryStream(data);
        }


        public void Create()
        {
            var shared = new DirectoryInfo(SharedRepository.DirectoryPath);
            shared.Create();
            SharedRepository.Execute($"init --bare --shared {SharedRepository.DirectoryPath}");
            ClonedRepository.Execute($"clone {SharedRepository.DirectoryPath} {ClonedRepository.DirectoryPath}", working_dir: "/");
        }

        public void ResetRemoteUrl()
        {
            ClonedRepository.Execute($"remote set-url origin {SharedRepository.DirectoryPath}");
        }
    }
}
