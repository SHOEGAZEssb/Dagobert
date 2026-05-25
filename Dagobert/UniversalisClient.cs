using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dagobert;

internal sealed class UniversalisClient : IDisposable
{
  private const int ListingCount = 10;
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  private readonly HttpClient _client;

  public UniversalisClient()
  {
    _client = new HttpClient
    {
      BaseAddress = new Uri("https://universalis.app/api/v2/"),
    };

    _client.DefaultRequestHeaders.UserAgent.ParseAdd($"Dagobert/{Plugin.PluginInterface.Manifest.AssemblyVersion}");
  }

  public async Task<UniversalisMarketDataResponse> GetMarketData(uint itemId, string worldDcRegion, bool hqOnly, CancellationToken cancellationToken)
  {
    var query = hqOnly ? $"?listings={ListingCount}&entries=0&hq=true" : $"?listings={ListingCount}&entries=0";
    var requestUri = new Uri($"{Uri.EscapeDataString(worldDcRegion)}/{itemId}{query}", UriKind.Relative);

    using var stream = await _client.GetStreamAsync(requestUri, cancellationToken).ConfigureAwait(false);
    return await JsonSerializer.DeserializeAsync<UniversalisMarketDataResponse>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
      ?? throw new InvalidOperationException($"Failed to parse Universalis market data for item {itemId} on {worldDcRegion}.");
  }

  public void Dispose()
  {
    _client.Dispose();
  }
}

internal sealed class UniversalisMarketDataResponse
{
  [JsonPropertyName("listings")]
  public List<UniversalisMarketDataListing> Listings { get; set; } = [];

  [JsonPropertyName("hasData")]
  public bool HasData { get; set; }
}

internal sealed class UniversalisMarketDataListing
{
  [JsonPropertyName("hq")]
  public bool Hq { get; set; }

  [JsonPropertyName("pricePerUnit")]
  public long PricePerUnit { get; set; }

  [JsonPropertyName("retainerID")]
  public string? RetainerId { get; set; }

  [JsonPropertyName("worldName")]
  public string? WorldName { get; set; }
}
