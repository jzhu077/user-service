using UserService.Models;

namespace UserService.Services;

public interface IUserService
{
    Task<string> Register(RegisterRequest request);
    Task<string> Login(LoginRequest request);
    Task<List<UserDTO>> ListUsers();
    Task<UserDTO> GetUser(string userName);
}