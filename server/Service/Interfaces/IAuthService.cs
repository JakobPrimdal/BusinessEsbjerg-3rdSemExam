using Service.DTOs;

namespace Service.Interfaces;

public interface IAuthService
{
    public Task<UserDTO> Register(RegisterRequestDTO dto);
    public Task<LoginResponseDTO> Login(LoginRequestDTO dto);
    public Task<UserDTO> GetUser(string userId);
}