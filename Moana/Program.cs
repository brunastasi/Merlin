using Binance.Net.Enums;
using Moana.Configurations;
using Moana.Models.MarketData;
using Moana.Services;
using Moana.Services.MarketData;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BinanceOptions>(builder.Configuration.GetSection("API").GetSection("Binance"));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("API").GetSection("OpenAI"));
builder.Services.Configure<NewsAPIOptions>(builder.Configuration.GetSection("API").GetSection("NewsAPI"));
builder.Services.Configure<AlphaVantageOptions>(builder.Configuration.GetSection("API").GetSection("AlphaVantage"));

// Add services to the container.
builder.Services.AddHttpClient<BinanceService>();
builder.Services.AddHttpClient<OpenAIService>();
builder.Services.AddHttpClient<NewsAPIService>();
builder.Services.AddHttpClient<AlternativeAPIService>();
builder.Services.AddHttpClient<AlphaVantageService>();

builder.Services.AddScoped<VolumeService>();
builder.Services.AddScoped<TrendService>();
builder.Services.AddScoped<IndicatorsService>();
builder.Services.AddScoped<LiquidityService>();
builder.Services.AddScoped<DerivativesService>();
builder.Services.AddScoped<FundamentalService>();
builder.Services.AddScoped<SentimentService>();
builder.Services.AddScoped<EconomicEventService>();
builder.Services.AddScoped<CorrelationService>();

var serviceProvider = builder.Services.BuildServiceProvider();

// Récupérer BinanceService depuis le conteneur DI
var binanceService = serviceProvider.GetRequiredService<BinanceService>();
var volumeService = serviceProvider.GetRequiredService<VolumeService>();
var trendService = serviceProvider.GetRequiredService<TrendService>();
var indicatorsService = serviceProvider.GetRequiredService<IndicatorsService>();
var liquidityService = serviceProvider.GetRequiredService<LiquidityService>();
var derivativesService = serviceProvider.GetRequiredService<DerivativesService>();
var fundamentalService = serviceProvider.GetRequiredService<FundamentalService>();
var sentimentService = serviceProvider.GetRequiredService<SentimentService>();
var economicEventService = serviceProvider.GetRequiredService<EconomicEventService>();
var correlationService = serviceProvider.GetRequiredService<CorrelationService>();



builder.Services.AddControllers();

builder.Services.AddOptions();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();


#region Test Service
// VOLUME SERVICE TEST
// Tester le service avec une paire de trading
//var symbol = "BTCUSDT";
//var volumeData = await volumeService.GetCryptoVolumeDataAsync(symbol);

//Console.WriteLine($"Volume Total sur 24h : {volumeData.Volume24h}");
//Console.WriteLine($"Ratio Acheteurs/Vendeurs : {volumeData.BuySellRatio}");
//Console.WriteLine($"Variation des Volumes : {volumeData.VolumeChangePercentage}%");

// TRENDSERVICE TEST
//var symbol = "BTCUSDT";
//KlineInterval interval = KlineInterval.OneHour;
//var trendData = await trendService.GetTrendDataAsync(symbol, interval);

//Console.WriteLine($"SMA : {trendData.SMA}");
//Console.WriteLine($"EMA : {trendData.EMA}");
//Console.WriteLine($"Support Level : {trendData.SupportLevel}");
//Console.WriteLine($"Resistance Level : {trendData.ResistanceLevel}");

//INDICATOR SERVICE
//var symbol = "BTCUSDT";
//var interval = KlineInterval.FourHour;
//var indicators = await indicatorsService.GetIndicatorsAsync(symbol, interval);

