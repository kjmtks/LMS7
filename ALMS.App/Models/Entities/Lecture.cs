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
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Models.Entities
{

    public class Lecture :
        IChildEntity<Lecture, User>,
        IDirectoryMountedEntity<Lecture>,
        IRepositoriesMountedEntity<Lecture, LectureContentsRepositoryPair, LectureContentsSharedRepository, LectureContentsClonedRepository>,
        IRepositoriesMountedEntity<Lecture, LectureSubmissionsRepositoryPair, LectureSubmissionsSharedRepository, LectureSubmissionsClonedRepository>,
        IEditableEntity<Lecture>
    {
        public int Id { get; set; }
        
        [RegularExpression(@"^[_A-Za-z][_A-Za-z0-9-]+$")]
        [Required, StringLength(32)]
        public string Name { get; set; }

        [Required, StringLength(64)]
        public string Subject { get; set; }
        public string Description { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; }

        public virtual ICollection<LectureSandbox> Sandboxes { get; set; } = new List<LectureSandbox>();
        public virtual ICollection<LectureUser> LectureUsers { get; set; } = new List<LectureUser>();

        [NotMapped]
        public string TeachersForEdit { get; set; }
        [NotMapped]
        public string StudentsForEdit { get; set; }

        [NotMapped]
        public bool IsEmptyRepositories { get; set; }

        [NotMapped]
        public User Parent { get { return Owner; } set { Owner = value; } }


        public LectureContentsRepositoryPair LectureContentsRepositoryPair { get; }
        public LectureSubmissionsRepositoryPair LectureSubmissionsRepositoryPair { get; }
        public Lecture()
        {
            LectureContentsRepositoryPair = new LectureContentsRepositoryPair(this);
            LectureSubmissionsRepositoryPair = new LectureSubmissionsRepositoryPair(this);
        }



        public string DirectoryPath => $"/data/users/{Owner.Account}/lectures/{Name}";

        IEntityRepositoryPair<LectureContentsRepositoryPair, LectureContentsSharedRepository, LectureContentsClonedRepository, Lecture>
            IRepositoriesMountedEntity<Lecture, LectureContentsRepositoryPair, LectureContentsSharedRepository, LectureContentsClonedRepository>.RepositoryPair => throw new NotImplementedException();

        IEntityRepositoryPair<LectureSubmissionsRepositoryPair, LectureSubmissionsSharedRepository, LectureSubmissionsClonedRepository, Lecture>
            IRepositoriesMountedEntity<Lecture, LectureSubmissionsRepositoryPair, LectureSubmissionsSharedRepository, LectureSubmissionsClonedRepository>.RepositoryPair => throw new NotImplementedException();

        public IEnumerable<User> GetTeachers() { return LectureUsers.Where(x => x.Role == LectureUserRole.Teacher).Select(x => x.User); }
        public IEnumerable<User> GetStudents() { return LectureUsers.Where(x => x.Role == LectureUserRole.Student).Select(x => x.User); }



        public void CreateDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            dir.Create();
        }

        public void UpdateDirectory(DatabaseContext context, IConfiguration config, Lecture previous)
        {
            if (previous.Name != Name)
            {
                Directory.Move(previous.DirectoryPath, DirectoryPath);
            }
        }

        public void RemoveDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            if (dir.Exists) { dir.Delete(true); }
        }

        public void CreateNew(DatabaseContext context, IConfiguration config)
        {
            CreateDirectory(context, config);

            // parse teachers and students
            var flts = context.Users.Select(x => x.Account);
            var ts = TeachersForEdit?.Split(' ', ',', '\t', '\n', '\r') ?? new string[0]{ };
            var ss = StudentsForEdit?.Split(' ', ',', '\t', '\n', '\r')?.Except(ts) ?? new string[0] { };
            foreach (var x in ts.Intersect(flts))
            {
                var user = context.Users.Where(y => y.Account == x).FirstOrDefault();
                var a = new LectureUser() { Lecture = this, User = user, Role = LectureUserRole.Teacher };
                LectureUsers.Add(a);
                a.CreateNew(context, config);
            }
            foreach (var x in ss.Intersect(flts))
            {
                var user = context.Users.Where(y => y.Account == x).FirstOrDefault();
                var a = new LectureUser() { Lecture = this, User = user, Role = LectureUserRole.Student };
                LectureUsers.Add(a);
                a.CreateNew(context, config);
            }

            // Create repositories
            LectureContentsRepositoryPair.Create();
            Console.WriteLine($"==> {IsEmptyRepositories}");
            if (!IsEmptyRepositories)
            {
                var cc = new DirectoryInfo(LectureContentsRepositoryPair.ClonedRepository.DirectoryPath);
                var pages = cc.CreateSubdirectory("pages");
                using (var x = new StreamWriter($"{pages.FullName}/@index.md"))
                {
                    x.WriteLine($"# {Subject}");
                }
                var activities = cc.CreateSubdirectory("activities");
                using (var x = new StreamWriter($"{activities.FullName}/.keep"))
                {
                    x.WriteLine("");
                }
                LectureContentsRepositoryPair.ClonedRepository.CommitChanges("Initial Commit", Owner.DisplayName, Owner.EmailAddress);
                LectureContentsRepositoryPair.ClonedRepository.Push();
            }

            LectureSubmissionsRepositoryPair.Create();
            using (var x = new StreamWriter($"{LectureSubmissionsRepositoryPair.ClonedRepository.DirectoryPath}/.keep"))
            {
                x.WriteLine("");
            }
            LectureSubmissionsRepositoryPair.ClonedRepository.CommitChanges("Initial Commit", Owner.DisplayName, Owner.EmailAddress);
            LectureSubmissionsRepositoryPair.ClonedRepository.Push();

            context.Add(this);
        }

        public void Update(DatabaseContext context, IConfiguration config, Lecture previous)
        {
            UpdateDirectory(context, config, previous);

            // reset repositories
            if(previous.Name != Name)
            {
                LectureContentsRepositoryPair.ResetRemoteUrl();
                LectureSubmissionsRepositoryPair.ResetRemoteUrl();
            }

            // parse teachers and students
            var flts = context.Users.Select(x => x.Account);
            var newTeachers = TeachersForEdit?.Split(' ', ',', '\t', '\n', '\r') ?? new string[0] { };
            var newStudents = StudentsForEdit?.Split(' ', ',', '\t', '\n', '\r')?.Except(newTeachers) ?? new string[0] { };
            var teachers = GetTeachers().Select(x => x.Account);
            var students = GetStudents().Select(x => x.Account);
            foreach (var user in context.Users)
            {
                if (newTeachers.Contains(user.Account) && !teachers.Contains(user.Account))
                {
                    var a = new LectureUser() { Lecture = this, User = user, Role = LectureUserRole.Teacher };
                    LectureUsers.Add(a);
                    a.CreateNew(context, config);
                }
                if (!newTeachers.Contains(user.Account) && teachers.Contains(user.Account))
                {
                    var a = LectureUsers.Where(x => x.UserId == user.Id && x.LectureId == Id && x.Role == LectureUserRole.Teacher).FirstOrDefault();
                    LectureUsers.Remove(a);
                    a.Remove(context, config);
                }

                if (newStudents.Contains(user.Account) && !students.Contains(user.Account))
                {
                    var a = new LectureUser() { Lecture = this, User = user, Role = LectureUserRole.Student };
                    LectureUsers.Add(a);
                    a.CreateNew(context, config);
                }
                if (!newStudents.Contains(user.Account) && students.Contains(user.Account))
                {
                    var a = LectureUsers.Where(x => x.UserId == user.Id && x.LectureId == Id && x.Role == LectureUserRole.Student).FirstOrDefault();
                    LectureUsers.Remove(a);
                    a.Remove(context, config);
                }
            }



            var me = GetEntityForEditOrRemove(context, config);
            foreach (var x in me.LectureUsers)
            {
                x.UpdateParent(context, config, this, previous);
            }
            foreach (var x in me.Sandboxes)
            {
                x.UpdateParent(context, config, this, previous);
            }

            context.Update(this);
        }

        public void Remove(DatabaseContext context, IConfiguration config)
        {
            var me = GetEntityForEditOrRemove(context, config);
            foreach (var x in me.LectureUsers)
            {
                x.Remove(context, config);
            }
            foreach (var x in me.Sandboxes)
            {
                x.Remove(context, config);
            }

            RemoveDirectory(context, config);
            context.Remove(this);
        }

        public void UpdateParent(DatabaseContext context, IConfiguration config, User successor, User previous)
        {
            // reset repositories
            if (successor.Account != previous.Account)
            {
                LectureContentsRepositoryPair.ResetRemoteUrl();
                LectureSubmissionsRepositoryPair.ResetRemoteUrl();

                foreach (var x in LectureUsers)
                {
                    x.UpdateParent(context, config, successor, previous);
                }
            }
        }


        public bool ServerSideValidationOnCreate(DatabaseContext context, IConfiguration config, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.Lectures.Where(u => u.Name == Name && u.OwnerId == OwnerId).FirstOrDefault();
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
            var instance = context.Lectures.Where(u => u.Name == Name && u.OwnerId == OwnerId).FirstOrDefault();
            if (instance != null && instance.Id != Id)
            {
                AddValidationError("Name", "Arleady exist");
                result = false;
            }
            return result;
        }
        public void PrepareModelForAddNew(DatabaseContext context, IConfiguration config)
        {
            TeachersForEdit = Owner.Account;
        }
        public void PrepareModelForEdit(DatabaseContext context, IConfiguration config, Lecture original)
        {
            TeachersForEdit = string.Join(" ", original.GetTeachers().Select(x => x.Account));
            StudentsForEdit = string.Join(" ", original.GetStudents().Select(x => x.Account));
        }
        public Lecture GetEntityForEditOrRemove(DatabaseContext context, IConfiguration config) => context.Lectures.Where(x => x.Id == Id).Include(x => x.Owner).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.User).FirstOrDefault();
        public Lecture GetEntityAsNoTracking(DatabaseContext context, IConfiguration config) => context.Lectures.Where(x => x.Id == Id).Include(x => x.Owner).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.User).AsNoTracking().FirstOrDefault();

 
        public string ReadPageFile(string page_path, string branch = "master")
        {
            var text = LectureContentsRepositoryPair.ReadFileWithoutTypeCheck($"pages/{page_path}", branch);
            using (var r = new StreamReader(text))
            {
                return r.ReadToEnd();
            }
        }
        public string ReadActivityFile(string page_path, string branch = "master")
        {
            var text = LectureContentsRepositoryPair.ReadFileWithoutTypeCheck($"activities/{page_path}", branch);
            using (var r = new StreamReader(text))
            {
                return r.ReadToEnd();
            }
        }

        public LectureParameters GetParameters(string branch = "master")
        {
            try
            {
                var text = LectureContentsRepositoryPair.ReadFileWithoutTypeCheck("parameters.xml", branch);
                XmlSerializer serializer = new XmlSerializer(typeof(LectureParameters));
                LectureParameters parameters = (LectureParameters)serializer.Deserialize(text);
                return parameters;
            }
            catch(FileNotFoundException)
            {
                return new LectureParameters();
            }
        }
        public LectureScorings GetScorings(string branch = "master")
        {
            try
            {
                var text = LectureContentsRepositoryPair.ReadFileWithoutTypeCheck("scorings.xml", branch);
                XmlSerializer serializer = new XmlSerializer(typeof(LectureScorings));
                LectureScorings scorings = (LectureScorings)serializer.Deserialize(text);
                return scorings;
            }
            catch (FileNotFoundException)
            {
                return new LectureScorings();
            }
        }
    }
}
