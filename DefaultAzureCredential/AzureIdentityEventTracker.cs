using Azure.Core.Diagnostics;
using System.Diagnostics.Tracing;

namespace IdentityPlayground.WebApi;

/// <summary>
/// Tracks Azure Identity events to provide insights into credential selection and token acquisition.
/// </summary>
public sealed class AzureIdentityEventTracker : IDisposable
{
    private readonly AzureEventSourceListener _listener;
    private readonly List<string> _events = [];
    private string? _credentialSelection;

    public string? CredentialSelection => _credentialSelection;
    public IReadOnlyList<string> Events => _events;

    public AzureIdentityEventTracker()
    {
        _listener = new AzureEventSourceListener(HandleEvent, EventLevel.Informational);
    }

    private void HandleEvent(EventWrittenEventArgs args, string message)
    {
        if (args is { EventSource.Name: "Azure-Identity" })
        {
            switch (args.EventName)
            {
                // NOTE: These events don't run on subsequent token requests because the selected credential is cached
                case "GetTokenSucceeded":
                    _credentialSelection = ExtractCredentialName(message);
                    _events.Add($"{args.EventName}: {message}");
                    break;
                case "GetToken" or "GetTokenFailed":
                    _events.Add($"{args.EventName}: {message}");
                    break;
            }
        }
    }

    public void Dispose() => _listener?.Dispose();

    private static string ExtractCredentialName(string message)
    {
        const string suffix = ".GetToken succeeded.";
        int index = message.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? message.Substring(0, index) : message;
    }
}