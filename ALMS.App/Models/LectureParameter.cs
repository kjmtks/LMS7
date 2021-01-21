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

    [Serializable, XmlRoot("Parameters")]
    public class LectureParameters
    {
        [XmlElement("DateTime", typeof(LectureDateTimeParameter))]
        [XmlElement("String", typeof(LectureStringParameter))]
        [XmlElement("Boolean", typeof(LectureBooleanParameter))]
        [XmlElement("Integer", typeof(LectureIntegerParameter))]
        [XmlElement("Float", typeof(LectureFloatParameter))]
        public object[] Parameters { get; set; } = new object[0];


        public IEnumerable<ILectureParameter> GetValues()
        {
            return Parameters != null ? Parameters.Cast<ILectureParameter>() : new ILectureParameter[] { };
        }
        public T GetValue<T>(string name) where T : ILectureParameter
        {
            return Parameters.Where(p => p is T).Cast<T>().FirstOrDefault(p => p?.Name == name);
        }
        public void SetValue<T>(string name, T value) where T : ILectureParameter
        {
            var p = Parameters.Where(p => p is T).Cast<T>().FirstOrDefault(p => p?.Name == name);

        }
    }

    public interface ILectureParameter
    {
        string Name { get; }
        string Description { get; }
        Type DataType { get; }
        dynamic GetValue();
    }
    [Serializable]
    public class LectureDateTimeParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public DateTime Value { get; set; }

        public dynamic GetValue() { return Value; }
        public Type DataType { get { return Value.GetType(); } }
    }
    [Serializable]
    public class LectureStringParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public string Value { get; set; }

        public dynamic GetValue() { return Value; }
        public Type DataType { get { return Value.GetType(); } }
    }
    [Serializable]
    public class LectureBooleanParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public bool Value { get; set; }

        public dynamic GetValue() { return Value; }
        public Type DataType { get { return Value.GetType(); } }
    }
    [Serializable]
    public class LectureIntegerParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public long Value { get; set; }

        public dynamic GetValue() { return Value; }
        public Type DataType { get { return Value.GetType(); } }
    }
    [Serializable]
    public class LectureFloatParameter : ILectureParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Description { get; set; }
        [XmlText]
        public double Value { get; set; }

        public dynamic GetValue() { return Value; }
        public Type DataType { get { return Value.GetType(); } }
    }




}
