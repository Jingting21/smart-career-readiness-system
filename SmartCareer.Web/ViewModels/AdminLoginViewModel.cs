using System.ComponentModel.DataAnnotations;
namespace SmartCareer.Web.ViewModels;
public sealed class AdminLoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
