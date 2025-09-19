﻿using System;
using System.Runtime.InteropServices;
using Azure;
using Azure.Identity;
using Azure.Identity.Broker;
using Azure.Security.KeyVault.Secrets;

/// <summary>
/// Get the handle of the console window for Linux
/// </summary>
[DllImport("libX11")]
static extern IntPtr XOpenDisplay(string? display);

[DllImport("libX11")]
static extern IntPtr XRootWindow(IntPtr display, int screen);

try
{
    IntPtr parentWindowHandle = XRootWindow(XOpenDisplay(null), 0);
    Func<IntPtr> consoleWindowHandleProvider = () => parentWindowHandle;

    InteractiveBrowserCredentialBrokerOptions brokerOptions = new(parentWindowHandle)
    {
        UseDefaultBrokerAccount = true,
    };

    // Create the InteractiveBrowserCredential using broker support
    InteractiveBrowserCredential credential = new(brokerOptions);

    Uri vaultUri = new("https://kv-scaddie.vault.azure.net/");
    SecretClient client = new(vaultUri, credential);

    Console.WriteLine("Retrieving secret 'MySecret' from Key Vault...");
    KeyVaultSecret secret = await client.GetSecretAsync("MySecret");
    Console.WriteLine($"Secret value: {secret.Value}");

    return 0;
}
catch (AuthenticationFailedException ex)
{
    Console.Error.WriteLine($"Authentication failed: {ex.Message}");
    return 2;
}
catch (RequestFailedException ex)
{
    Console.Error.WriteLine($"Key Vault request failed: {ex.Message}");
    return 3;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    return 1;
}
