using Azure;
using Microsoft.Extensions.Azure;
using TinyHelpers.AspNetCore.Extensions;
using XmasPostcards.Models;
using XmasPostcards.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: true);

// Add services to the container.
var openAISettings = builder.Services.ConfigureAndGet<OpenAISettings>(builder.Configuration, "OpenAI")!;

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddOpenAIClient(
        new Uri(openAISettings.Endpoint),
        new AzureKeyCredential(openAISettings.Credential)
    );
});

builder.Services.AddHttpClient<PostcardService>(client =>
{
    client.BaseAddress = new Uri(openAISettings.Endpoint);
    client.DefaultRequestHeaders.TryAddWithoutValidation("Api-Key", openAISettings.Credential);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/postcard", async (PostcardService postcardService) =>
{
    var postcard = await postcardService.GenerateAsync();
    return TypedResults.Ok(postcard);
})
.WithOpenApi();

app.Run();
