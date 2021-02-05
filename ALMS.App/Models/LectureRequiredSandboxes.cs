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

    [Serializable, XmlRoot("RequiredSandboxes")]
    public class LectureRequiredSandboxes
    {
        [XmlElement("RequiredSandbox", typeof(LectureRequiredSandbox))]
        public LectureRequiredSandbox[] Children { get; set; } = new LectureRequiredSandbox[0];
    }

    [Serializable]
    public class LectureRequiredSandbox
    {
        [XmlAttribute]
        public string Name { get; set; }

        public string Description { get; set; }

        public string Validation { get; set; }

        public string Installer { get; set; }
    }
}
