using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.IO;
using ALMS.App.Components.Admin;
using Microsoft.EntityFrameworkCore;


namespace ALMS.App.Models.Entities
{

    public class User : IDirectoryMountedEntity<User>, IEditableEntity<User>
    {
        public int Id { get; set; }
        [RegularExpression(@"^[_A-Za-z][_A-Za-z0-9-]+$")]
        [Required, StringLength(32)]
        public string Account { get; set; }

        [Required, StringLength(64), RegularExpression("^[^\\\\\\\"]+$")]
        public string DisplayName { get; set; }

        [Required, EmailAddress]
        public string EmailAddress { get; set; }

        public string EncryptedPassword { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSenior { get; set; }

        [NotMapped]
        public string Password { get; set; }

        public virtual ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        public virtual ICollection<Sandbox> Sandboxes { get; set; } = new List<Sandbox>();
        public virtual ICollection<LectureUser> LectureUsers { get; set; } = new List<LectureUser>();



        public string DirectoryPath => $"/data/users/{Account}";

        public void CreateDirectory(DatabaseContext context)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            dir.Create();
        }

        public void UpdateDirectory(DatabaseContext context, User previous)
        {
            if (previous.Account != Account)
            {
                Directory.Move(previous.DirectoryPath, DirectoryPath);
            }
        }

        public void RemoveDirectory(DatabaseContext context)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            if (dir.Exists) { dir.Delete(true); }
        }

        public void CreateNew(DatabaseContext context)
        {
            CreateDirectory(context);

            EncryptedPassword = Encrypt(Password);

            context.Add(this);
        }

        public void Update(DatabaseContext context, User previous)
        {
            UpdateDirectory(context, previous);

            if (string.IsNullOrEmpty(this.Password))
            {
                EncryptedPassword = previous.EncryptedPassword;
            }
            else
            {
                EncryptedPassword = Encrypt(Password);
            }

            var me = GetEntityForEditOrRemove(context);
            foreach (var x in me.LectureUsers)
            {
                x.UpdateParent(context, this, previous);
            }
            foreach (var x in me.Lectures)
            {
                x.UpdateParent(context, this, previous);
            }
            foreach (var x in me.Sandboxes)
            {
                x.UpdateParent(context, this, previous);
            }
            context.Update(this);
        }

        public void Remove(DatabaseContext context)
        {

            var me = GetEntityForEditOrRemove(context);
            foreach (var x in me.LectureUsers)
            {
                x.Remove(context);
            }
            foreach (var x in me.Lectures)
            {
                x.Remove(context);
            }
            foreach (var x in me.Sandboxes)
            {
                x.Remove(context);
            }
            RemoveDirectory(context);
            context.Remove(this);
        }

        public bool ServerSideValidationOnCreate(DatabaseContext context, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.Users.Where(u => u.Account == Account).FirstOrDefault();
            if (instance != null)
            {
                AddValidationError("Account", "Arleady exist");
                result = false;
            }
            if (string.IsNullOrEmpty(Password))
            {
                AddValidationError("Password", "The Password field is required.");
                result = false;
            }
            return result;
        }
        public bool ServerSideValidationOnUpdate(DatabaseContext context, Action<string, string> AddValidationError)
        {
            bool result = true;
            var instance = context.Users.Where(u => u.Account == Account).FirstOrDefault();
            if (instance != null && instance.Id != Id)
            {
                AddValidationError("Account", "Arleady exist");
                result = false;
            }
            return result;
        }




        public bool Authenticate(string password)
        {
            return this.EncryptedPassword == Encrypt(password);
        }

        public bool IsTeacher(Lecture lecture)
        {
            return lecture.GetTeachers().Select(x => x.Id).Contains(Id);
        }
        public bool IsStudent(Lecture lecture)
        {
            return lecture.GetStudents().Select(x => x.Id).Contains(Id);
        }

        public static string Encrypt(string rawPassword)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(@"sfdvawh(Yhwoeirg");
                rijndael.Key = Encoding.UTF8.GetBytes(@"jkdvH09BHvqedahj");

                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(rawPassword);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }

        public void PrepareModelForAddNew(DatabaseContext context) { }

        public void PrepareModelForEdit(DatabaseContext context, User original) { }

        public User GetEntityForEditOrRemove(DatabaseContext context) => context.Users.Where(x => x.Id == Id).Include(x => x.Lectures).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.Lecture).FirstOrDefault();
        public User GetEntityAsNoTracking(DatabaseContext context) => context.Users.Where(x => x.Id == Id).Include(x => x.Lectures).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.Lecture).AsNoTracking().FirstOrDefault();

    }

}