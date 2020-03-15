using System.IO;
using System.Linq;
using ALMS.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;


namespace ALMS.App.Controllers
{
    public class RawFileController : Controller
    {
        DatabaseService DB;
        public RawFileController(DatabaseService db)
        {
            DB = db;
        }

        [HttpGet("/lecture/{owner_account}/{lecture_name}/contents/{branch}/{**path}")]
        public IActionResult LectureContentsFileRaw(string owner_account, string lecture_name, string branch, string path)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).Where(x => x.Owner.Account == owner_account && x.Name == lecture_name).FirstOrDefault();

            if (lecture == null) return new NotFoundResult();

            var ms = lecture.LectureContentsRepositoryPair.ReadFileWithoutTypeCheck(Path.Combine("pages", path), branch);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return File(ms, contentType);
            
        }
    }
}