//Console.WriteLine($"RSI : {indicators.RSI}");
//Console.WriteLine($"MACD Line : {indicators.MACD.Line}");
//Console.WriteLine($"MACD Signal : {indicators.MACD.Signal}");
//Console.WriteLine($"Bollinger Bands : Upper = {indicators.BollingerBands.Upper}, Middle = {indicators.BollingerBands.Middle}, Lower = {indicators.BollingerBands.Lower}");
//Console.WriteLine($"ATR : {indicators.ATR}");
//Console.WriteLine($"Ichimoku : TenkanSen = {indicators.Ichimoku.TenkanSen}, KijunSen = {indicators.Ichimoku.KijunSen}, SenkouSpanA = {indicators.Ichimoku.SenkouSpanA}, SenkouSpanB = {indicators.Ichimoku.SenkouSpanB}");
//Console.WriteLine($"Stochastic : %K = {indicators.Stochastic.PercentK}, %D = {indicators.Stochastic.PercentD}");
//Console.WriteLine($"VWAP : {indicators.VWAP}");
//Console.WriteLine($"Parabolic SAR : {string.Join(", ", indicators.ParabolicSAR)}");
//Console.WriteLine($"ADX : {indicators.ADX}");
//Console.WriteLine($"CMF : {indicators.CMF}");
//Console.WriteLine($"RVI : {indicators.RVI}");
//Console.WriteLine($"Williams %R : {indicators.WilliamsR}");
//Console.WriteLine($"ADL : {indicators.ADL}");
//Console.WriteLine($"CMO : {indicators.CMO}");
//Console.WriteLine($"OBV : {indicators.OBV}");

// LIQUIDITY SERVICE
//var symbol = "BTCUSDT";
//var amountsToTest = new decimal[] { 20000, 50000, 100000 }; // Tester différents montants

//foreach (var amount in amountsToTest)
//{
//    Console.WriteLine($"Test avec un montant de {amount} USD");
//    var liquidityData = await liquidityService.GetLiquidityDataAsync(symbol, amount);
//    Console.WriteLine($"Profondeur du Carnet d'Ordres : {liquidityData.OrderBookDepth}");
//    Console.WriteLine($"Spread : {liquidityData.Spread}");
//    Console.WriteLine($"Volume des 10 Premiers Ordres : {liquidityData.TopOrderVolume}");
//    Console.WriteLine($"Slippage : {liquidityData.Slippage}");
//    Console.WriteLine($"Score de Liquidité : {liquidityData.LiquidityScore}");
//    Console.WriteLine("-------------------");
//}

// DERIVATIVES SERVICE

//var derivativesData = await derivativesService.GetDerivativesDataAsync("BTCUSDT");

//Console.WriteLine($"Open Interest : {derivativesData.OpenInterest}");
//Console.WriteLine($"Funding Rate : {derivativesData.FundingRate}");
//Console.WriteLine($"Long/Short Ratio: {derivativesData.LongShortRatio}");
//Console.WriteLine($"Long Positions: {derivativesData.LongPositions}");
//Console.WriteLine($"Short Positions: {derivativesData.ShortPositions}");
//Console.WriteLine($"Futures Volume : {derivativesData.FuturesVolume}");
//Console.WriteLine($"Last Updated : {derivativesData.LastUpdated}");

// FUNDAMENTAL SERVICE

//var newsData = await fundamentalService.GetMarketNewsAsync("BTC");
//foreach (var news in newsData)
//{
//    Console.WriteLine($"Date: {news.Date}");
//    Console.WriteLine($"Title: {news.Title}");
//    Console.WriteLine($"Description: {news.Description}");
//    Console.WriteLine($"Impact: {news.Impact}");
//    Console.WriteLine($"Summary: {news.Summary}");
//    Console.WriteLine($"Source: {news.Source}");
//    Console.WriteLine("-------------------");
//}

// SENTIMENT SERVICE
//var sentimentData = await sentimentService.GetMarketSentimentAsync();

//Console.WriteLine($"Fear & Greed Index: {sentimentData.FearGreedIndex}");
//Console.WriteLine($"Sentiment Classification: {sentimentData.SentimentClassification}");

// ECONOMIC EVENT SERVICE

//var events = await economicEventService.GetEconomicEventsAsync();

//foreach (var ev in events)
//{
//    Console.WriteLine($"Date : {ev.EventDate}, Titre : {ev.Title}, Description : {ev.Description}, Impact : {ev.ImpactLevel}, Pays : {ev.Country}");
//}

#endregion

// CORRELATION SERVICE

var assets = new List<(string Asset, string Type)>
{
    ("BTC", "crypto"),
    ("ETH", "crypto"),
    ("AAPL", "stock"),
    ("MSFT", "stock")
};

var correlationData = await correlationService.GetCorrelationDataAsync(assets, "daily");

foreach (var data in correlationData)
{
    Console.WriteLine($"{data.Asset1} - {data.Asset2} : Corrélation = {data.CorrelationCoefficient}");
}


app.Run();


