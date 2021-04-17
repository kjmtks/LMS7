using ALMS.App.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Services
{
    public class OnlineStatusService
    {
        private Dictionary<string, OnlineStatus> status = new Dictionary<string, OnlineStatus>();

        public OnlineStatus GetStatus(string account)
        {
            if (status.ContainsKey(account))
            {
                return status[account];
            }
            return null;
        }

        public void VisitContentPage(Lecture lecture, string page_name, User user)
        {
            var st = new OnlineStatus { Lecture = lecture, PageName = page_name };
            if (status.ContainsKey(user.Account))
            {
                status[user.Account] = st;
            }
            else
            {
                status.Add(user.Account, st);
            }
        }

        public void LeaveContentPage(User user)
        {
            if (status.ContainsKey(user.Account))
            {
                status.Remove(user.Account);
            }
        }
    }

    public class OnlineStatus
    {
        public Lecture Lecture { get; set; }
        public string PageName { get; set; }
    }
}
