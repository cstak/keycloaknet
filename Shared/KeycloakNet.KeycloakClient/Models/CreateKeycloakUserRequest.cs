using System.Text.Json.Serialization;

namespace KeycloakNet.KeycloakClient.Models;

public class CreateKeycloakUserRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; } = true;

    [JsonPropertyName("credentials")]
    public List<KeycloakCredentialDto> Credentials { get; set; } = [];

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; set; }
}

