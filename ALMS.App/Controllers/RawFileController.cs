using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ALMS.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;


namespace ALMS.App.Controllers
{

    [Authorize]
    public class RawFileController : Controller
    {
        DatabaseService DB;
        public RawFileController(DatabaseService db)
        {
            DB = db;
        }

        [HttpGet("/lecture/{owner_account}/{lecture_name}/contents/{branch}/{**path}")]
        public IActionResult LectureContentsRawFile(string owner_account, string lecture_name, string branch, string path)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).Where(x => x.Owner.Account == owner_account && x.Name == lecture_name).FirstOrDefault();
            if (lecture == null)
            {
                return new NotFoundResult();
            }

            var user = DB.Context.Users.Include(x => x.Lectures).Include(x => x.LectureUsers).Where(x => x.Account == HttpContext.User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return new UnauthorizedResult();
            }
            var assignment = DB.Context.LectureUsers.Where(x => x.UserId == user.Id && x.LectureId == lecture.Id).FirstOrDefault();
            if (assignment == null)
            {
                return new UnauthorizedResult();
            }
            if(!user.IsTeacher(lecture))
            {
                if (branch != "master")
                {
                    return new UnauthorizedResult();
                }
                if(path.Split("/").FirstOrDefault() != "pages")
                {
                    return new UnauthorizedResult();
                }
            }

            var ms = lecture.LectureContentsRepositoryPair.ReadFileWithoutTypeCheck(path, branch);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return File(ms, contentType);
        }

        [HttpGet("/lecture/{owner_account}/{lecture_name}/submissions/{branch}/{**path}")]
        public IActionResult LectureSubmissionsRawFile(string owner_account, string lecture_name, string branch, string path)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).Where(x => x.Owner.Account == owner_account && x.Name == lecture_name).FirstOrDefault();
            if (lecture == null)
            {
                return new NotFoundResult();
            }

            var user = DB.Context.Users.Include(x => x.Lectures).Include(x => x.LectureUsers).ThenInclude(x => x.Lecture).Where(x => x.Account == HttpContext.User.Identity.Name).FirstOrDefault();
            if (user == null || !user.IsTeacher(lecture))
            {
                return new UnauthorizedResult();
            }

            var ms = lecture.LectureSubmissionsRepositoryPair.ReadFileWithoutTypeCheck(path, branch);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return File(ms, contentType);
        }

        [HttpGet("/lecture/{owner_account}/{lecture_name}/rawdata/{user_account}/{branch}/{**path}")]
        public IActionResult UserFiles(string owner_account, string lecture_name, string user_account, string branch, string path)
        {
            var login_user = DB.Context.Users.Include(x => x.Lectures).Include(x => x.LectureUsers).Where(x => x.Account == HttpContext.User.Identity.Name).FirstOrDefault();
            if (login_user == null)
            {
                return new UnauthorizedResult();
            }
            var lecture_user = DB.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.Lecture.Name == lecture_name && x.Lecture.Owner.Account == owner_account && x.User.Account == user_account).FirstOrDefault();
            if (lecture_user == null)
            {
                return new NotFoundResult();
            }

            var ms = lecture_user.RepositoryPair.ReadFileWithoutTypeCheck(path, branch);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return File(ms, contentType);
        }

        [HttpGet("/myfiles/{**path}")]
        public IActionResult UserFiles(string path)
        {
            var user = DB.Context.Users.Include(x => x.Lectures).Include(x => x.LectureUsers).Where(x => x.Account == HttpContext.User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return new UnauthorizedResult();
            }

            try
            {
                var ms = new MemoryStream();
                using (var r = new FileStream($"{user.DirectoryPath}/{path}", FileMode.Open, FileAccess.Read))
                {
                    r.CopyTo(ms);
                }

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(path, out var contentType))
                {
                    contentType = "application/octet-stream";
                }
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, contentType);
            }
            catch
            {
                return new NotFoundResult();
            }
        }
    }
}
