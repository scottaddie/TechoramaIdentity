using Azure;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using System.Diagnostics.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAzureClients(configureClients: c =>
{
    IConfigurationSection keyVaultConfig = builder.Configuration.GetSection("KeyVault");

    // DEMO 1.1: Use implicit DAC
    //c.AddSecretClient(keyVaultConfig);

    // DEMO 1.2: Use implicit DAC w/ env var set in launchSettings.json
    //TODO: this doesn't work because Microsoft.Extensions.Azure still uses an old version of Azure.Identity.
    //TODO: Chris is working on a PR and will ship v1.13.0 next week (https://github.com/Azure/azure-sdk-for-net/pull/52733).
    c.AddSecretClient(keyVaultConfig);

    // DEMO 2: Use specific credential (VS)
    //c.AddSecretClient(keyVaultConfig).WithCredential(new VisualStudioCredential());

    // DEMO 3: Use specific credential for a different dev tool
    //c.AddSecretClient(keyVaultConfig).WithCredential(new AzureCliCredential());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.MapGet("/Secret", async (SecretClient secretClient) =>
{
    string? credSelection = null;
    List<string> messages = new();

    using AzureEventSourceListener listener = new((args, message) =>
    {
        // Log all credentials attempted and the one selected
        if (args is {
            EventSource.Name: "Azure-Identity",
            EventName: "GetToken" or "GetTokenFailed" or "GetTokenSucceeded" or "DefaultAzureCredentialCredentialSelected"
        })
        {
            if (args.EventName == "DefaultAzureCredentialCredentialSelected")
                credSelection = ExtractCredentialName(message);
            else
                messages.Add(message);
        }
    }, EventLevel.Informational);

    try
    {
        Response<KeyVaultSecret> secret = await secretClient.GetSecretAsync("MySecret");

        return Results.Ok(new
        {
            Message = "Secret retrieved successfully",
            SecretRetrieved = true,
            SecretName = secret.Value.Name,
            Timestamp = DateTime.UtcNow,
            CredentialUsed = credSelection,
            CredentialChainAttempted = messages,
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Failed to retrieve secret",
            detail: ex.Message,
            statusCode: 500
        );
    }

    static string ExtractCredentialName(string message)
    {
        int index = message.IndexOf("Azure.Identity.");
        return index >= 0 ? message.Substring(index + "Azure.Identity.".Length) : message;
    }
});

app.Run();
