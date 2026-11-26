using Infrastructure.Entities;
using Infrastructure.Entities.Enums;
using Infrastructure.Interfaces;
using Service.DTOs;
using Service.Exceptions;
using Service.Interfaces;

namespace Service;

public class AuthService (ILoginDb db,ITokenService tokenService,IPasswordHasher passwordHasher) : IAuthService
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
        
        if (await db.UsernameExistsAsync(dto.Username!.Trim()))
            throw new BadRequestException("Username is already taken");

        UserRole role = RoleToEnum(dto.Role);
        if (role == UserRole.Accountant || role == UserRole.Admin)
        { 
            bool roleAlreadyExists = await db.RoleExistsAsync(role);
            if (roleAlreadyExists)
                throw new BadRequestException("It is not possible to create more users with the selected role");
        }
        
        //  Build the entity
        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),             
            Username = dto.Username.Trim(),
            PasswordHash = passwordHasher.HashAndSaltPassword(password),
            Role = RoleToEnum(dto.Role),
            IsActive = true
        };

        // save user
        await db.AddUserAsync(user);
        
        return UserToUserDTO(user);
    }
    public async Task<LoginResponseDTO> Login(LoginRequestDTO dto)
    {
        var user = await db.GetUserByUsernameAsync(dto.Username.Trim());
        
        if (user is null || !user.IsActive || !passwordHasher.VerifyHashedPassword(dto.Password ?? "", user.PasswordHash))
            throw new UnauthorizedException("Invalid username or password");
        
        return new LoginResponseDTO(tokenService.CreateToken(user), UserToUserDTO(user));
    }
    public async Task<UserDTO> GetUser(string userId)
    {
        var user = await db.GetUserByIdAsync(userId)
                   ?? throw new NotFoundException("User not found");
        
        return UserToUserDTO(user);
    }
    
    private UserRole RoleToEnum(string role)
    {
        if (Enum.TryParse<UserRole>(role, ignoreCase: true, out var result) && Enum.IsDefined(result))
        {
            return result;
        }

        throw new BadRequestException("Unsupported user-role: " + role);
    }

    private UserDTO UserToUserDTO(User user)
    {
        return new UserDTO
        {
            UserId = user.UserId,
            Username = user.Username,
            Role =
            {
                Id = user.Role,
                Name = user.Role.ToString()
            },
            IsActive = user.IsActive
        };
    }
}
