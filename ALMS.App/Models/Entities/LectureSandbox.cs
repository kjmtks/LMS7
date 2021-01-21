using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Models.Entities
{
    public class LectureSandbox : SandboxBase<LectureSandbox>,
        IChildEntity<LectureSandbox, Lecture>,
        IDirectoryMountedEntity<LectureSandbox>,
        IEditableEntity<LectureSandbox>
    {
        public int Id { get; set; }
        
        [RegularExpression(@"^[_A-Za-z][_A-Za-z0-9-]+$")]
        [Required, StringLength(32)]
        public override string Name { get; set; }
        public string Description { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }

        [NotMapped]
        public Sandbox Original { get; set; }


        [NotMapped]
        public Lecture Parent { get { return Lecture; } set { Lecture = value; } }

        public LectureSandbox GetEntityForEditOrRemove(DatabaseContext context, IConfiguration config) => context.LectureSandboxes.Where(x => x.Id == Id).Include(x=> x.Lecture).ThenInclude(x => x.Owner).FirstOrDefault();
        public LectureSandbox GetEntityAsNoTracking(DatabaseContext context, IConfiguration config) => context.LectureSandboxes.Where(x => x.Id == Id).Include(x => x.Lecture).ThenInclude(x => x.Owner).AsNoTracking().FirstOrDefault();
        public override string DirectoryPath => $"{Lecture.DirectoryPath}/sandboxes/{Name}";


        public override void CreateDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            if (dir.Exists) { dir.Delete(true); }
            if (!dir.Parent.Exists) { dir.Parent.Create(); }
            
            Console.WriteLine($"{Original.DirectoryPath} => {DirectoryPath}");
            Process.Start("/bin/sh", $"-c \"cp -rax {Original.DirectoryPath} {DirectoryPath}\"").WaitForExit();


        }
        public override void UpdateDirectory(DatabaseContext context, IConfiguration config, LectureSandbox previous)
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

            // copy /etc/passwd, /etc/group
            File.Copy($"{DirectoryPath}/etc/passwd", $"{DirectoryPath}/etc/passwd.original");
            File.Copy($"{DirectoryPath}/etc/group", $"{DirectoryPath}/etc/group.original");

            // mounting user homes
            SetUsers();

        }
        public override void Update(DatabaseContext context, IConfiguration config, LectureSandbox previous)
        {
            UpdateDirectory(context, config, previous);
            context.Update(this);
        }
        public override void Remove(DatabaseContext context, IConfiguration config)
        {
            // unmounting
            Console.WriteLine("Unmounting user...");
            if (Directory.Exists($"{DirectoryPath}/home"))
            {
                foreach (var account in new DirectoryInfo($"{DirectoryPath}/home").GetDirectories().Select(x => x.Name))
                {
                    if (Directory.Exists($"{DirectoryPath}/home/{account}"))
                    {
                        Console.WriteLine($"unmounting: {DirectoryPath}/home/{account}");
                        System.Diagnostics.Process.Start("umount", $"{DirectoryPath}/home/{account}").WaitForExit();
                        Console.WriteLine($"removing: {DirectoryPath}/home/{account}");
                        System.Diagnostics.Process.Start("rm", $"-rf {DirectoryPath}/home/{account}").WaitForExit();
                    }
                }
            }
            Console.WriteLine("Unmounting lecture...");
            if (Directory.Exists($"{DirectoryPath}/lecture"))
            {
                Console.WriteLine($"unmounting: {DirectoryPath}/lecture");
                System.Diagnostics.Process.Start("umount", $"{DirectoryPath}/lecture").WaitForExit();
                Console.WriteLine($"removing: {DirectoryPath}/lecture");
                System.Diagnostics.Process.Start("rm", $"-rf {DirectoryPath}/lecture").WaitForExit();
            }

            RemoveDirectory(context, config);
            context.Remove(this);
        }
        public void UpdateParent(DatabaseContext context, IConfiguration config, Lecture successor, Lecture previous)
        {
            var a = string.Join(',', Lecture.LectureUsers.Select(x => x.UserId));
            var b = string.Join(',', previous.LectureUsers.Select(x => x.UserId));
            if(a != b)
            {
                SetUsers();
            }
        }


        public void PrepareModelForAddNew(DatabaseContext context, IConfiguration config) { }

        public void PrepareModelForEdit(DatabaseContext context, IConfiguration config, LectureSandbox original) { }


        public bool ServerSideValidationOnCreate(DatabaseContext context, IConfiguration config, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.LectureSandboxes.Where(u => u.Name == Name && u.LectureId == LectureId).FirstOrDefault();
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
            var instance = context.LectureSandboxes.Where(u => u.Name == Name && u.LectureId == LectureId).FirstOrDefault();
            if (instance != null && instance.Id != Id)
            {
                AddValidationError("Name", "Arleady exist");
                result = false;
            }
            return result;
        }




        public void SetUsers()
        {
            var users = Lecture.LectureUsers.Select(x => x.User).Distinct();
            var sb1 = new System.Text.StringBuilder();
            using (var r = new StreamReader($"{DirectoryPath}/etc/passwd.original"))
            {
                sb1.AppendLine(r.ReadToEnd());
            }
            foreach (var user in users)
            {
                sb1.AppendLine($"{user.Account}:x:{user.Id + 1000}:{user.Id + 1000}:{user.Account}:/home/{user.Account}:/bin/bash");
            }
            using (var f = new System.IO.FileStream($"{DirectoryPath}/etc/passwd", System.IO.FileMode.Create))
            {
                using (var w = new System.IO.StreamWriter(f))
                {
                    w.Write(sb1.ToString());
                }
            }

            var sb2 = new System.Text.StringBuilder();
            using (var r = new StreamReader($"{DirectoryPath}/etc/group.original"))
            {
                sb2.AppendLine(r.ReadToEnd());
            }
            foreach (var user in users)
            {
                sb2.AppendLine($"{user.Account}:x:{user.Id + 1000}:");
            }
            using (var f = new System.IO.FileStream($"{DirectoryPath}/etc/group", System.IO.FileMode.Create))
            {
                using (var w = new System.IO.StreamWriter(f))
                {
                    w.Write(sb2.ToString());
                }
            }

            foreach (var user in users)
            {
                var h = new System.IO.DirectoryInfo($"{DirectoryPath}/home/{user.Account}");
                if (!h.Exists)
                {
                    h.Create();
                }
                var f = new System.IO.DirectoryInfo($"{user.DirectoryPath}/lecture_data/{Lecture.Owner.Account}/{Lecture.Name}/home");
                if (!f.Exists)
                {
                    f.Create();
                }
                Console.WriteLine($"mounting: {f.FullName} {h.FullName}");
                System.Diagnostics.Process.Start("chmod", $"700 {f.FullName}").WaitForExit();
                System.Diagnostics.Process.Start("chown", $"{user.Id + 1000}:{user.Id + 1000} {f.FullName}").WaitForExit();
                System.Diagnostics.Process.Start("mount", $"--bind {f.FullName} {h.FullName}").WaitForExit();

                // Future consideration: use /etc/fstab such like: $"{f.FullName}   {h.FullName}   none   bind  0 0" 

            }
            var accounts = users.Select(x => x.Account);
            foreach (var account in new DirectoryInfo($"{DirectoryPath}/home").GetDirectories().Select(x => x.Name))
            {
                if(!accounts.Contains(account))
                {
                    Console.WriteLine($"unmounting: {DirectoryPath}/home/{account}");
                    System.Diagnostics.Process.Start("umount", $"{DirectoryPath}/home/{account}").WaitForExit();
                    Console.WriteLine($"removing: {DirectoryPath}/home/{account}");
                    System.Diagnostics.Process.Start("rm", $"-rf {DirectoryPath}/home/{account}").WaitForExit();
                }
            }
        }

    }
}
