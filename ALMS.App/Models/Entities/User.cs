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
using Microsoft.Extensions.Configuration;

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

        public bool IsLdapUser { get; set; }
        public bool IsLdapInitialized { get; set; }

        [NotMapped]
        public string Password { get; set; }

        public virtual ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
        public virtual ICollection<Sandbox> Sandboxes { get; set; } = new List<Sandbox>();
        public virtual ICollection<LectureUser> LectureUsers { get; set; } = new List<LectureUser>();



        public string DirectoryPath => $"/data/users/{Account}";

        public void CreateDirectory(DatabaseContext context, IConfiguration config)
        {
            var dir = new DirectoryInfo(DirectoryPath);
            dir.Create();
        }

        public void UpdateDirectory(DatabaseContext context, IConfiguration config, User previous)
        {
            if (previous.Account != Account)
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

            EncryptedPassword = Encrypt(Password, config);

            context.Add(this);
        }

        public void Update(DatabaseContext context, IConfiguration config, User previous)
        {
            UpdateDirectory(context, config, previous);

            if (string.IsNullOrEmpty(this.Password))
            {
                EncryptedPassword = previous.EncryptedPassword;
            }
            else
            {
                EncryptedPassword = Encrypt(Password, config);
            }

            var me = GetEntityForEditOrRemove(context, config);
            foreach (var x in me.LectureUsers)
            {
                x.UpdateParent(context, config, this, previous);
            }
            foreach (var x in me.Lectures)
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
            foreach (var x in me.Lectures)
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

        public bool ServerSideValidationOnCreate(DatabaseContext context, IConfiguration config, Action<string, string> AddValidationError)
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
        public bool ServerSideValidationOnUpdate(DatabaseContext context, IConfiguration config, Action<string, string> AddValidationError)
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




        public bool Authenticate(string password, IConfiguration config)
        {
            if (IsLdapUser)
            {
                var ldap_host = Environment.GetEnvironmentVariable("LDAP_HOST");
                var ldap_port = int.Parse(Environment.GetEnvironmentVariable("LDAP_PORT"));
                var ldap_base = Environment.GetEnvironmentVariable("LDAP_BASE");
                var ldap_id_attr = Environment.GetEnvironmentVariable("LDAP_ID_ATTR");
                var ldap_mail_attr = Environment.GetEnvironmentVariable("LDAP_MAIL_ATTR");
                var ldap_name_attr = Environment.GetEnvironmentVariable("LDAP_NAME_ATTR");
                var authenticator = new LdapAuthenticator(ldap_host, ldap_port, ldap_base, ldap_id_attr, entry => {
                    var attrs = entry.getAttributeSet();
                    var email = attrs.getAttribute(ldap_mail_attr).StringValue;
                    var xs = ldap_name_attr.Split(";");
                    string name = null;
                    if (xs.Length == 1)
                    {
                        name = attrs.getAttribute(xs[0]).StringValue;
                    }
                    else
                    {
                        name = attrs.getAttribute(xs[0], xs[1]).StringValue;
                    }
                    return (name, email);
                });
                var (result, name, email) = authenticator.Authenticate(Account, password);
                if(result && !IsLdapInitialized)
                {
                    DisplayName = name;
                    EmailAddress = email;
                }
                return result;
            }
            else
            {
                return EncryptedPassword == Encrypt(password, config);
            }
        }

        public bool IsTeacher(Lecture lecture)
        {
            return lecture.GetTeachers().Select(x => x.Id).Contains(Id);
        }
        public bool IsStudent(Lecture lecture)
        {
            return lecture.GetStudents().Select(x => x.Id).Contains(Id);
        }

        public static string Encrypt(string rawPassword, IConfiguration config)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                var key = config.GetValue<string>("ApplicationKey");

                rijndael.IV = Encoding.UTF8.GetBytes(key.Substring(0, 16));
                rijndael.Key = Encoding.UTF8.GetBytes(key.Substring(16, 16));
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

        public void PrepareModelForAddNew(DatabaseContext context, IConfiguration config) { }

        public void PrepareModelForEdit(DatabaseContext context, IConfiguration config, User original) { }

        public User GetEntityForEditOrRemove(DatabaseContext context, IConfiguration config) => context.Users.Where(x => x.Id == Id).Include(x => x.Lectures).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.Lecture).FirstOrDefault();
        public User GetEntityAsNoTracking(DatabaseContext context, IConfiguration config) => context.Users.Where(x => x.Id == Id).Include(x => x.Lectures).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.Lecture).AsNoTracking().FirstOrDefault();

    }
}