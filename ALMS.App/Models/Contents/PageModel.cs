using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace ALMS.App.Models.Contents
{
    public class PageModel
    {
        private Entities.Lecture lecture { get; }

        public Lecture Lecture { get; }
        public User User { get; }
        public IEnumerable<User> Teachers { get; }
        public IEnumerable<User> Students { get; }
        public CommitInfo CommitInfo { get; }

        public string CurrentPagePath { get; }
        public string Branch { get; }
        public System.Dynamic.ExpandoObject ViewBag { get; }
        public PageModel(Entities.Lecture lecture, Entities.User user, CommitInfo commitInfo, string current_page_path, string branch, System.Dynamic.ExpandoObject viewBag)
        {
            Lecture = new Lecture(lecture);
            CommitInfo = commitInfo;
            CurrentPagePath = current_page_path;
            Branch = branch;
            ViewBag = viewBag;
            User = new User(user, user.IsTeacher(lecture) ? Role.Teacher : Role.Studnet);
            Teachers = lecture.GetTeachers().Where(x => x.IsTeacher(lecture) ).Select(x => new User(x, Role.Teacher));
            Students = lecture.GetTeachers().Where(x => x.IsStudent(lecture)).Select(x => new User(x, Role.Studnet));
        }


        public string DateTimeToString(DateTime dt)
        {
            return Utility.DateTimeToString(dt);
        }
        public bool HasParameter(string parameterName)
        {
            return ((IDictionary<string, object>)ViewBag).ContainsKey(parameterName);
        }
    }

    //-----

    public class Utility
    {
        public static string DateTimeToString(DateTime dt)
        {
            return dt.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
    }

    public class Lecture
    {
        public string Name { get; }
        public User Owner { get; }
        public string Subject { get; }
        public Lecture(Entities.Lecture lecture)
        {
            Name = lecture.Name;
            Subject = lecture.Subject;
            Owner = new User(lecture.Owner, lecture.Owner.IsTeacher(lecture) ? Role.Teacher : Role.Studnet);
        }
    }

    public class User
    {
        public string Account { get; }
        public string DisplayName { get; }
        public string EmailAddress { get; }
        public Role Role { get; }
        public User(Entities.User user, Role role)
        {
            Account = user.Account;
            DisplayName = user.DisplayName;
            EmailAddress = user.EmailAddress;
            Role = role;
        }
    }

    public enum Role { Teacher, Studnet }
}
