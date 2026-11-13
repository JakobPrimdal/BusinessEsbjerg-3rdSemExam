namespace Service.DTOs;

public record RegisterRequestDTO(string Username, string Password, string Role);

public record LoginRequestDTO(string Username, string Password);

public record LoginResponseDTO(string Token, UserDTO User);