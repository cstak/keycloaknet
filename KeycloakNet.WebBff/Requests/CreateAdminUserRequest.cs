namespace KeycloakNet.WebBff.Requests;

public class CreateAdminUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

