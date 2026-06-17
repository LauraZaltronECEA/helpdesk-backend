namespace api.models.Responses;

// Base response class returned by most API endpoints.
// Contains a success flag, a numeric code, and a human-readable message.
public class GeneralResponse
{
    // Indicates whether the operation succeeded (true) or failed (false).
    public bool Estado { get; set; }

    // Numeric code: 1 = success, 400 = bad request, 403 = forbidden, 404 = not found, 500 = error.
    public int Codigo { get; set; }

    // Human-readable result or error message.
    public string Mensaje { get; set; } = string.Empty;
}
