namespace XmasPostcards.Models;

public class OpenAISettings
{
    public string Endpoint { get; init; } = null!;

    public string Credential { get; init; } = null!;

    public string Model { get; init; } = null!;

    public string DallEModel { get; init; } = null!;

    public int ImageWidth { get; init; } = 1792;

    public int ImageHeight { get; init; } = 1024;
}
