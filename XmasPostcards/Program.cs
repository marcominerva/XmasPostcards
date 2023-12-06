using System.Diagnostics;
using System.Threading.RateLimiting;
using Azure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Azure;
using TinyHelpers.AspNetCore.Extensions;
using XmasPostcards.Extensions;
using XmasPostcards.Models;
using XmasPostcards.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddRequestLocalization("it", "en");

builder.Services.AddWebOptimizer(minifyCss: true, minifyJavaScript: builder.Environment.IsProduction());

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

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
    {
        return RateLimitPartition.GetTokenBucketLimiter("Default", _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 500,
            TokensPerPeriod = 50,
            ReplenishmentPeriod = TimeSpan.FromHours(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var window))
        {
            context.HttpContext.Response.Headers.RetryAfter = window.TotalSeconds.ToString();
        }

        return ValueTask.CompletedTask;
    };
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var statusCode = context.ProblemDetails.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);
        context.ProblemDetails.Type ??= $"https://httpstatuses.io/{statusCode}";
        context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();
app.Services.GetRequiredService<IWebHostEnvironment>().ApplicationName = builder.Configuration.GetValue<string>("SiteName")!;

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseWhen(context => context.IsWebRequest(), builder =>
{
    if (!app.Environment.IsDevelopment())
    {
        builder.UseExceptionHandler("/errors/500");

        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        builder.UseHsts();
    }

    builder.UseStatusCodePagesWithReExecute("/errors/{0}");
});

app.UseWhen(context => context.IsApiRequest(), builder =>
{
    builder.UseExceptionHandler();
    builder.UseStatusCodePages();
});

app.UseWebOptimizer();
app.UseStaticFiles();

app.UseRouting();
app.UseRequestLocalization();

app.UseWhen(context => context.IsApiRequest(), builder =>
{
    builder.UseRateLimiter();
});

app.MapRazorPages();

app.MapPost("/api/postcard", async (PostcardService postcardService) =>
{
    var postcard = await postcardService.GenerateAsync();
    return TypedResults.Ok(postcard);
})
.WithOpenApi();

app.Run();
