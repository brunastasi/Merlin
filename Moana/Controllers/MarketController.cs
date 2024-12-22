using Microsoft.AspNetCore.Mvc;
using Moana.Services;

namespace Moana.Controllers
{
    [ApiController]
	[Route("api/[controller]")]
	public class MarketController : ControllerBase
	{
		private readonly BinanceService _binanceService;
		private readonly OpenAIService _openAIService;

		public MarketController(BinanceService binanceService, OpenAIService openAIService)
		{
			_binanceService = binanceService;
			_openAIService = openAIService;
		}

		[HttpGet("price/{symbol}")]
		public async Task<IActionResult> GetPrice(string symbol)
		{
			try
			{
				var price = await _binanceService.GetPriceAsync(symbol);
				return Ok(new { symbol, price });
			}
			catch
			{
				return BadRequest("Error fetching the price.");
			}
		}

		[HttpGet("advice/{symbol}")]
		public async Task<IActionResult> GetTradingAdvice(string symbol)
		{
			try
			{
				// Récupérer le prix actuel depuis Binance
				var price = await _binanceService.GetPriceAsync(symbol);

				// Préparer les données de marché pour GPT-4
				string marketData = $"Le prix actuel de {symbol} est {price}. Donne une recommandation de trading (entrée, sortie et stop-loss).";

				// Appeler GPT-4 via OpenAIService
				var advice = await _openAIService.GetTradingAdviceAsync(marketData);

				return Ok(new { symbol, price, advice });
			}
			catch
			{
				return BadRequest("Erreur lors de la récupération des données ou de l'analyse.");
			}
		}

	}
}
