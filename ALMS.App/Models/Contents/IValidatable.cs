using System.Threading.Tasks;
namespace ALMS.App.Models.Contents
{

    public delegate Task<bool> Validator(Validation validation);
    public interface IValidatable
    {
        Task<bool> ValidateAsync(Validator validator);
    }
}
