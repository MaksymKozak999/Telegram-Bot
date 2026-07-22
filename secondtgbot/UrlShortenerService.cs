using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace secondtgbot
{
    public static class UrlShortenerService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ShortenUrlAsync(string longUrl)
        {
            try
            {
                // Validate that the string is a properly formatted HTTP/HTTPS URL
                if (!Uri.TryCreate(longUrl, UriKind.Absolute, out var uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    return null;
                }

                // Call the free is.gd endpoint
                string requestUrl = $"https://is.gd/create.php?format=simple&url={Uri.EscapeDataString(longUrl)}";
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"URL Shortening Error: {ex.Message}");
            }

            return null;
        }
    }
}
