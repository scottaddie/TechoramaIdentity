using Azure;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace IdentityPlayground.WebApi
{
    public static class Endpoints
    {
        public static void RegisterEndpoints(this WebApplication app)
        {
            app.MapGet("/Secret", async (SecretClient secretClient) =>
            {
                using AzureIdentityEventTracker tracker = CreateAzureIdentityListener();

                try
                {
                    Response<KeyVaultSecret> secret = await secretClient.GetSecretAsync("MySecret");

                    return Results.Ok(new
                    {
                        Message = "Secret retrieved successfully",
                        SecretRetrieved = true,
                        SecretName = secret.Value.Name,
                        Timestamp = DateTime.UtcNow,
                        CredentialUsed = tracker.CredentialSelection,
                        CredentialChainAttempted = tracker.Events,
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
            });

            //app.MapGet("/BlobContainers", async (BlobServiceClient blobClient) =>
            //{
            //    using AzureIdentityEventTracker tracker = CreateAzureIdentityListener();
            //    List<string> containerNames = [];

            //    try
            //    {
            //        await foreach (BlobContainerItem container in blobClient.GetBlobContainersAsync())
            //        {
            //            containerNames.Add(container.Name);
            //        }

            //        return Results.Ok(new
            //        {
            //            Message = "Blob containers listed successfully",
            //            BlobContainersListed = true,
            //            ContainersFound = containerNames,
            //            Timestamp = DateTime.UtcNow,
            //            CredentialUsed = tracker.CredentialSelection,
            //            CredentialChainAttempted = tracker.Events,
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        return Results.Problem(
            //            title: "Failed to list blob containers",
            //            detail: ex.Message,
            //            statusCode: 500
            //        );
            //    }
            //});

            //// NOTE: Duplicate events are logged when using 2 clients. Not an issue when only using 1 client.
            //app.MapGet("/Test", async (BlobServiceClient blobClient, SecretClient secretClient) =>
            //{
            //    using AzureIdentityEventTracker tracker = CreateAzureIdentityListener();
            //    List<string> containerNames = [];

            //    try
            //    {
            //        await foreach (BlobContainerItem container in blobClient.GetBlobContainersAsync())
            //        {
            //            containerNames.Add(container.Name);
            //        }

            //        Response<KeyVaultSecret> secret = await secretClient.GetSecretAsync("MySecret");

            //        return Results.Ok(new
            //        {
            //            Message = "Secret and blob containers listed successfully",
            //            BlobContainersListed = true,
            //            ContainersFound = containerNames,
            //            SecretRetrieved = true,
            //            SecretName = secret.Value.Name,
            //            Timestamp = DateTime.UtcNow,
            //            CredentialUsed = tracker.CredentialSelection,
            //            CredentialChainAttempted = tracker.Events,
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        return Results.Problem(
            //            title: "Failed to retrieve secret or blob containers",
            //            detail: ex.Message,
            //            statusCode: 500
            //        );
            //    }
            //});
        }

        private static AzureIdentityEventTracker CreateAzureIdentityListener() => new();
    }
}
