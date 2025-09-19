using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Identity.Broker;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
#if WINDOWS
using SecretVaultApp.WinUI;
#endif
#if MACCATALYST
using Foundation;
using UIKit;
#endif
using System.Diagnostics;

namespace SecretVaultApp;

public partial class MainPage : ContentPage
{
    private const string KeyVaultUrl = "https://kv-scaddie.vault.azure.net/";
    private const string SecretName = "MySecret";

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnRetrieveSecretClicked(object? sender, EventArgs e)
    {
        // Show loading indicator and hide previous results
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ResultLabel.IsVisible = false;
        ErrorLabel.IsVisible = false;
        RetrieveSecretBtn.IsEnabled = false;

        try
        {
#if WINDOWS
            // Get the parent window handle for MAUI on Windows
            Microsoft.Maui.Controls.Window? parentWindow = this.GetParentWindow();
            Microsoft.UI.Xaml.Window? windowHandle = parentWindow?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            IntPtr hwnd = windowHandle != null ? WinRT.Interop.WindowNative.GetWindowHandle(windowHandle) : IntPtr.Zero;

            // ===== MSAL =====
            string[] scopes = ["https://vault.azure.net/.default"];

            BrokerOptions options = new(BrokerOptions.OperatingSystems.Windows)
            {
                Title = "My Awesome Application",
            };

            IPublicClientApplication app = PublicClientApplicationBuilder
                .Create("777b0380-bed8-46e5-83cd-a137ab47c667")
                .WithTenantId("72f988bf-86f1-41af-91ab-2d7cd011db47")
                .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                .WithBroker(options)
                .WithParentActivityOrWindow(() => hwnd)
                .WithDefaultRedirectUri()
                .Build();

            // Try to use the previously signed-in account from the cache
            IEnumerable<IAccount> accounts = await app.GetAccountsAsync();
            IAccount? existingAccount = accounts.FirstOrDefault();
            AuthenticationResult result = existingAccount != null
                ? await app.AcquireTokenSilent(scopes, existingAccount).ExecuteAsync()
                : await app.AcquireTokenInteractive(scopes).ExecuteAsync();

            TokenCredential credential = new MsalTokenCredential(result);
#elif MACCATALYST
            // Get the parent window handle for MAUI on Mac Catalyst
            Microsoft.Maui.Controls.Window? parentWindow = this.GetParentWindow();
            UIWindow? uiWindow = parentWindow?.Handler?.PlatformView as UIWindow;
            IntPtr windowHandle = uiWindow?.Handle ?? IntPtr.Zero;

            // Configure InteractiveBrowserCredentialBrokerOptions with parent window reference
            InteractiveBrowserCredentialBrokerOptions options = new(windowHandle)
            {
                UseDefaultBrokerAccount = true,
            };

            // Create credential that will use the broker on macOS
            InteractiveBrowserCredential credential = new(options);
#else
            // For non-Windows and non-macOS platforms, use standard interactive browser credential
            InteractiveBrowserCredential credential = new();
#endif

            SecretClient client = new(new Uri(KeyVaultUrl), credential);
            KeyVaultSecret secret = await client.GetSecretAsync(SecretName);

            // Display the secret value (in production, be careful about displaying secrets)
            ResultLabel.Text = $"✅ Secret '{SecretName}' retrieved successfully!\n" +
                              $"🔑 Value: {secret.Value}\n" +
                              $"📅 Created: {secret.Properties.CreatedOn:yyyy-MM-dd HH:mm:ss}";
            ResultLabel.IsVisible = true;

            Debug.WriteLine($"Successfully retrieved secret: {SecretName}");
        }
        catch (RequestFailedException ex)
        {
            string errorMessage = ex.Status switch
            {
                401 => "❌ Authentication failed. Please ensure you're signed in to Azure and have the correct permissions.",
                403 => "🚫 Access denied. Please check your Azure Key Vault access policies.",
                404 => $"🔍 Secret '{SecretName}' not found in the Key Vault. Please verify the secret name and Key Vault URL.",
                _ => $"⚠️ Azure Key Vault error ({ex.Status}): {ex.Message}"
            };

            ErrorLabel.Text = errorMessage;
            ErrorLabel.IsVisible = true;
            Debug.WriteLine($"RequestFailedException: Status={ex.Status}, Message={ex.Message}");
        }
        catch (AuthenticationFailedException ex)
        {
            ErrorLabel.Text = $"🔐 Authentication failed: {ex.Message}\n\nPlease ensure you're signed in to Azure and try again.";
            ErrorLabel.IsVisible = true;
            Debug.WriteLine($"AuthenticationFailedException: {ex.Message}");
        }
        catch (UriFormatException)
        {
            ErrorLabel.Text = "🌐 Invalid Key Vault URL. Please update the KeyVaultUrl in the code with your actual Key Vault URL.";
            ErrorLabel.IsVisible = true;
            Debug.WriteLine("Invalid KeyVaultUrl format");
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"💥 An unexpected error occurred: {ex.Message}";
            ErrorLabel.IsVisible = true;
            Debug.WriteLine($"Unexpected Exception: {ex.GetType().Name} - {ex.Message}");
        }
        finally
        {
            // Hide loading indicator and re-enable button
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RetrieveSecretBtn.IsEnabled = true;
        }
    }
}

class MsalTokenCredential(AuthenticationResult authResult) : TokenCredential
{
    public override AccessToken GetToken(
        TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new AccessToken(authResult.AccessToken, authResult.ExpiresOn);

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new ValueTask<AccessToken>(new AccessToken(authResult.AccessToken, authResult.ExpiresOn));
}
