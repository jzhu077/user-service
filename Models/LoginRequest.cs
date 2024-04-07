
using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class LoginRequest
{
    [Required]
    public string UserName { get; set; } = String.Empty;

    [Required]
    public string Password { get; set; } = String.Empty;
}