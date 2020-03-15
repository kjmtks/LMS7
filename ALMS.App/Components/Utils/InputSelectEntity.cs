using ALMS.App.Models.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ALMS.App.Components.Utils
{
    public class InputSelectEntity<T> : InputSelect<T>
    {
        [Parameter]
        public IEnumerable<T> Collection { get; set; }
        protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage)
        {
            if (int.TryParse(value, out var resultInt))
            {
                result = Collection.Where(x =>
                {
                    var p = x.GetType().GetProperty("Id", typeof(int));
                    return (p != null && p.CanRead) ? (int)p.GetValue(x) == resultInt : false;
                }).FirstOrDefault();
                validationErrorMessage = null;
                return true;
            }
            else
            {
                result = default;
                validationErrorMessage = "The chosen value is not a valid number.";
                return false;
            }
        }

        protected override string FormatValueAsString(T value)
        {
            var p = value?.GetType()?.GetProperty("Id", typeof(int));
            return (p != null && p.CanRead) ? p.GetValue(value).ToString() : "0";
        }
    }
}
