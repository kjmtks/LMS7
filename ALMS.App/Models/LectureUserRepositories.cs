using ALMS.App.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Models
{

    public class LectureUserHomeRepositoryPair : RepositoryPairBase<LectureUserHomeRepositoryPair, LectureUserHomeSharedRepository, LectureUserHomeClonedRepository, LectureUser>
    {
        private LectureUserHomeSharedRepository lectureUserHomeSharedRepository;
        private LectureUserHomeClonedRepository lectureUserHomeClonedRepository;
        private LectureUser lectureUser;
        public LectureUserHomeRepositoryPair(LectureUser lectureUser)
        {
            this.lectureUser = lectureUser;
            lectureUserHomeSharedRepository = new LectureUserHomeSharedRepository(lectureUser);
            lectureUserHomeClonedRepository = new LectureUserHomeClonedRepository(lectureUser);
        }

        public override LectureUser Entity => lectureUser;

        public override LectureUserHomeSharedRepository SharedRepository => lectureUserHomeSharedRepository;

        public override LectureUserHomeClonedRepository ClonedRepository => lectureUserHomeClonedRepository;

        public override string ApiUrl => $"{Environment.GetEnvironmentVariable("APP_URL")}/api/git/user/lecture_data/{lectureUser.User.Account}/{lectureUser.Lecture.Owner.Account}/{lectureUser.Lecture.Name}.home.git";

        public override bool CanPull(User user)
        {
            return user.Account == lectureUser.User.Account || user.IsTeacher(lectureUser.Lecture);
        }
        public override bool CanPush(User user)
        {
            return user.Account == lectureUser.User.Account || (user.IsTeacher(lectureUser.Lecture) && (user.IsSenior || user.IsAdmin));
        }
    }

    public class LectureUserHomeSharedRepository : SharedRepositoryBase<LectureUserHomeSharedRepository, LectureUserHomeClonedRepository, LectureUserHomeRepositoryPair, LectureUser>
    {
        private LectureUser lectureUser;
        public LectureUserHomeSharedRepository(LectureUser lectureUser)
        {
            this.lectureUser = lectureUser;
        }
        public override LectureUser Entity => lectureUser;

        public override string DirectoryPath => $"{lectureUser.DirectoryPath}/home.git";
    }
    public class LectureUserHomeClonedRepository : ClonedRepositoryBase<LectureUserHomeClonedRepository, LectureUserHomeSharedRepository, LectureUserHomeRepositoryPair, LectureUser>
    {
        private LectureUser lectureUser;
        public LectureUserHomeClonedRepository(LectureUser lectureUser)
        {
            this.lectureUser = lectureUser;
        }
        public override LectureUser Entity => lectureUser;

        public override string DirectoryPath => $"{lectureUser.DirectoryPath}/home";
    }

}
