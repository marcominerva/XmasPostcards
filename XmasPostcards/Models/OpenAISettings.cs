namespace XmasPostcards.Models;

public class OpenAISettings
{
    public string Endpoint { get; init; } = null!;

    public string Credential { get; init; } = null!;

    public string Model { get; init; } = null!;

    public string DallEModel { get; init; } = null!;
}
