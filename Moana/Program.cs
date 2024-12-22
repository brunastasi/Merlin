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

var serviceProvider = builder.Services.BuildServiceProvider();

// Récupérer BinanceService depuis le conteneur DI
var binanceService = serviceProvider.GetRequiredService<BinanceService>();
var volumeService = serviceProvider.GetRequiredService<VolumeService>();
var trendService = serviceProvider.GetRequiredService<TrendService>();
var indicatorsService = serviceProvider.GetRequiredService<IndicatorsService>();



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
var symbol = "BTCUSDT";
var interval = KlineInterval.OneHour;
var indicators = await indicatorsService.GetIndicatorsAsync(symbol, interval);

Console.WriteLine($"RSI : {indicators.RSI}");
Console.WriteLine($"MACD Line : {indicators.MACD.Line}");
Console.WriteLine($"MACD Signal : {indicators.MACD.Signal}");
Console.WriteLine($"Bollinger Bands : Upper = {indicators.BollingerBands.Upper}, Middle = {indicators.BollingerBands.Middle}, Lower = {indicators.BollingerBands.Lower}");
Console.WriteLine($"ATR : {indicators.ATR}");
Console.WriteLine($"Ichimoku : TenkanSen = {indicators.Ichimoku.TenkanSen}, KijunSen = {indicators.Ichimoku.KijunSen}, SenkouSpanA = {indicators.Ichimoku.SenkouSpanA}, SenkouSpanB = {indicators.Ichimoku.SenkouSpanB}");
Console.WriteLine($"Stochastic : %K = {indicators.Stochastic.PercentK}, %D = {indicators.Stochastic.PercentD}");
Console.WriteLine($"VWAP : {indicators.VWAP}");
Console.WriteLine($"Parabolic SAR : {string.Join(", ", indicators.ParabolicSAR)}");
Console.WriteLine($"ADX : {indicators.ADX}");

app.Run();


