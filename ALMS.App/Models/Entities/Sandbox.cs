using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Models.Entities
{
    public class Sandbox : SandboxBase<Sandbox>,
        IChildEntity<Sandbox, User>,
        IDirectoryMountedEntity<Sandbox>,
        IEditableEntity<Sandbox>
    {
        public int Id { get; set; }
        
        [RegularExpression(@"^[_A-Za-z][_A-Za-z0-9-.]+$")]
        [Required, StringLength(32)]
        public override string Name { get; set; }
        public string Description { get; set; }

        [NotMapped]
        public string SetupCommands { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; }


        [NotMapped]
        public User Parent { get { return Owner; } set { Owner = value; } }

        public override string DirectoryPath => $"{Owner.DirectoryPath}/sandboxes/{Name}";

        public Sandbox GetEntityForEditOrRemove(DatabaseContext context, IConfiguration config) => context.Sandboxes.Where(x => x.Id == Id).Include(x => x.Owner).FirstOrDefault();
        public Sandbox GetEntityAsNoTracking(DatabaseContext context, IConfiguration config) => context.Sandboxes.Where(x => x.Id == Id).Include(x => x.Owner).AsNoTracking().FirstOrDefault();

        public void PrepareModelForAddNew(DatabaseContext context, IConfiguration config)
        {
            SetupCommands = "";
        }

        public void PrepareModelForEdit(DatabaseContext context, IConfiguration config, Sandbox original)
        {
            SetupCommands = "";
        }


        public void UpdateParent(DatabaseContext context, IConfiguration config, User successor, User previous) { }


        public override void CreateDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            dir.Create();
        }

        public override void UpdateDirectory(DatabaseContext context, IConfiguration config, Sandbox previous)
        {
            if (previous.Name != Name)
            {
                Directory.Move(previous.DirectoryPath, DirectoryPath);
            }
        }

        public override void RemoveDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            if (dir.Exists) { dir.Delete(true); }
        }

        public override void CreateNew(DatabaseContext context, IConfiguration config)
        {
            CreateDirectory(context, config);
            context.Add(this);
        }

        public override void Update(DatabaseContext context, IConfiguration config, Sandbox previous)
        {
            UpdateDirectory(context, config, previous);
            context.Update(this);
        }

        public override void Remove(DatabaseContext context, IConfiguration config)
        {
            RemoveDirectory(context, config);
            context.Remove(this);
        }


        public bool ServerSideValidationOnCreate(DatabaseContext context, IConfiguration config, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.Sandboxes.Where(u => u.Name == Name && u.OwnerId == OwnerId).FirstOrDefault();
            if (instance != null)
            {
                AddValidationError("Name", "Arleady exist");
                result = false;
            }
            return result;
        }

        public bool ServerSideValidationOnUpdate(DatabaseContext context, IConfiguration config, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.Sandboxes.Where(u => u.Name == Name && u.OwnerId == OwnerId).FirstOrDefault();
            if (instance != null && instance.Id != Id)
            {
                AddValidationError("Name", "Arleady exist");
                result = false;
            }
            return result;
        }


        public static async Task<Sandbox> CloneSandboxDirectoryAsync(DatabaseContext context, IConfiguration config, string name, Sandbox from, User user)
        {
            var to = new Sandbox()
            {
                Name = name,
                Description = from.Description,
                OwnerId = user.Id,
                Owner = user
            };
            await to.DoAsync($"cp -rax {from.DirectoryPath} {to.DirectoryPath}");
            context.Add(to);
            return to;
        }


    }


}
