#define Demo1

using Microsoft.Extensions.Azure;
using IdentityPlayground.WebApi;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAzureClients(configureClients: c =>
{
    IConfigurationSection keyVaultConfig = builder.Configuration.GetSection("Azure:KeyVault");

#if Demo1
    c.AddSecretClient(keyVaultConfig);
#elif Demo2
    c.AddSecretClient(keyVaultConfig);
    //.WithCredential(new DefaultAzureCredential(DefaultAzureCredential.DefaultEnvironmentVariableName));
#elif Demo3
    ChainedTokenCredential credential = new(
        new AzureCliCredential(),
        new VisualStudioCredential());
    c.AddSecretClient(keyVaultConfig)
        .WithCredential(credential);
#elif Demo4
    IConfigurationSection storageConfig = builder.Configuration.GetSection("Azure:Storage");

    c.AddSecretClient(keyVaultConfig);
    c.AddBlobServiceClient(storageConfig);

    c.UseCredential(new DefaultAzureCredential(
        DefaultAzureCredential.DefaultEnvironmentVariableName));
#endif
});

var app = builder.Build();
app.UseHttpsRedirection();
app.RegisterEndpoints();
app.Run();
