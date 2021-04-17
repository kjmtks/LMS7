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

        public event Func<Task> Notify;

        public OnlineStatus GetStatus(string account)
        {
            if (status.ContainsKey(account))
            {
                return status[account];
            }
            return null;
        }

        public async Task VisitContentPageAsync(Lecture lecture, string page_name, User user)
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
            if (Notify != null)
            {
                await Notify.Invoke();
            }
        }

        public async Task LeaveContentPageAsync(User user)
        {
            if (status.ContainsKey(user.Account))
            {
                status.Remove(user.Account);
                if (Notify != null)
                {
                    await Notify.Invoke();
                }
            }
        }
    }

    public class OnlineStatus
    {
        public Lecture Lecture { get; set; }
        public string PageName { get; set; }
    }
}
