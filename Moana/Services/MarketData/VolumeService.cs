using Moana.Models.MarketData;

namespace Moana.Services.MarketData
{
    public class VolumeService
    {
        private readonly BinanceService _binanceService; // Exemple pour Binance

        public VolumeService(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        public async Task<VolumeData> GetVolumeDataAsync(string symbol)
        {
            if (IsCryptoSymbol(symbol))
            {
                return await GetCryptoVolumeDataAsync(symbol);
            }
            else
            {
                //return await GetForexVolumeDataAsync(symbol);
                return null;
            }
        }

        public async Task<VolumeData> GetCryptoVolumeDataAsync(string symbol)
        {
            (decimal totalVolume, decimal buySellRatio, decimal volumeChange) = await _binanceService.GetVolumeAsync(symbol);

            return new VolumeData
            {
                Volume24h = totalVolume,
                BuySellRatio = buySellRatio,
                VolumeChangePercentage = volumeChange
            };
        }

        //private async Task<VolumeData> GetForexVolumeDataAsync(string symbol)
        //{
        //    // Appel à une API FOREX pour récupérer les données de volume
        //    var forexVolume = await _forexDataService.GetVolumeAsync(symbol);

        //    return new VolumeData
        //    {
        //        Volume24h = forexVolume.TotalVolume,
        //        BuySellRatio = forexVolume.BuySellRatio,
        //        VolumeChangePercentage = forexVolume.VolumeChange
        //    };
        //}

        private bool IsCryptoSymbol(string symbol)
        {
            // Exemple : les paires crypto se terminent souvent par "USDT", "BTC", etc.
            return symbol.EndsWith("USDT") || symbol.EndsWith("BTC");
        }
    }
}
