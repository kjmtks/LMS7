using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Models.Entities
{
    public class ActivityComment
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool AlreadyRead { get; set; }
        public DateTime SendAt { get; set; }
        public string ActivityName { get; set; }

        [ForeignKey("Lecture")]
        public int LectureId { get; set; }
        public virtual Lecture Lecture { get; set; }


        [ForeignKey("Sender")]
        public int SenderId { get; set; }
        public virtual User Sender { get; set; }


        [ForeignKey("Receiver")]
        public int ReceiverId { get; set; }
        public virtual User Receiver { get; set; }
    }
}
