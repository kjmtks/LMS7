using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using ALMS.App.Components.Contents;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Rendering;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using ALMS.App.Services;

namespace ALMS.App.Models.Contents
{

    [Serializable]
    public partial class Activity
    {
        [XmlAttribute]
        public string Version { get; set; }
        public string Sandbox { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public string Directory { get; set; }
        public ActivityFlags Flags { get; set; }
        public ActivityFiles Files { get; set; }
        public string Run { get; set; }
        public string Submit { get; set; }
        public ActivityLimits Limits { get; set; }
        public ActivityValidations Validations { get; set; }


        private IDictionary<string, FileActivityComponentBase> fileComponents;
        private IEnumerable<(IActivityFile, RenderFragment)> childRenderFragments;
        public IDictionary<string, FileActivityComponentBase> GetFileComponents() { return fileComponents; }
        public IEnumerable<(IActivityFile, RenderFragment)> GetChildRenderFragments() { return childRenderFragments; }
        public void CreateFileComponents()
        {
            childRenderFragments = new List<(IActivityFile, RenderFragment)>();
            var frgs = childRenderFragments as List<(IActivityFile, RenderFragment)>;
            fileComponents = new Dictionary<string, FileActivityComponentBase>();
            foreach (var file in Files.Children)
            {
                if (file is ActivityFilesHidden fh)
                {
                    RenderFragment frg = (RenderTreeBuilder builder) =>
                    {
                        builder.OpenComponent(0, typeof(HiddenActivityComponent));
                        builder.AddAttribute(1, "File", fh);
                        builder.AddComponentReferenceCapture(2, e => { fileComponents[file.Name] = e as FileActivityComponentBase; });
                        builder.CloseComponent();
                    };
                    frgs.Add((file, frg));
                }
                if (file is ActivityFilesText ft)
                {
                    RenderFragment frg = (RenderTreeBuilder builder) =>
                    {
                        builder.OpenComponent(0, typeof(TextActivityComponent));
                        builder.AddAttribute(1, "File", ft);
                        builder.AddAttribute(1, "IsReadOnly", ft.ReadOnly);
                        builder.AddComponentReferenceCapture(2, e => { fileComponents[file.Name] = e as FileActivityComponentBase; });
                        builder.CloseComponent();
                    };
                    frgs.Add((file, frg));
                }
                else if (file is ActivityFilesForm ff)
                {
                    RenderFragment frg = (RenderTreeBuilder builder) =>
                    {
                        builder.OpenComponent(0, typeof(FormActivityComponent));
                        builder.AddAttribute(1, "File", ff);
                        builder.AddComponentReferenceCapture(2, e => { fileComponents[file.Name] = e as FileActivityComponentBase; });
                        builder.CloseComponent();
                    };
                    frgs.Add((file, frg));
                }
                else if (file is ActivityFilesCode fc)
                {
                    RenderFragment frg = (RenderTreeBuilder builder) =>
                    {
                        builder.OpenComponent(0, typeof(CodeActivityComponent));
                        builder.AddAttribute(1, "File", fc);
                        builder.AddAttribute(1, "IsReadOnly", fc.ReadOnly);
                        builder.AddComponentReferenceCapture(2, e => { fileComponents[file.Name] = e as FileActivityComponentBase; });
                        builder.CloseComponent();
                    };
                    frgs.Add((file, frg));
                }
                else if (file is ActivityFilesString fs)
                {
                    RenderFragment frg = (RenderTreeBuilder builder) =>
                    {
                        builder.OpenComponent(0, typeof(StringActivityComponent));
                        builder.AddAttribute(1, "File", fs);
                        builder.AddAttribute(1, "IsReadOnly", fs.ReadOnly);
                        builder.AddComponentReferenceCapture(2, e => { fileComponents[file.Name] = e as FileActivityComponentBase; });
                        builder.CloseComponent();
                    };
                    frgs.Add((file, frg));
                }
                else if (file is ActivityFilesUpload fu)
                {
                    RenderFragment frg = (RenderTreeBuilder builder) =>
                    {
                        builder.OpenComponent(0, typeof(UploadActivityComponent));
                        builder.AddAttribute(1, "File", fu);
                        builder.AddComponentReferenceCapture(2, e => { fileComponents[file.Name] = e as FileActivityComponentBase; });
                        builder.CloseComponent();
                    };
                    frgs.Add((file, frg));
                }
            }
        }

        public bool UseMarkdown()
        {
            return this.Flags.UseMarkdown;
        }
        public bool UseSave()
        {
            return this.Flags.UseSave;
        }
        public bool UseReset()
        {
            return this.Flags.UseReset;
        }
        public bool UseRun()
        {
            return this.Run != null;
        }
        public bool UseValidate()
        {
            return this.Validations != null && this.Validations.Child != null;
        }
        public bool UseSubmit()
        {
            return this.Files.Children.Any(f => f.Submit);
        }
        public bool UseAnswer()
        {
            return this.Files.Children.Any(f => f.HasAnswer());
        }
    }

