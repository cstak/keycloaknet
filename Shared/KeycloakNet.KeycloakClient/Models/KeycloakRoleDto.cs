using System.Text.Json.Serialization;

namespace KeycloakNet.KeycloakClient.Models;

public class KeycloakRoleDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

