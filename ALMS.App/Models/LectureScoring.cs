using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Xml.Serialization;

namespace ALMS.App.Models
{

    [Serializable, XmlRoot("Scorings")]
    public class LectureScorings
    {
        [XmlElement("Scoring", typeof(LectureScoring))]
        public LectureScoring[] Children { get; set; } = new LectureScoring[0];
    }

    [Serializable]
    public class LectureScoring
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
    }

}
