namespace Service.Interfaces;

public interface IPasswordHasher
{
    public string HashPassword(string password, string salt);
    public string HashAndSaltPassword(string password);
    public bool VerifyHashedPassword(string password, string hashedPassword);
}