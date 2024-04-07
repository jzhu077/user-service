using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;

namespace UserService.Services;

public class UsersService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;

    public UsersService(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<string> Register(RegisterRequest request)
    {
        var userByEmail = await _userManager.FindByEmailAsync(request.Email);
        var userByUserName = await _userManager.FindByNameAsync(request.UserName);
        if (userByEmail is not null || userByUserName is not null)
        {
            throw new ArgumentException($"A user with email {request.Email} or username {request.UserName} already exists.");
        }

        User user = new()
        {
            Email = request.Email,
            UserName = request.UserName,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new ArgumentException($"Unable to register user {request.UserName} errors: {GetErrorsText(result.Errors)}");
        }

        return await Login(new LoginRequest { UserName = request.Email, Password = request.Password });
    }

    public async Task<string> Login(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user is null)
        {
            user = await _userManager.FindByEmailAsync(request.UserName);
        }

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new ArgumentException($"Unable to authenticate user {request.UserName}");
        }

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = GetToken(authClaims);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<List<UserDTO>> ListUsers()
    {
        var Users = await _userManager.Users.ToListAsync();
        return Users.Select(user => new UserDTO
        {
            UserName = String.IsNullOrWhiteSpace(user.UserName) ? "" : user.UserName,
            Email = String.IsNullOrWhiteSpace(user.Email) ? "" : user.Email
        }).ToList();
    }

    public async Task<UserDTO> GetUser(string userName)
    {
        User? user = await _userManager.Users.Where(user => user.UserName == userName).FirstOrDefaultAsync();

        if (user is null)
        {
            throw new ArgumentException($"A user with username '{userName}' does not exist.");
        }

        return new UserDTO
        {
            UserName = String.IsNullOrWhiteSpace(user?.UserName) ? "" : user.UserName,
            Email = String.IsNullOrWhiteSpace(user?.Email) ? "" : user.Email
        };
    }

    private JwtSecurityToken GetToken(IEnumerable<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

        return token;
    }

    private string GetErrorsText(IEnumerable<IdentityError> errors)
    {
        return string.Join(", ", errors.Select(error => error.Description).ToArray());
    }
}