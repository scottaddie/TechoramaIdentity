using Microsoft.Extensions.Azure;
using IdentityPlayground.WebApi;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAzureClients(configureClients: c =>
{
    IConfigurationSection keyVaultConfig = 
        builder.Configuration.GetSection("Azure:KeyVault");
    IConfigurationSection storageConfig = 
        builder.Configuration.GetSection("Azure:Storage");

    c.AddSecretClient(keyVaultConfig);
    c.AddBlobServiceClient(storageConfig);

    c.UseCredential(new DefaultAzureCredential(
        DefaultAzureCredential.DefaultEnvironmentVariableName));
});

var app = builder.Build();
app.UseHttpsRedirection();
app.RegisterEndpoints();
app.Run();
