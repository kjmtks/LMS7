using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Models.Entities
{
    public enum ActivityActionType
    {
        Save = 0,
        SaveAndRun = 1,
        SaveAndSubmit = 2,
        SaveAndValidateAccept = 3,
        SaveAndValidateReject = 4,
        SaveAndForceSubmit = 5
    }
    public class ActivityActionHistory
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }
        public ActivityActionType ActionType { get; set; }
        public string ActivityName { get; set; }
        public string Directory { get; set; }
        public DateTime DateTime { get; set; }
    }
}
