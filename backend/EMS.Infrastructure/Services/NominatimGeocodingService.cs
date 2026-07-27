using EMS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NominatimGeocodingService> _logger;

        public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken ct = default)
        {
            try
            {
                var url = $"reverse?format=jsonv2&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}";
                using var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Reverse geocoding request failed with {StatusCode} for ({Latitude}, {Longitude})", response.StatusCode, latitude, longitude);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return doc.RootElement.TryGetProperty("display_name", out var displayName) ? displayName.GetString() : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A geocoding outage must never block a punch — Latitude/Longitude are still recorded either way.
                _logger.LogWarning(ex, "Reverse geocoding threw for ({Latitude}, {Longitude})", latitude, longitude);
                return null;
            }
        }
    }
}
