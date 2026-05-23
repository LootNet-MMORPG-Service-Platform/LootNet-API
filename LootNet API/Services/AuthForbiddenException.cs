namespace LootNet_API.Services;

public class AuthForbiddenException : Exception
{
    public AuthForbiddenException(string message) : base(message)
    {
    }
}
