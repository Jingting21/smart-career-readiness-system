using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SmartCareer.Web.ViewModels;

public sealed class StudentProfileViewModel : StudentDetailsInputModel
{
    [BindNever]
    public string Email { get; set; } = string.Empty;

    [BindNever]
    public bool IsEditing { get; set; }
}
