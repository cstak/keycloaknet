using System.ComponentModel.DataAnnotations;

namespace KeycloakNet.KeycloakClient.Options;

public class KeycloakClientOptions
{
    public const string SectionName = "KeycloakClient";

    /// <summary>
    /// Base URL of the Keycloak server (e.g. http://localhost:8080)
    /// </summary>
    [Required(ErrorMessage = "Keycloak AuthServerUrl is required.")]
    [Url(ErrorMessage = "Keycloak AuthServerUrl must be a valid URL.")]
    public string AuthServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Target realm name to manage (e.g. keycloaknet-realm)
    /// </summary>
    [Required(ErrorMessage = "Keycloak Realm is required.")]
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Realm used to acquire the admin access token (e.g. keycloaknet-realm)
    /// </summary>
    [Required(ErrorMessage = "Keycloak AdminRealm is required.")]
    public string AdminRealm { get; set; } = string.Empty;

    /// <summary>
    /// Client ID of the service account client (e.g. keycloaknet-admin-client)
    /// </summary>
    [Required(ErrorMessage = "Keycloak ClientId is required.")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for client credentials grant flow (M2M Service Account)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Optional admin username for password grant flow fallback
    /// </summary>
    public string? AdminUsername { get; set; }

    /// <summary>
    /// Optional admin password for password grant flow fallback
    /// </summary>
    public string? AdminPassword { get; set; }
}
