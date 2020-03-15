using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Models.Entities
{


    public interface IEntity<ME>
        where ME : IEntity<ME>
    {
        public void CreateNew(DatabaseContext context);
        public void Update(DatabaseContext context, ME previous);
        public void Remove(DatabaseContext context);
    }

    public interface IChildEntity<ME, PARENT> : IEntity<ME>
        where ME : IChildEntity<ME, PARENT>
        where PARENT : IEntity<PARENT>
    {
        public PARENT Parent { get; set; }
        public void UpdateParent(DatabaseContext context, PARENT successor, PARENT previous);
    }

    public interface IEditableEntity<ME> : IEntity<ME>
        where ME : IEditableEntity<ME>
    {
        public void PrepareModelForAddNew(DatabaseContext context);
        public void PrepareModelForEdit(DatabaseContext context, ME original);
        public ME GetEntityForEditOrRemove(DatabaseContext context);
        public ME GetEntityAsNoTracking(DatabaseContext context);
        
        public bool ServerSideValidationOnCreate(DatabaseContext context, Action<string, string> AddValidationError);
        public bool ServerSideValidationOnUpdate(DatabaseContext context, Action<string, string> AddValidationError);
    }

    public interface IDirectoryMountedEntity<ME> : IEntity<ME>
        where ME : IDirectoryMountedEntity<ME>
    {
        public string DirectoryPath { get; }
        public void CreateDirectory(DatabaseContext context);
        public void UpdateDirectory(DatabaseContext context, ME previous);
        public void RemoveDirectory(DatabaseContext context);
    }


    public interface IRepositoriesMountedEntity<ME, PAIR, SHARED, CLONED> : IEntity<ME>
        where ME : IRepositoriesMountedEntity<ME, PAIR, SHARED, CLONED>
        where PAIR : IEntityRepositoryPair<PAIR, SHARED, CLONED, ME>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, PAIR, ME>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, PAIR, ME>
    {
        public IEntityRepositoryPair<PAIR, SHARED, CLONED, ME> RepositoryPair { get; }
    }


    public interface IRepository
    {
        public string DirectoryPath { get; }
        public byte[] Execute(string arguments, byte[] input = null, string program = "git", string working_dir = null);
        public (byte[], int) ExecuteWithExitCode(string arguments, byte[] input = null, string program = "git", string working_dir = null);
    }

    public interface IApiedRepository
    {
        public string ApiUrl { get; }
        public bool CanPush(User user);
        public bool CanPull(User user);
    }

    public interface ISharedRepository : IRepository
    {
        public enum FileType { Text, Binary }
        public string Pack(PackService pack_service);
        public Task<byte[]> Pack(PackService pack_service, Stream body);

        public (MemoryStream, FileType) ReadFile(string path, string branch = "master");
        public MemoryStream ReadFileWithoutTypeCheck(string path, string branch = "master");

        public CommitInfo ReadCommitInfo(string path=".", string branch = "master");

        public IEnumerable<string> GetBranches();
    }
    public interface IClonedRepository : IRepository
    {
        public void Synchronize(string remote = "origin", string branch = "master");
        public string Push(string remote = "origin", string branch = "master");
        public string CommitChanges(string message, string author, string email);
    }

    public interface IEntityRepository<ENTITY, PAIR, SHARED, CLONED> : IRepository
        where ENTITY : IRepositoriesMountedEntity<ENTITY, PAIR, SHARED, CLONED>
        where PAIR : IEntityRepositoryPair<PAIR, SHARED, CLONED, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, PAIR, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, PAIR, ENTITY>
    {
        public ENTITY Entity { get; }
    }

    public enum PackService { GitUploadPack, GitReceivePack }

    public interface IEntitySharedRepository<SHARED, CLONED, PAIR, ENTITY> : IEntityRepository<ENTITY, PAIR, SHARED, CLONED>, ISharedRepository
        where ENTITY : IRepositoriesMountedEntity<ENTITY, PAIR, SHARED, CLONED>
        where PAIR : IEntityRepositoryPair<PAIR, SHARED, CLONED, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, PAIR, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, PAIR, ENTITY>
    {

    }
    public interface IEntityClonedRepository<CLONED, SHARED, PAIR, ENTITY> : IEntityRepository<ENTITY, PAIR, SHARED, CLONED>, IClonedRepository
        where ENTITY : IRepositoriesMountedEntity<ENTITY, PAIR, SHARED, CLONED>
        where PAIR : IEntityRepositoryPair<PAIR, SHARED, CLONED, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, PAIR, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, PAIR, ENTITY>
    {
    }

    public interface IRepositoryPair
    {
        public void Create();
        public void ResetRemoteUrl();
        public ISharedRepository SharedRepository { get; }
        public IClonedRepository ClonedRepository { get; }
    }

    public interface IEntityRepositoryPair<ME, SHARED, CLONED, ENTITY> : IRepositoryPair
        where ME : IEntityRepositoryPair<ME, SHARED, CLONED, ENTITY>
        where SHARED : IEntitySharedRepository<SHARED, CLONED, ME, ENTITY>
        where CLONED : IEntityClonedRepository<CLONED, SHARED, ME, ENTITY>
        where ENTITY : IRepositoriesMountedEntity<ENTITY, ME, SHARED, CLONED>
    {
        public ENTITY Entity { get; }
        public new SHARED SharedRepository { get; }
        public new CLONED ClonedRepository { get; }

    }

}
