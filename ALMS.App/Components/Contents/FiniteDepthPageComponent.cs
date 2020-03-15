using ALMS.App.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace ALMS.App.Components.Contents
{
    
    // temporary...
    // wainting for https://github.com/dotnet/aspnetcore/issues/18539
    public class FiniteDepthPageComponent : ComponentBase
    {
        [Parameter]
        public string OwnerAccount { get; set; }
        [Parameter]
        public string LectureName { get; set; }
        [Parameter]
        public string Branch { get; set; }
        public string Path { get; set; }



        [Inject]
        protected DatabaseService DB { get; set; }
        [Inject]
        protected NavigationManager NM { get; set; }

        protected ALMS.App.Models.Entities.Lecture Lecture;
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Lecture = DB.Context.Lectures.Include(x => x.LectureUsers).ThenInclude(x => x.User).Include(x => x.Owner).Where(x => x.Owner.Account == OwnerAccount && x.Name == LectureName).FirstOrDefault();
            Path = "";
        }
    }
}
