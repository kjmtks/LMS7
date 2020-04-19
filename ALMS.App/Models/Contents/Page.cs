using ALMS.App.Components.Contents;
using ALMS.App.Models.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;

namespace ALMS.App.Models.Contents
{

    public class Page
    {
        public ALMS.App.Models.Entities.Lecture Lecture { get; set; }
        public string Path { get; set; }
        public string Branch { get; set; } = "master";

        public Page(ALMS.App.Models.Entities.Lecture lecture, string path, string branch)
        {
            Lecture = lecture;
            Path = path;
            Branch = branch;
            if (string.IsNullOrWhiteSpace(Path))
            {
                // TODO
                Path = "@index.md";
            }
            else
            {
                Path = Regex.Replace(Path, "/$", "");
            }
        }

        public async Task<RenderFragment> RenderAsync(NavigationManager NM, Entities.User user)
        {
            try
            {
                var text = Lecture.ReadPageFile(Path, Branch);
                var source = await preprocessAsync(text, user);
                var file = new System.IO.FileInfo(Path);

                if (file.Extension == ".md")
                {
                    source = Markdig.Markdown.ToHtml(source
                        .Replace("\\[", "\\\\[").Replace("\\]", "\\\\]")
                        .Replace("\\(", "\\\\(").Replace("\\)", "\\\\)")
                        .Replace("\\{", "\\\\{").Replace("\\}", "\\\\}"));
                }

                if (file.Name[0] == '@')
                {
                    return (builder) =>
                    {
                        var options = new AngleSharp.Html.Parser.HtmlParserOptions();
                        var parser = new AngleSharp.Html.Parser.HtmlParser(options);
                        var doc = parser.ParseDocument(source);
                        var content = compile(NM, doc.Body, user);
                        builder.AddContent(0, content);
                    };
                }

                return (builder) => builder.AddMarkupContent(0, source);
            }
            catch(FileNotFoundException e)
            {
                return (builder) => builder.AddMarkupContent(0, $"Not found page `{Path}': {e.Message}");
            }
        }



        class Dummy { }
        private async Task<string> preprocessAsync(string source, ALMS.App.Models.Entities.User user)
        {
            var engine = new RazorLight.RazorLightEngineBuilder()
               .UseEmbeddedResourcesProject(typeof(Dummy))
               .UseMemoryCachingProvider()
               .Build();
            var commitInfo = Lecture.LectureContentsRepositoryPair.ReadCommitInfo($"pages/{Path}", Branch);
            var model = new PageModel(Lecture, user, commitInfo, Path, Branch);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"@model {model.GetType().FullName}");
            sb.AppendLine("@{ DisableEncoding = true; }");
            sb.AppendLine("<div>");
            sb.AppendLine("");
            sb.Append(source);
            sb.AppendLine("");
            sb.AppendLine("</div>");

            dynamic viewbag = new System.Dynamic.ExpandoObject();
            var x = viewbag as IDictionary<string, Object>;
            foreach (var p in Lecture.GetParameters().GetValues())
            {
                x.Add(p.Name, p.GetValue());
            }
            return await engine.CompileRenderStringAsync($"{Lecture.DirectoryPath}/contents/pages/{Path}:{Branch}", sb.ToString(), model, viewbag);
        }

        private RenderFragment compile(NavigationManager NM, AngleSharp.Dom.IElement element, Entities.User user, int depth=0)
        {
            return (builder) => {
                var seq = 0; // <-- bad performance...

                foreach (var e in element.ChildNodes)
                {
                    if (e is AngleSharp.Dom.IElement el)
                    {
                        if (el.TagName == "SCRIPT" && el.GetAttribute("language") == "activity")
                        {

                            builder.OpenComponent(seq++, typeof(ActivityComponent));

                            try
                            {
                                var parameters = parseParameters(el.InnerHtml);
                                var activity = ActivityParser.BuildActivityAsync(Lecture, el.GetAttribute("ref"), user, parameters, Path, Branch).Result;
                                activity.CreateFileComponents();
                                builder.AddAttribute(seq++, "Activity", activity);
                                builder.AddAttribute(seq++, "Lecture", Lecture);
                                builder.AddAttribute(seq++, "User", user);
                            }
                            catch (Exception ex)
                            {
                                builder.AddAttribute(seq++, "ErrorSubject", ex.Message);
                                builder.AddAttribute(seq++, "ErrorMessage", ex.StackTrace);
                            }
                            builder.CloseComponent();

                        }
                        else
                        {
                            builder.OpenElement(seq++, el.TagName);
                            if (el.TagName == "A")
                            {
                                foreach (var attr in el.Attributes.Where(x => x.Name != "href"))
                                {
                                    builder.AddAttribute(seq++, attr.Name, attr.Value);
                                }
                                if(el.HasAttribute("href"))
                                {
                                    var href = el.GetAttribute("href");
                                    if(!Regex.IsMatch(href, "^[a-zA-Z0-9]+://"))
                                    {
                                        href = $"lecture/{Lecture.Owner.Account}/{Lecture.Name}/page/{Branch}/{localPath(href)}";
                                    }
                                    var ec = EventCallback.Factory.Create(el, () => {
                                        NM.NavigateTo(href, true);
                                    });
                                    builder.AddAttribute(seq++, "href", "javascript: void(0);");
                                    builder.AddAttribute(seq++, "onclick", ec);
                                }
                            }
                            else if (el.TagName == "IMG")
                            {
                                foreach (var attr in el.Attributes.Where(x => x.Name != "src"))
                                {
                                    builder.AddAttribute(seq++, attr.Name, attr.Value);
                                }
                                if (el.HasAttribute("src"))
                                {
                                    var src = el.GetAttribute("src");
                                    if (!Regex.IsMatch(src, "^[a-zA-Z0-9]+://"))
                                    {
                                        src = $"lecture/{Lecture.Owner.Account}/{Lecture.Name}/contents/{Branch}/pages/{localPath(src)}";
                                    }
                                    builder.AddAttribute(seq++, "src", src);
                                }
                            }
                            else
                            {
                                foreach (var attr in el.Attributes)
                                {
                                    builder.AddAttribute(seq++, attr.Name, attr.Value);
                                }
                            }
                            var c = compile(NM, el, user, depth + 1);
                            builder.AddContent(seq++, c);
                            builder.CloseElement();
                        }

                    }
                    else if (e is AngleSharp.Dom.IComment)
                    {
                        // Comment Element
                    }
                    else
                    {
                        builder.AddMarkupContent(seq++, escapeForXml(e.TextContent));
                    }
                }
            };
        }
        private IDictionary<string, string> parseParameters(string code)
        {
            var result = new Dictionary<string, string>();
            var reg = new Regex(@"<(?<tagname>[a-zA-Z_][a-zA-Z0-9_]+)>(?<value>.*)?</\k<tagname>>", RegexOptions.Singleline);
            foreach (Match m in reg.Matches(code))
            {
                result.Add(m.Groups["tagname"].Value, escapeForXml(m.Groups["value"].Value));
            }
            return result;
        }
        private string escapeForXml(string source)
        {
            return source.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private string localPath(string page_path)
        {
            string path;
            if (page_path[0] == '/')
            {
                path = page_path.Substring(1);
            }
            else
            {
                var xs = Path.Split("/").Where(x => !string.IsNullOrWhiteSpace(x));
                if (xs.Count() <= 1)
                {
                    path = page_path;
                }
                else
                {
                    path = string.Join("/", xs.Take(xs.Count() - 1)) + "/" + page_path;
                }
            }

            return path;
        }
    }
}
