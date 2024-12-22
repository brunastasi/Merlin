using Binance.Net.Enums;
using Moana.Configurations;
using Moana.Services;
using Moana.Services.MarketData;

var builder = WebApplication.CreateBuilder(args);

var test = builder.Services.Configure<BinanceOptions>(builder.Configuration.GetSection("API").GetSection("Binance"));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("API").GetSection("OpenAI"));

// Add services to the container.
builder.Services.AddScoped<BinanceService>();
builder.Services.AddHttpClient<OpenAIService>();

builder.Services.AddScoped<VolumeService>();
builder.Services.AddScoped<TrendService>();
builder.Services.AddScoped<IndicatorsService>();
builder.Services.AddScoped<LiquidityService>();
builder.Services.AddScoped<DerivativesService>();

var serviceProvider = builder.Services.BuildServiceProvider();

// Récupérer BinanceService depuis le conteneur DI
var binanceService = serviceProvider.GetRequiredService<BinanceService>();
var volumeService = serviceProvider.GetRequiredService<VolumeService>();
var trendService = serviceProvider.GetRequiredService<TrendService>();
var indicatorsService = serviceProvider.GetRequiredService<IndicatorsService>();
var liquidityService = serviceProvider.GetRequiredService<LiquidityService>();
var derivativesService = serviceProvider.GetRequiredService<DerivativesService>();



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

var derivativesData = await derivativesService.GetDerivativesDataAsync("BTCUSDT");

Console.WriteLine($"Open Interest : {derivativesData.OpenInterest}");
Console.WriteLine($"Funding Rate : {derivativesData.FundingRate}");
Console.WriteLine($"Long/Short Ratio : {derivativesData.LongShortRatio}");
Console.WriteLine($"Futures Volume : {derivativesData.FuturesVolume}");
Console.WriteLine($"Last Updated : {derivativesData.LastUpdated}");

app.Run();


