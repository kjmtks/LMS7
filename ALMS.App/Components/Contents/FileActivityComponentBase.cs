using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace ALMS.App.Components.Contents
{
    public abstract class FileActivityComponentBase : ComponentBase
    {
        [Parameter]
        public ALMS.App.Models.Contents.IActivityFile File { get; set; }
        public abstract Task<string> GetValueAsync();
        public abstract Task SetValueAsync(string value);
        public abstract Task SetDefaultValueAsync();
        public abstract Task SetAnswerValueAsync();
    }
}
