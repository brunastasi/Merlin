using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Merlin.Configurations;
using Merlin.Models;
using Merlin.Services;
using Merlin.Services.MarketData;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

// Configuration des services dans une application console
var services = new ServiceCollection();

// Charger la configuration à partir du fichier appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Ajouter les configurations
services.Configure<BinanceOptions>(configuration.GetSection("API").GetSection("BinanceTest"));
services.Configure<OpenAIOptions>(configuration.GetSection("API").GetSection("OpenAI"));
services.Configure<NewsAPIOptions>(configuration.GetSection("API").GetSection("NewsAPI"));
services.Configure<AlphaVantageOptions>(configuration.GetSection("API").GetSection("AlphaVantage"));
services.Configure<UserPreferences>(configuration.GetSection("TradingSettings"));


// Ajouter les services nécessaires
services.AddSingleton<LoggerService>();

services.AddHttpClient<BinanceService>();
services.AddHttpClient<OpenAIService>();
services.AddHttpClient<NewsAPIService>();
services.AddHttpClient<AlternativeAPIService>();
services.AddHttpClient<AlphaVantageService>();

services.AddScoped<VolumeService>();
services.AddScoped<TrendService>();
services.AddScoped<IndicatorsService>();
services.AddScoped<LiquidityService>();
services.AddScoped<DerivativesService>();
services.AddScoped<FundamentalService>();
services.AddScoped<SentimentService>();
services.AddScoped<EconomicEventService>();
services.AddScoped<CorrelationService>();
services.AddScoped<DataAggregatorService>();
services.AddScoped<TradingStrategyService>();
services.AddScoped<TradingExecutionService>();

// Construire le conteneur de services
var serviceProvider = services.BuildServiceProvider();

// Récupérer les services nécessaires
var loggerService = serviceProvider.GetRequiredService<LoggerService>();
Console.WriteLine("Lancement du BOT DE TRADING");

var binanceService = serviceProvider.GetRequiredService<BinanceService>();
var openAIService = serviceProvider.GetRequiredService<OpenAIService>();
var userPreferences = serviceProvider.GetRequiredService<IOptions<UserPreferences>>().Value;

var volumeService = serviceProvider.GetRequiredService<VolumeService>();
var trendService = serviceProvider.GetRequiredService<TrendService>();
var indicatorsService = serviceProvider.GetRequiredService<IndicatorsService>();
var liquidityService = serviceProvider.GetRequiredService<LiquidityService>();
var derivativesService = serviceProvider.GetRequiredService<DerivativesService>();
var fundamentalService = serviceProvider.GetRequiredService<FundamentalService>();
var sentimentService = serviceProvider.GetRequiredService<SentimentService>();
var economicEventService = serviceProvider.GetRequiredService<EconomicEventService>();
var correlationService = serviceProvider.GetRequiredService<CorrelationService>();
var dataAggregatorService = serviceProvider.GetRequiredService<DataAggregatorService>();
var tradingStrategyService = serviceProvider.GetRequiredService<TradingStrategyService>();
var tradingExecutionService = serviceProvider.GetRequiredService<TradingExecutionService>();

// Fonction principale pour exécuter l'analyse et le trading
async Task ExecuteAnalyzeAndTradeAsync(
    string symbol,
    List<(string Asset, string Type)> assets,
    UserPreferences userPreferences,
    bool useAIAnalysis = false)
{
    try
    {
        loggerService.LogInformation($"Démarrage de l'analyse et du trading pour {symbol}", "TRADING");

        // Étape 1 : Analyse des données du marché
        string analysisResult = await tradingStrategyService.AnalyzeAndExecuteStrategyAsync(symbol, assets, userPreferences, useAIAnalysis);

        // Étape 2 : Conversion des résultats en TradingDecision
        var decision = JsonSerializer.Deserialize<TradingDecision>(analysisResult);

        if (decision == null || string.IsNullOrWhiteSpace(decision.Action))
        {
            loggerService.LogInformation($"Aucune décision valide obtenue pour {symbol}. Analyse ignorée.", "TRADING");
            return;
        }

        // Étape 3 : Exécution de la décision de trading
        await tradingExecutionService.ExecuteTradingDecisionAsync(decision, symbol, userPreferences.Budget, userPreferences.Leverage);
    }
    catch (Exception ex)
    {
        loggerService.LogError($"Erreur lors de l'exécution de l'analyse et du trading pour {symbol} : {ex.Message}", "ERROR");
        throw;
    }
}

// Exécution du bot
await ExecuteAnalyzeAndTradeAsync(
    symbol: userPreferences.Symbol,
    assets: userPreferences.Assets,
    userPreferences: userPreferences,
    useAIAnalysis: userPreferences.UseAIAnalysis);

Console.WriteLine("Exécution du BOT de trading terminée.");

Console.WriteLine("Appuyez sur une touche pour quitter...");
Console.ReadKey();
