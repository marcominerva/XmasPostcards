using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using XmasPostcards.Models;

namespace XmasPostcards.Services;

public class PostcardService(OpenAIClient openAIClient, IOptions<OpenAISettings> openAISettingsOptions)
{
    private readonly OpenAISettings openAISettings = openAISettingsOptions.Value;

    public async Task<Postcard> GenerateAsync()
    {
        // L'immagine può contenere ad esempio un paesaggio sotto la neve, un presepe, renne, un albero di Natale, pupazzi di neve, persone festose, bambini che giocano. Non è necessario che nella cartolina sia presenti tutti questi soggetti. Non è obbligatorio che nella cartolina ci sia un albero di Natale.
        var chatGptResponse = await openAIClient.GetChatCompletionsAsync(new()
        {
            DeploymentName = openAISettings.Model,
            Messages =
            {
                new ChatRequestSystemMessage("Sei un assistente che genera descrizioni di cartoline da inviare ad amici e parenti."),
                new ChatRequestUserMessage("""
                    Crea una descrizione casuale per una cartolina di Natale.                     
                    Descrivi i dettagli dell'immagine e parla dei colori usati.
                    Non inserire alcuna scritta nella cartolina. 
                    Non dire che si tratta di una cartolina. Non commentare lo scopo della cartolina.
                    Non usare più di 1000 caratteri. Non andare mai a capo nella descrizione.
                    """),
            },
            Temperature = 1F,
            MaxTokens = 800
        });

        var description = chatGptResponse.Value.Choices[0].Message.Content;

        var imageGeneration = await openAIClient.GetImageGenerationsAsync(new()
        {
            DeploymentName = openAISettings.DallEModel,
            Prompt = description,
            Size = $"{openAISettings.ImageWidth}x{openAISettings.ImageHeight}"
        });

        var imageUrl = imageGeneration.Value.Data[0].Url.ToString();
        return new Postcard(imageUrl, description);
    }
}
