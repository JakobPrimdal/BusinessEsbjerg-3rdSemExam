using Infrastructure.Entities;

namespace Service.Interfaces;

public interface ITokenService
{
    public string CreateToken(User user);
}