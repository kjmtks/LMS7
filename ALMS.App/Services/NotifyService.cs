using System;
using System.Threading.Tasks;

namespace ALMS.App.Services
{
    public class NotifierService
    {
        public async Task Update()
        {
            if (Notify != null)
            {
                await Notify.Invoke();
            }
        }

        public event Func<Task> Notify;
    }
}