    [Serializable]
    public partial class ActivityFlags
    {
        public bool UseMarkdown { get; set; } = false;
        public bool UseStdout { get; set; } = false;
        public bool UseStderr { get; set; } = false;
        public bool UseSave { get; set; } = false;
        public bool UseReset { get; set; } = false;
        public bool CanSubmitAfterDeadline { get; set; } = false;
        public bool CanSubmitBeforeAccept { get; set; } = false;
    }

    [Serializable]
    public partial class ActivityLimits
    {
        public uint CpuTime { get; set; }
        public string Memory { get; set; }
        public uint StdoutLength { get; set; }
        public uint StderrLength { get; set; }
        public uint Pids { get; set; }
    }


    public interface IActivityFile
    {
        string Name { get; }
        string Label { get; }
        bool Submit { get; }
        bool HasAnswer();
    }

    [Serializable]
    public partial class ActivityFiles
    {
        [XmlElement("Hidden", typeof(ActivityFilesHidden))]
        [XmlElement("Code", typeof(ActivityFilesCode))]
        [XmlElement("Text", typeof(ActivityFilesText))]
        [XmlElement("String", typeof(ActivityFilesString))]
        [XmlElement("Upload", typeof(ActivityFilesUpload))]
        [XmlElement("Form", typeof(ActivityFilesForm))]
        public object[] Files { get; set; }
        public IEnumerable<IActivityFile> Children { get { return this.Files.Cast<IActivityFile>(); } }
    }

    [Serializable]
    public partial class ActivityFilesHidden : IActivityFile
    {
        [XmlAttribute]
        public string Name { get; set; }
        public string Label { get { return ""; } }
        [XmlAttribute]
        public bool Submit { get; set; }

        [XmlText]
        public string Text { get; set; }
        public bool HasAnswer()
        {
            return false;
        }
    }

    [Serializable]
    public partial class ActivityFilesCode : IActivityFile
    {
        public string Default { get; set; }

