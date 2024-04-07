using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class RegisterRequest
{
    [Required]
    public string UserName { get; set; } = String.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = String.Empty;

    [Required]
    public string Password { get; set; } = String.Empty;
}