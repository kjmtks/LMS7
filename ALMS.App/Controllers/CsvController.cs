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


            var scorings = lecture.GetScorings().Children ?? new Models.LectureScoring[]{ };

            var sb = new System.Text.StringBuilder();
            sb.Append("account,name");
            foreach (var scoring in scorings)
            {
                sb.Append($",{scoring.Name}");
            }
            sb.AppendLine();
            foreach (var u in lecture.GetStudents())
            {
                sb.Append($"{u.Account},{u.DisplayName}");
                foreach (var scoring in scorings)
                {
                    var path = new FileInfo($"{lecture.DirectoryPath}/submissions/{u.Account}/{scoring.Name}/SCORE");
                    if (path.Exists)
                    {
                        using (var fs = path.OpenText())
                        {
                            sb.Append($",{fs.ReadToEnd()}");
                        }
                    }
                    else
                    {
                        sb.Append(",");
                    }
                }
                sb.AppendLine();
            }

            return File(sb.ToString(), "text/csv");
        }

    }
}
