using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using XmasPostcards.Models;

namespace XmasPostcards.Services;

public class PostcardService(OpenAIClient openAIClient, HttpClient dallEClient, IOptions<OpenAISettings> openAISettingsOptions)
{
    private readonly OpenAISettings openAISettings = openAISettingsOptions.Value;

    public async Task<Postcard> GenerateAsync()
    {
        // L'immagine può contenere ad esempio un paesaggio sotto la neve, un presepe, renne, un albero di Natale, pupazzi di neve, persone festose, bambini che giocano. Non è necessario che nella cartolina sia presenti tutti questi soggetti. Non è obbligatorio che nella cartolina ci sia un albero di Natale.
        var chatGptResponse = await openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = openAISettings.Model,
            Messages =
            {
                new(ChatRole.System, "Sei un assistente che genera descrizioni di cartoline da inviare ad amici e parenti."),
                new(ChatRole.User, """
                    Crea una descrizione casuale per una cartolina di Natale.                     
                    Descrivi i dettagli dell'immagine e parla dei colori usati.
                    Non inserire alcuna scritta nella cartolina. 
                    Non dire che si tratta di una cartolina. Non commentare lo scopo della cartolina.
                    Non usare più di 1000 caratteri. Non andare mai a capo nella descrizione.
                    """),
            },
            MaxTokens = 800
        });

        var description = chatGptResponse.Value.Choices[0].Message.Content;

        using var imageGenerationResponse = await dallEClient.PostAsJsonAsync($"openai/deployments/{openAISettings.DallEModel}/images/generations?api-version=2023-12-01-preview",
            new
            {
                Prompt = description,
                Size = $"{openAISettings.ImageWidth}x{openAISettings.ImageHeight}"
            });

        using var streamResponse = await imageGenerationResponse.Content.ReadAsStreamAsync();
        var content = await JsonDocument.ParseAsync(streamResponse);
        var imageUrl = content.RootElement.GetProperty("data")[0].GetProperty("url").GetString()!;

        return new Postcard(description, imageUrl);
    }
}
