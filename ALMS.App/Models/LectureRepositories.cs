using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ALMS.App.Models.Entities;

namespace ALMS.App.Models
{

    public class LectureContentsRepositoryPair : RepositoryPairBase<LectureContentsRepositoryPair, LectureContentsSharedRepository, LectureContentsClonedRepository, Lecture>
    {
        private LectureContentsSharedRepository lectureContentsSharedRepository;
        private LectureContentsClonedRepository lectureContentsClonedRepository;
        private Lecture lecture;
        public LectureContentsRepositoryPair(Lecture lecture)
        {
            this.lecture = lecture;
            lectureContentsSharedRepository = new LectureContentsSharedRepository(lecture);
            lectureContentsClonedRepository = new LectureContentsClonedRepository(lecture);
        }

        public override Lecture Entity => lecture;

        public override LectureContentsSharedRepository SharedRepository => lectureContentsSharedRepository;

        public override LectureContentsClonedRepository ClonedRepository => lectureContentsClonedRepository;

        public override string ApiUrl => $"http://localhost:8080/api/git/lecture/{lecture.Owner.Account}/{lecture.Name}.contents.git";

        public override bool CanPull(User user)
        {
            return user.IsTeacher(lecture) && (user.IsSenior || user.IsAdmin);
        }

        public override bool CanPush(User user)
        {
            return user.IsTeacher(lecture) && (user.IsSenior || user.IsAdmin);
        }
    }

    public class LectureContentsSharedRepository : SharedRepositoryBase<LectureContentsSharedRepository, LectureContentsClonedRepository, LectureContentsRepositoryPair, Lecture>
    {
        private Lecture lecture;
        public LectureContentsSharedRepository(Lecture lecture)
        {
            this.lecture = lecture;
        }
        public override Lecture Entity => lecture;

        public override string DirectoryPath => $"{lecture.DirectoryPath}/contents.git";
    }
    public class LectureContentsClonedRepository : ClonedRepositoryBase<LectureContentsClonedRepository, LectureContentsSharedRepository, LectureContentsRepositoryPair, Lecture>
    {
        private Lecture lecture;
        public LectureContentsClonedRepository(Lecture lecture)
        {
            this.lecture = lecture;
        }
        public override Lecture Entity => lecture;

        public override string DirectoryPath => $"{lecture.DirectoryPath}/contents";
    }




    public class LectureSubmissionsRepositoryPair : RepositoryPairBase<LectureSubmissionsRepositoryPair, LectureSubmissionsSharedRepository, LectureSubmissionsClonedRepository, Lecture>
    {
        private LectureSubmissionsSharedRepository lectureSubmissionsSharedRepository;
        private LectureSubmissionsClonedRepository lectureSubmissionsClonedRepository;
        private Lecture lecture;
        public LectureSubmissionsRepositoryPair(Lecture lecture)
        {
            this.lecture = lecture;
            lectureSubmissionsSharedRepository = new LectureSubmissionsSharedRepository(lecture);
            lectureSubmissionsClonedRepository = new LectureSubmissionsClonedRepository(lecture);
        }

        public override Lecture Entity => lecture;

        public override LectureSubmissionsSharedRepository SharedRepository => lectureSubmissionsSharedRepository;

        public override LectureSubmissionsClonedRepository ClonedRepository => lectureSubmissionsClonedRepository;

        public override string ApiUrl => $"http://localhost:8080/api/git/lecture/{lecture.Owner.Account}/{lecture.Name}.submissions.git";

        public override bool CanPull(User user)
        {
            return user.IsTeacher(lecture);
        }
        public override bool CanPush(User user)
        {
            return user.IsTeacher(lecture) && (user.IsSenior || user.IsAdmin);
        }
    }

    public class LectureSubmissionsSharedRepository : SharedRepositoryBase<LectureSubmissionsSharedRepository, LectureSubmissionsClonedRepository, LectureSubmissionsRepositoryPair, Lecture>
    {
        private Lecture lecture;
        public LectureSubmissionsSharedRepository(Lecture lecture)
        {
            this.lecture = lecture;
        }
        public override Lecture Entity => lecture;

        public override string DirectoryPath => $"{lecture.DirectoryPath}/submissions.git";
    }
    public class LectureSubmissionsClonedRepository : ClonedRepositoryBase<LectureSubmissionsClonedRepository, LectureSubmissionsSharedRepository, LectureSubmissionsRepositoryPair, Lecture>
    {
        private Lecture lecture;
        public LectureSubmissionsClonedRepository(Lecture lecture)
        {
            this.lecture = lecture;
        }
        public override Lecture Entity => lecture;

        public override string DirectoryPath => $"{lecture.DirectoryPath}/submissions";
    }


}
