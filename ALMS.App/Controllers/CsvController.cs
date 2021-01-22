using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using ALMS.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Text;


namespace ALMS.App.Controllers
{

    [Authorize]
    public class CsvController : Controller
    {
        DatabaseService DB;
        public CsvController(DatabaseService db)
        {
            DB = db;
        }

        [HttpGet("lecture/{owner_account}/{lecture_name}/csv/{branch}/scores.csv")]
        public IActionResult ScoresCsv(string owner_account, string lecture_name, string branch)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Owner.Account == owner_account && x.Name == lecture_name).FirstOrDefault();
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
            }

            byte[] result;
            var scorings = lecture.GetScorings().Children ?? new Models.LectureScoring[]{ };
            using (var ms = new MemoryStream())
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (var w = new StreamWriter(ms, Encoding.GetEncoding("Shift_JIS")))
                {
                    w.Write("account,name");
                    foreach (var scoring in scorings)
                    {
                        w.Write($",{scoring.Name}");
                    }
                    w.WriteLine();


                    foreach (var u in lecture.GetStudents())
                    {
                        w.Write($"{u.Account},{u.DisplayName}");
                        foreach (var scoring in scorings)
                        {
                            var path = new FileInfo($"{lecture.DirectoryPath}/submissions/{u.Account}/{scoring.Name}/SCORE");
                            if (path.Exists)
                            {
                                using (var fs = path.OpenText())
                                {
                                    w.Write($",{fs.ReadToEnd()}");
                                }
                            }
                            else
                            {
                                w.Write(",");
                            }
                        }
                        w.WriteLine();
                    }
                }
                result = ms.ToArray();
            }



            return File(result, "text/csv");
        }

    }
}
