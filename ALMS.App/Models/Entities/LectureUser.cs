using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Models.Entities
{
    public enum LectureUserRole
    {
        Teacher = 1, Student = 2
    }
    public class LectureUser :
        IDirectoryMountedEntity<LectureUser>,
        IChildEntity<LectureUser, User>,
        IChildEntity<LectureUser, Lecture>,
        IRepositoriesMountedEntity<LectureUser, LectureUserHomeRepositoryPair, LectureUserHomeSharedRepository, LectureUserHomeClonedRepository>
    {

        [Required, ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }

        [Required, ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }
        [Required]
        public LectureUserRole Role { get; set; }

        private LectureUserHomeRepositoryPair LectureUserHomeRepositoryPair;
        public LectureUser()
        {
            LectureUserHomeRepositoryPair = new LectureUserHomeRepositoryPair(this);
        }

        public string DirectoryPath => $"{User.DirectoryPath}/lecture_data/{Lecture.Name}";

        [NotMapped]
        User IChildEntity<LectureUser, User>.Parent { get { return User; } set { User = value; } }
        [NotMapped]
        Lecture IChildEntity<LectureUser, Lecture>.Parent { get { return Lecture; } set { Lecture = value; } }

        public IEntityRepositoryPair<LectureUserHomeRepositoryPair, LectureUserHomeSharedRepository, LectureUserHomeClonedRepository, LectureUser> RepositoryPair => LectureUserHomeRepositoryPair;

        public void CreateDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            dir.Create();
        }
        public void UpdateDirectory(DatabaseContext context, IConfiguration config, LectureUser previous)
        {
            if (previous.User.Account != User.Account || previous.Lecture.Name != Lecture.Name)
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

            // Create repositories
            LectureUserHomeRepositoryPair.Create();
            using (var x = new StreamWriter($"{LectureUserHomeRepositoryPair.ClonedRepository.DirectoryPath}/.keep"))
            {
                x.WriteLine("");
            }
            LectureUserHomeRepositoryPair.ClonedRepository.CommitChanges("Initial Commit", User.DisplayName, User.EmailAddress);
            LectureUserHomeRepositoryPair.ClonedRepository.Push();

            context.Add(this);
        }
        public void Update(DatabaseContext context, IConfiguration config, LectureUser previous)
        {
            UpdateDirectory(context, config, previous);

            // reset repositories
            if (previous.User.Account != User.Account || previous.Lecture.Name != Lecture.Name)
            {
                LectureUserHomeRepositoryPair.ResetRemoteUrl();
            }

            context.Update(this);
        }
        public void Remove(DatabaseContext context, IConfiguration config)
        {
            RemoveDirectory(context, config);
            context.Remove(this);
        }

        public void UpdateParent(DatabaseContext context, IConfiguration config, User successor, User previous)
        {
            if (previous.Account != User.Account)
            {
                LectureUserHomeRepositoryPair.ResetRemoteUrl();
            }
        }

        public void UpdateParent(DatabaseContext context, IConfiguration config, Lecture successor, Lecture previous)
        {
            if (previous.Name != Lecture.Name)
            {
                Directory.Move($"{User.DirectoryPath}/lecture_data/{previous.Name}", $"{User.DirectoryPath}/lecture_data/{successor.Name}");
                LectureUserHomeRepositoryPair.ResetRemoteUrl();
            }
        }
    }
}
