using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Moana.Configurations;

namespace Moana.Services
{
    public class OpenAIService
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;

		public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
		{
			_httpClient = httpClient;
			_apiKey = options.Value.ApiKey;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

		public async Task<string> GetTradingAdviceAsync(string marketData)
		{
			var requestData = new
			{
				model = "gpt-4o",
				messages = new[]
				{
					new { role = "system", content = "Tu es un assistant expert en trading crypto. Donne des recommandations de points d'entrée et de sortie pour le marché suivant." },
					new { role = "user", content = marketData }
				},
				max_completion_tokens = 10
			};

			var requestContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completionsT", requestContent);
			if (!response.IsSuccessStatusCode)
			{
				return "Erreur lors de l'appel à OpenAI.";
			}

			var responseContent = await response.Content.ReadAsStringAsync();
			using var jsonDoc = JsonDocument.Parse(responseContent);
			var reply = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

			return reply;
		}
	}
}
