using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.IO;
using ALMS.App.Components.Admin;
using System.ComponentModel;
using System.Globalization;

namespace ALMS.App.Models.Entities
{
    public class SandboxTemplate : IEditableEntity<SandboxTemplate>
    {
        public int Id { get; set; }

        [RegularExpression(@"^[_A-Za-z][_A-Za-z0-9-]+$")]
        [Required, StringLength(32)]
        public string Name { get; set; }

        [Required, StringLength(64)]
        public string Subject { get; set; }
        public string Description { get; set; }
        public string SetupCommands { get; set; }


        public SandboxTemplate GetEntityForEditOrRemove(DatabaseContext context) => context.SandboxTemplates.Where(x => x.Id == Id).FirstOrDefault();
        public SandboxTemplate GetEntityAsNoTracking(DatabaseContext context) => context.SandboxTemplates.Where(x => x.Id == Id).AsNoTracking().FirstOrDefault();

        public void PrepareModelForAddNew(DatabaseContext context) { }

        public void PrepareModelForEdit(DatabaseContext context, SandboxTemplate original) { }
        public void CreateNew(DatabaseContext context)
        {
            context.Add(this);
        }

        public void Update(DatabaseContext context, SandboxTemplate previous)
        {
            context.Update(this);
        }

        public void Remove(DatabaseContext context)
        {
            context.Remove(this);
        }

        public bool ServerSideValidationOnCreate(DatabaseContext context, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.SandboxTemplates.Where(u => u.Name == Name).FirstOrDefault();
            if (instance != null)
            {
                AddValidationError("Name", "Arleady exist");
                result = false;
            }
            return result;
        }
        public bool ServerSideValidationOnUpdate(DatabaseContext context, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.SandboxTemplates.Where(u => u.Name == Name).FirstOrDefault();
            if (instance != null && instance.Id != Id)
            {
                AddValidationError("Name", "Arleady exist");
                result = false;
            }
            return result;
        }


    }
}
