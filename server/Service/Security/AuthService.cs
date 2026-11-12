using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using LinqToDB.Async;
using Service.DTOs;
using Service.Exceptions;
using Service.Security;

namespace Service;

public class AuthService (LoginDb db,TokenService tokenService,IPasswordHasher passwordHasher)   
{
    public async Task<UserDTO> Register(RegisterRequestDTO dto)
    {
        var username = (dto.Username ?? "").Trim().ToLowerInvariant();
        var password = dto.Password ?? "";

        // validate
        if (username.Length < 3)
            throw new BadRequestException("Username must be at least 3 characters");
        if (username.Length > 30)
            throw new BadRequestException("Username can be at most 30 characters");
        if (password.Length < 7)
            throw new BadRequestException("Password must be at least 7 characters");
        
        if (await db.Users.AnyAsync(u => u.Username == username))
            throw new BadRequestException("Username is already taken");

        //  Build the entity
        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),             
            Username = username,
            PasswordHash = passwordHasher.HashAndSaltPassword(password),
            Role = RoleToEnum(dto.Role),
            IsActive = true
        };

        // save user
        await db.InsertAsync(user);
        
        return UserToUserDTO(user);
    }
    public async Task<LoginResponseDTO> Login(LoginRequestDTO dto)
    {
        var username = (dto.Username ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        
        if (user is null || !passwordHasher.VerifyHashedPassword(dto.Password ?? "", user.PasswordHash))
            throw new UnauthorizedException("Invalid username or password");
        
        return new LoginResponseDTO(tokenService.CreateToken(user), UserToUserDTO(user));
    }
    public async Task<UserDTO> GetUser(string userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                   ?? throw new NotFoundException("User not found");
        
        return UserToUserDTO(user);
    }
    
    private UserRole RoleToEnum(string role)
    {
        if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new ArgumentException("Unsupported user-role: " + role);
    }

    private UserDTO UserToUserDTO(User user)
    {
        return new UserDTO
        {
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            IsActive = user.IsActive
        };
    }
}