        public string Answer { get; set; }

        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string Language { get; set; }
        [XmlAttribute]
        public string ContentType { get; set; }
        [XmlAttribute]
        public ushort Maxlength { get; set; }
        [XmlAttribute]
        public bool ReadOnly { get; set; }
        [XmlAttribute]
        public bool Submit { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesText : IActivityFile
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string ContentType { get; set; }
        [XmlAttribute]
        public ushort Maxlength { get; set; }
        [XmlAttribute]
        public bool ReadOnly { get; set; }
        [XmlAttribute]
        public bool Submit { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesString : IActivityFile
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        public bool HasAnswer() { return !string.IsNullOrWhiteSpace(Answer); }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string ContentType { get; set; }
        [XmlAttribute]
        public byte Maxlength { get; set; }
        [XmlAttribute]
        public bool ReadOnly { get; set; }
        [XmlAttribute]
        public bool Submit { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesUpload : IActivityFile
    {
        public bool HasAnswer() { return false; }

        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public string AllowedContentTypes { get; set; }
        [XmlAttribute]
        public uint Maxsize { get; set; }
        [XmlAttribute]
        public bool Submit { get; set; }
    }




    [Serializable]
    public partial class ActivityFilesForm : IActivityFile
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public bool Submit { get; set; }

        [XmlElement("Text", typeof(ActivityFilesFormText))]
        [XmlElement("Checkbox", typeof(ActivityFilesFormCheckbox))]
        [XmlElement("Radio", typeof(ActivityFilesFormRadio))]
        [XmlElement("Select", typeof(ActivityFilesFormSelect))]
        [XmlElement("String", typeof(ActivityFilesFormString))]
        [XmlElement("Textarea", typeof(ActivityFilesFormTextarea))]
        public object[] Forms { get; set; }
        public IEnumerable<IActivityFilesFormInput> Children { get { return this.Forms.Cast<IActivityFilesFormInput>(); } }

        public bool HasAnswer() { return Children.Any(x => !string.IsNullOrWhiteSpace(x.GetAnswer())); }

    }

    public interface IActivityFilesFormInput
    {
        string Name { get; }
        string GetAnswer();
        string GetDefault();
        bool Block { get; }
    }



    [Serializable]
    public partial class ActivityFilesFormText : IActivityFilesFormInput
    {
        [XmlText]
        public string Text { get; set; }
        [XmlAttribute]

        public string Name { get { return null; } }

        [XmlAttribute]
        public bool Block { get; set; } = false;
        public string GetAnswer()
        {
            return null;
        }

        public string GetDefault()
        {
            return null;
        }
    }
    [Serializable]
    public partial class ActivityFilesFormCheckbox : IActivityFilesFormInput
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Block { get; set; } = false;

        [XmlAttribute]
        public string True { get; set; }
        [XmlAttribute]
        public string False { get; set; }
        [XmlAttribute]
        public bool Default { get; set; }
        [XmlAttribute]
        public bool Answer { get; set; }
        [XmlText]
        public string Label { get; set; }

        public string GetAnswer()
        {
            return this.Answer ? this.True : this.False;
        }

        public string GetDefault()
        {
            return this.Default ? this.True : this.False;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormRadio : IActivityFilesFormInput
    {
        [XmlElement("Option")]
        public ActivityFilesFormRadioOption[] Options { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Block { get; set; } = false;
        [XmlAttribute]
        public string Label { get; set; }
        public string GetAnswer()
        {
            return this.Options.FirstOrDefault(opt => opt.Answer)?.Value;
        }

        public string GetDefault()
        {
            return this.Options.FirstOrDefault(opt => opt.Default)?.Value;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormRadioOption
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }
        [XmlAttribute]
        public bool Default { get; set; } = false;
        [XmlAttribute]
        public bool Answer { get; set; } = false;
        [XmlText]
        public string Label { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesFormSelect : IActivityFilesFormInput
    {
        [XmlElement("Option")]
        public ActivityFilesFormSelectOption[] Options { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Block { get; set; } = false;
        [XmlAttribute]
        public string Label { get; set; }

        public string GetAnswer()
        {
            return this.Options.FirstOrDefault(opt => opt.Answer)?.Value;
        }

        public string GetDefault()
        {
            return this.Options.FirstOrDefault(opt => opt.Default)?.Value;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormSelectOption
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }
        [XmlAttribute]
        public bool Default { get; set; } = false;
        [XmlAttribute]
        public bool Answer { get; set; } = false;
        [XmlText]
        public string Label { get; set; }
    }

    [Serializable]
    public partial class ActivityFilesFormString : IActivityFilesFormInput
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Block { get; set; } = false;
        [XmlAttribute]
        public string Label { get; set; }
        [XmlAttribute]
        public uint Maxlength { get; set; } = 1000;

        [XmlAttribute]
        public string Size { get; set; } = "small";
        public string GetAnswer()
        {
            return this.Answer;
        }
        public string GetDefault()
        {
            return this.Default;
        }
    }

    [Serializable]
    public partial class ActivityFilesFormTextarea : IActivityFilesFormInput
    {
        public string Default { get; set; }
        public string Answer { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Label { get; set; }

        [XmlAttribute]
        public bool Block { get; set; } = false;
        [XmlAttribute]
        public uint Maxlength { get; set; } = 100000;
        [XmlAttribute]
        public uint Rows { get; set; } = 4;

        public string GetAnswer()
        {
            return this.Answer;
        }
        public string GetDefault()
        {
            return this.Default;
        }
    }





    [Serializable]
    public partial class ActivityValidations
    {
        [XmlElement("Conjunction", typeof(Conjunction)), XmlElement("Disjunction", typeof(Disjunction)), XmlElement("Negation", typeof(Negation)), XmlElement("Validation", typeof(Validation))]
        public object Child { get; set; }
    }

    [Serializable]
    public partial class Validation : IValidatable
    {
        public string Run { get; set; }
        public string Answer { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
        public Task<bool> ValidateAsync(Validator validator)
        {
            return validator(this);
        }
    }

    [Serializable]
    public partial class Negation : IValidatable
    {
        [XmlElement("Conjunction", typeof(Conjunction)), XmlElement("Disjunction", typeof(Disjunction)), XmlElement("Negation", typeof(Negation)), XmlElement("Validation", typeof(Validation))]
        public object Child { get; set; }

        public async Task<bool> ValidateAsync(Validator validator)
        {
            var c = this.Child as IValidatable;
            if (c != null)
            {
                return !(await c.ValidateAsync(validator));
            }
            else
            {
                // throw new FormatException("Negation tag should have a child.");
                return false;
            }
        }
    }

    [Serializable]
    public partial class Conjunction : IValidatable
    {
        [XmlElement("Validation")]
        public Validation[] Validations { get; set; }

        [XmlElement("Negation")]
        public Negation[] NegativeChildren { get; set; }
        [XmlElement("Conjunction")]
        public Conjunction[] ConjunctiveChildren { get; set; }
        [XmlElement("Disjunction")]
        public Disjunction[] DisjunctiveChildren { get; set; }
        public async Task<bool> ValidateAsync(Validator validator)
        {
            if(Validations != null)
            {
                foreach (var c in Validations)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            if (NegativeChildren != null)
            {
                foreach (var c in NegativeChildren)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            if (ConjunctiveChildren != null)
            {
                foreach (var c in ConjunctiveChildren)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            if (DisjunctiveChildren != null)
            {
                foreach (var c in DisjunctiveChildren)
                {
                    if (!(await c.ValidateAsync(validator))) { return false; }
                }
            }
            return true;
        }
    }

    [Serializable]
    public partial class Disjunction : IValidatable
    {
        [XmlElement("Validation")]
        public Validation[] Validations { get; set; }

        [XmlElement("Negation")]
        public Negation[] NegativeChildren { get; set; }
        [XmlElement("Conjunction")]
        public Conjunction[] ConjunctiveChildren { get; set; }
        [XmlElement("Disjunction")]
        public Disjunction[] DisjunctiveChildren { get; set; }
        public async Task<bool> ValidateAsync(Validator validator)
        {
            if (Validations != null)
            {
                foreach (var c in Validations)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            if (NegativeChildren != null)
            {
                foreach (var c in NegativeChildren)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            if (ConjunctiveChildren != null)
            {
                foreach (var c in ConjunctiveChildren)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            if (DisjunctiveChildren != null)
            {
                foreach (var c in DisjunctiveChildren)
                {
                    if (await c.ValidateAsync(validator)) { return true; }
                }
            }
            return false;
        }
    }
}
