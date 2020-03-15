using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using ALMS.App.Models.Entities;

namespace ALMS.App.Models.Contents
{

    public class ActivityParser
    {
        class Dummy { }
        public static async Task<Activity> BuildActivityAsync(Entities.Lecture lecture, string activity_filename, Entities.User user, IDictionary<string, string> args, string current_page_path, string branch)
        {
            var text = lecture.ReadActivityFile(activity_filename, branch);

            var engine = new RazorLight.RazorLightEngineBuilder()
               .UseEmbeddedResourcesProject(typeof(Dummy))
               .UseMemoryCachingProvider()
               .Build();
            var commitInfo = lecture.LectureContentsRepositoryPair.ReadCommitInfo($"activities/{activity_filename}", branch);
            var model = new PageModel(lecture, user, commitInfo, current_page_path, branch);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            sb.AppendLine("@{ DisableEncoding = true; }");
            sb.Append(text);

            dynamic viewbag = new System.Dynamic.ExpandoObject();
            var x = viewbag as IDictionary<string, Object>;
            foreach (var p in lecture.GetParameters().GetValues())
            {
                x.Add(p.Name, p.GetValue());
            }
            foreach (var p in args)
            {
                x.Add(p.Key, p.Value);
            }

            var xml = await engine.CompileRenderStringAsync($"{lecture.DirectoryPath}/contents/activities/{activity_filename}:{branch}", sb.ToString(), model, viewbag);

            XmlSchemaSet set = new XmlSchemaSet();
            set.Add("urn:activity-schema", $"ActivitySchema.xsd");
            XmlSchema schema = null;
            foreach (XmlSchema s in set.Schemas("urn:activity-schema"))
            {
                schema = s;
            }

            XmlDocument xdoc = new XmlDocument();
            xdoc.Schemas.Add(schema);
            xdoc.LoadXml(xml);
            var sb2 = new System.Text.StringBuilder();
            xdoc.Validate((sender, args) => {
                if (args.Severity == XmlSeverityType.Error)
                {
                    sb2.AppendLine("Error: " + args.Message);
                }
            });
            var validateResult = sb2.ToString().TrimEnd();
            if (validateResult != string.Empty)
            {
                throw new FormatException(validateResult);
            }
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Activity));
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)))
            {
                return (Activity)serializer.Deserialize(ms);
            }
        }
    }

}
