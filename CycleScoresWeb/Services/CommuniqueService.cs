using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CycleScoresWeb.Models;
using System.Text;
using System.Text.Json;

namespace CycleScoresWeb.Services
{
    public interface ICommuniqueService
    {
        public Task<Communique> FetchCommunique(Guid communiqueId);
    }

    public class CommuniqueService: ICommuniqueService
    {
        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainerClient;
        private JsonSerializerOptions _options;

        public CommuniqueService() 
        {
            _blobServiceClient = new BlobServiceClient(
                new Uri("https://cyclescoresweb.blob.core.windows.net"),
                new DefaultAzureCredential());

            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("communiques");
            _options = new JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                Converters = { new ForgivingStringConverter() }
            };

        }

        public async Task<Communique> FetchCommunique(Guid communiqueId)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient($"{communiqueId}.json");
                BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
                string blobContents = downloadResult.Content.ToString();
                var c = JsonSerializer.Deserialize<Communique>(blobContents, _options);
                if (c != null)
                    return c;
                throw new InvalidDataException("Json could not be deserialised");
            }
            catch
            {
                throw new FileNotFoundException($"Communique with id {communiqueId} could not be found;");
            }
        }

        private class ForgivingStringConverter : System.Text.Json.Serialization.JsonConverter<string>
        {
            public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.False => "false",
                    JsonTokenType.True => "true",
                    JsonTokenType.Number => reader.GetDouble().ToString(),
                    _ => reader.GetString()
                };
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
