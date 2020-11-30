using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ALMS.App.Models;
using ALMS.App.Models.Entities;
using ALMS.App.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Controllers
{
    [ApiController]
    public class GitController : BasicAuthenticatableController
    {
        public GitController(DatabaseService db, IConfiguration config) : base(db, config)
        { }

        [HttpGet("api/git/lecture/{user_account}/{lecture_name}.contents.git/info/refs")]
        public IActionResult contents_info_refs(string lecture_name, string user_account, [FromQuery]string service)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Name == lecture_name && x.Owner.Account == user_account).FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, user =>
            {
                return info_refs(lecture.LectureContentsRepositoryPair, service, user);
            });
        }
        [HttpGet("api/git/lecture/{user_account}/{lecture_name}.submissions.git/info/refs")]
        public IActionResult data_info_refs(string lecture_name, string user_account, [FromQuery]string service)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Name == lecture_name && x.Owner.Account == user_account).FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, user =>
            {
                return info_refs(lecture.LectureSubmissionsRepositoryPair, service, user);
            });
        }

        [HttpPost("api/git/lecture/{user_account}/{lecture_name}.contents.git/git-upload-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult contents_git_upload_pack(string lecture_name, string user_account, [FromQuery]string service)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Name == lecture_name && x.Owner.Account == user_account).FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async user =>
            {
                return await git_upload_pack(lecture.LectureContentsRepositoryPair, user);
            });
        }
        [HttpPost("api/git/lecture/{user_account}/{lecture_name}.submissions.git/git-upload-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult data_git_upload_pack(string lecture_name, string user_account, [FromQuery]string service)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Name == lecture_name && x.Owner.Account == user_account).FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async user =>
            {
                return await git_upload_pack(lecture.LectureSubmissionsRepositoryPair, user);
            });
        }

        [HttpPost("api/git/lecture/{user_account}/{lecture_name}.contents.git/git-receive-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult contents_git_receive_pack(string lecture_name, string user_account, [FromQuery]string service)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Name == lecture_name && x.Owner.Account == user_account).FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async user =>
            {
                return await git_receive_pack(lecture.LectureContentsRepositoryPair, user);
            });
        }
        [HttpPost("api/git/lecture/{user_account}/{lecture_name}.submissions.git/git-receive-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult data_git_receive_pack(string lecture_name, string user_account, [FromQuery]string service)
        {
            var lecture = DB.Context.Lectures.Include(x => x.Owner).Include(x => x.LectureUsers).ThenInclude(x => x.User).Where(x => x.Name == lecture_name && x.Owner.Account == user_account).FirstOrDefault();
            if (lecture == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async user =>
            {
                return await git_receive_pack(lecture.LectureSubmissionsRepositoryPair, user);
            });
        }



        [HttpGet("api/git/lecture_data/{user_account}/{owner_account}/{lecture_name}.home.git/info/refs")]
        public IActionResult lecture_data_info_refs(string lecture_name, string user_account, string owner_account, [FromQuery]string service)
        {
            var lectureUser = DB.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.User.Account == user_account && x.Lecture.Name == lecture_name && x.Lecture.Owner.Account == owner_account)
                .FirstOrDefault();
            if (lectureUser == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, user =>
            {
                return info_refs(lectureUser.RepositoryPair, service, user);
            });
        }
        [HttpPost("api/git/user/lecture_data/{user_account}/{owner_account}/{lecture_name}.home.git/git-upload-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult lecture_data_git_upload_pack(string lecture_name, string user_account, string owner_account, [FromQuery]string service)
        {
            var lectureUser = DB.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.User.Account == user_account && x.Lecture.Name == lecture_name && x.Lecture.Owner.Account == owner_account)
                .FirstOrDefault();
            if (lectureUser == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async user =>
            {
                return await git_upload_pack(lectureUser.RepositoryPair, user);
            });
        }
        [HttpPost("api/git/user/lecture_data/{user_account}/{owner_account}/{lecture_name}.home.git/git-upload-pack")]
        [RequestSizeLimit(120_000_000)]
        public IActionResult lecture_data_git_receive_pack(string lecture_name, string user_account, string owner_account, [FromQuery]string service)
        {
            var lectureUser = DB.Context.LectureUsers.Include(x => x.User).Include(x => x.Lecture).ThenInclude(x => x.Owner)
                .Where(x => x.User.Account == user_account && x.Lecture.Name == lecture_name && x.Lecture.Owner.Account == owner_account)
                .FirstOrDefault();
            if (lectureUser == null) return new NotFoundResult();
            return BasicAuthFiltered(user => true, async user =>
            {
                return await git_receive_pack(lectureUser.RepositoryPair, user);
            });
        }








        private IActionResult info_refs(IRepositoryPair pair, string service, User user)
        {
            if (pair is IApiedRepository repos)
            {
                if(service == "git-upload-pack")
                {
                    if(repos.CanPull(user))
                    {
                        return Content(pair.SharedRepository.Pack(PackService.GitUploadPack), "application/x-git-upload-pack-advertisement");
                    }
                    else
                    {
                        return new UnauthorizedResult();
                    }
                }
                if (service == "git-receive-pack")
                {
                    if (repos.CanPush(user))
                    {
                        return Content(pair.SharedRepository.Pack(PackService.GitReceivePack), "application/x-git-receive-pack-advertisement");
                    }
                    else
                    {
                        return new UnauthorizedResult();
                    }
                }
            }
            throw new ArgumentException($"Invalid service was requested: `{service}'");
        }

        private async Task<IActionResult> git_upload_pack(IRepositoryPair pair, User user)
        {
            if (pair is IApiedRepository repos && repos.CanPull(user))
            {
                return File(await pair.SharedRepository.Pack(PackService.GitUploadPack, Request.Body), "application/x-git-upload-pack-result");
            }
            return new UnauthorizedResult();
        }

        private async Task<IActionResult> git_receive_pack(IRepositoryPair pair, User user)
        {
            if (pair is IApiedRepository repos && repos.CanPush(user))
            {
                var result = await pair.SharedRepository.Pack(PackService.GitReceivePack, Request.Body);
                pair.ClonedRepository.Synchronize();
                return File(result, "application/x-git-receive-pack-result");
            }
            return new UnauthorizedResult();

        }
    }


}