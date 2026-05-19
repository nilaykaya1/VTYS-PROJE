namespace VTYS_PROJE.Services;

public class PasswordService
{
    public string HashPassword(string password)
    {
        return password;
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        return password == storedHash;
    }

    public bool NeedsRehash(string storedHash)
    {
        return false;
    }
}