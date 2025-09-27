using Microsoft.Extensions.Azure;
using IdentityPlayground.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAzureClients(configureClients: c =>
{
    IConfigurationSection keyVaultConfig = builder.Configuration.GetSection("Azure:KeyVault");
    IConfigurationSection storageConfig = builder.Configuration.GetSection("Azure:Storage");

    #region "DEMO 1: Use implicit DAC"
    // Send 1 request, then "%LOCALAPPDATA%\.IdentityService\msalV2.cache" is created, which is where the token is cached by MSAL.
    // Stop API and send another request. The cache file is updated.
    c.AddSecretClient(keyVaultConfig);
    #endregion

    #region "DEMO 1.2: Use implicit DAC w/ env var in launchSettings.json"
    // 1. Set env var to "dev" and explain why VisualStudioCredential is used.
    // 2. Set env var to "VisualStudioCodeCredential" and explain why VSCodeCredential is used.
    #endregion

    #region "DEMO 2: Use specific cred (VS)"
    //c.AddSecretClient(keyVaultConfig).WithCredential(new VisualStudioCredential());
    #endregion

    #region "DEMO 3: Use specific cred for a different dev tool"
    //c.AddSecretClient(keyVaultConfig).WithCredential(new AzureCliCredential());
    #endregion

    #region "DEMO 4: Validate value of AZURE_TOKEN_CREDENTIALS"
    // 1. Change to invalid value to show exception.
    //c.AddSecretClient(keyVaultConfig).WithCredential(
    //    new DefaultAzureCredential(DefaultAzureCredential.DefaultEnvironmentVariableName));
    #endregion

    #region "DEMO 5: UseCredential with multiple clients"
    //c.AddSecretClient(keyVaultConfig);
    //c.AddBlobServiceClient(storageConfig);

    //c.UseCredential(new DefaultAzureCredential(
    //    DefaultAzureCredential.DefaultEnvironmentVariableName));
    #endregion
});

var app = builder.Build();
app.UseHttpsRedirection();
app.RegisterEndpoints();
app.Run();
