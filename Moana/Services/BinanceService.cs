using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using Microsoft.Extensions.Options;
using Moana.Configurations;
using Moana.Models.ApiData;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Moana.Services
{
    public class BinanceService
    {
        private readonly HttpClient _httpClient;
        // Récupération de données ou passage d'ordre.
        private readonly BinanceRestClient _binanceClient;
        // Ecoute en temps réel des mises à jour (variation de prix..)
        private readonly BinanceSocketClient _socketClient;


        public BinanceService(HttpClient httpClient, IOptions<BinanceOptions> options)
        {
            var apiKey = options.Value.ApiKey;
            var apiSecret = options.Value.ApiSecret;

            ApiCredentials credentials = new ApiCredentials(apiKey, apiSecret);

            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", apiKey);

            // Utilisation correcte d'une Action pour configurer les options
            _binanceClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = credentials;
                options.AutoTimestamp = true;
                options.Environment = BinanceEnvironment.Testnet;
            });

            _socketClient = new BinanceSocketClient(options =>
            {
                options.ApiCredentials = credentials;
            });
        }

        #region OldMethodHTTPClient
        //      /// <summary>
        //      /// Récupère le prix actuel pour une paire de trading.
        //      /// </summary>
        //      /// <param name="symbol">Paire de trading, ex: BTCUSDT</param>
        //      /// <returns>Prix actuel</returns>
        //public async Task<decimal> GetPriceAsync(string symbol)
        //{
        //	string url = $"https://api.binance.com/api/v3/ticker/price?symbol={symbol}";

        //	var response = await _httpClient.GetAsync(url);
        //          if (!response.IsSuccessStatusCode)
        //          {
        //              throw new Exception($"Erreur lors de la récupération du prix: {response.ReasonPhrase}");
        //          }

        //          var content = await response.Content.ReadAsStringAsync();
        //          dynamic result = JsonConvert.DeserializeObject(content);

        //          return Convert.ToDecimal(result.price);
        //      }

        //      /// <summary>
        //      /// Récupère les informations de compte, y compris les soldes.
        //      /// </summary>
        //      /// <returns>Solde disponible pour chaque actif</returns>
        //      public async Task<dynamic> GetAccountInfoAsync()
        //      {
        //          string url = "https://api.binance.com/api/v3/account";

        //          // Ajouter l'authentification pour une requête signée
        //          var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        //          var signature = GenerateSignature($"timestamp={timestamp}");

        //          var requestUrl = $"{url}?timestamp={timestamp}&signature={signature}";
        //          var response = await _httpClient.GetAsync(requestUrl);

        //          if (!response.IsSuccessStatusCode)
        //          {
        //              throw new Exception($"Erreur lors de la récupération des informations de compte : {response.ReasonPhrase}");
        //          }

        //          var content = await response.Content.ReadAsStringAsync();
        //          return JsonConvert.DeserializeObject(content);
        //      }

        //      /// <summary>
        //      /// Place un ordre de marché (achat ou vente).
        //      /// </summary>
        //      /// <param name="symbol">Paire de trading</param>
        //      /// <param name="quantity">Quantité à trader</param>
        //      /// <param name="isBuy">True pour un achat, False pour une vente</param>
        //      /// <returns>Détails de l'ordre placé</returns>
        //      public async Task<dynamic> PlaceMarketOrderAsync(string symbol, decimal quantity, bool isBuy)
        //      {
        //          string url = "https://api.binance.com/api/v3/order";
        //          var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        //          var parameters = $"symbol={symbol}&side={(isBuy ? "BUY" : "SELL")}&type=MARKET&quantity={quantity}&timestamp={timestamp}";
        //          var signature = GenerateSignature(parameters);

        //          var requestUrl = $"{url}?{parameters}&signature={signature}";
        //          var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        //          var response = await _httpClient.SendAsync(request);
        //          if (!response.IsSuccessStatusCode)
        //          {
        //              throw new Exception($"Erreur lors du placement de l'ordre : {response.ReasonPhrase}");
        //          }

        //          var content = await response.Content.ReadAsStringAsync();
        //          return JsonConvert.DeserializeObject(content);
        //      }

        //      /// <summary>
        //      /// Génère une signature pour les requêtes sécurisées.
        //      /// </summary>
        //      /// <param name="data">Paramètres à signer</param>
        //      /// <returns>Signature HMAC SHA256</returns>
        //      private string GenerateSignature(string data)
        //      {
        //          using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_apiSecret));
        //          var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        //          return BitConverter.ToString(hash).Replace("-", "").ToLower();
        //      }
        #endregion

        #region Récupération
        /// <summary>
        /// Récupère les données de volume sur 24 heures pour une paire donnée.
        /// </summary>
        /// <param name="symbol">Le symbole de la paire (ex: BTCUSDT)</param>
        /// <returns>
        /// Un tuple contenant :
        /// - Le volume total échangé (TotalVolume)
        /// - Le ratio acheteurs/vendeurs (BuySellRatio)
        /// - La variation des volumes en pourcentage (VolumeChange)
        /// </returns>
        public async Task<(decimal TotalVolume, decimal BuySellRatio, decimal VolumeChange)> GetVolumeAsync(string symbol)
        {
            // Appel à l'API Binance pour récupérer les statistiques de 24 heures
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetTickerAsync(symbol);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des données Binance : {result.Error?.Message}");
            }

            var ticker = result.Data;

            // Calculer les métriques nécessaires
            var totalVolume = ticker.Volume;
            var buySellRatio = CalculateBuySellRatio(ticker);
            var volumeChange = CalculateVolumeChange(ticker);

            return (totalVolume, buySellRatio, volumeChange);
        }

        public async Task<decimal[]> GetVolumesAsync(string symbol, KlineInterval interval)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des volumes : {result.Error?.Message}");
            }

            return result.Data.Select(k => k.Volume).ToArray();
        }


        /// <summary>
        /// Calcule le ratio acheteurs/vendeurs à partir des données du ticker.
        /// </summary>
        /// <param name="ticker">Les données du ticker sur 24 heures.</param>
        /// <returns>Le ratio acheteurs/vendeurs.</returns>
        private decimal CalculateBuySellRatio(IBinance24HPrice ticker)
        {
            // Exemple simple : calcul basé sur des champs disponibles (si applicable)
            return ticker.QuoteVolume != 0 ? ticker.Volume / ticker.QuoteVolume : 0;
        }

        /// <summary>
        /// Calcule la variation du volume en pourcentage par rapport au prix d'ouverture.
        /// </summary>
        /// <param name="ticker">Les données du ticker sur 24 heures.</param>
        /// <returns>La variation du volume en pourcentage.</returns>
        private decimal CalculateVolumeChange(IBinance24HPrice ticker)
        {
            // Exemple : variation basée sur le prix d'ouverture et de clôture
            return ticker.OpenPrice != 0 ? (ticker.LastPrice - ticker.OpenPrice) / ticker.OpenPrice * 100 : 0;
        }

        public async Task<decimal[]> GetHistoricalPricesAsync(string symbol, KlineInterval interval)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des données historiques Binance : {result.Error?.Message}");
            }

            // Extraire uniquement les prix de clôture
            return result.Data.Select(k => k.ClosePrice).ToArray();
        }

        public async Task<decimal[]> GetHighPricesAsync(string symbol, KlineInterval interval)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des hauts : {result.Error?.Message}");
            }

            return result.Data.Select(k => k.HighPrice).ToArray();
        }

        public async Task<decimal[]> GetLowPricesAsync(string symbol, KlineInterval interval)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des bas : {result.Error?.Message}");
            }

            return result.Data.Select(k => k.LowPrice).ToArray();
        }

        public async Task<decimal[]> GetOpenPricesAsync(string symbol, KlineInterval interval)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des ouvertures : {result.Error?.Message}");
            }

            return result.Data.Select(k => k.OpenPrice).ToArray(); // Extraire les prix d'ouverture
        }

        public async Task<decimal[]> GetClosePricesAsync(string symbol, KlineInterval interval)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération des clôtures : {result.Error?.Message}");
            }

            return result.Data.Select(k => k.ClosePrice).ToArray();
        }

        public async Task<BinanceOrderBook> GetOrderBookAsync(string symbol, int limit)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetOrderBookAsync(symbol, limit);

            if (!result.Success || result.Data == null)
            {
                throw new Exception($"Erreur lors de la récupération du carnet d'ordres : {result.Error?.Message}");
            }

            return result.Data;
        }

        public async Task<decimal> GetOpenInterestAsync(string symbol)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetOpenInterestAsync(symbol);
            return result.Success ? result.Data.OpenInterest : 0m;
        }

        public async Task<decimal> GetFundingRateAsync(string symbol)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetFundingRatesAsync(symbol);
            return result.Success && result.Data.Any() ? result.Data.First().FundingRate : 0m;
        }

        /// <summary>
        /// Récupère les données du ratio long/short depuis Binance.
        /// </summary>
        /// <param name="symbol">Symbole de la paire (ex. : BTCUSDT).</param>
        /// <returns>Les données du ratio long/short.</returns>
        public async Task<List<LongShortRatioApiResponse>> GetLongShortRatioAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Le symbole ne peut pas être vide", nameof(symbol));

            // Construire l'URL de l'endpoint
            string url = $"https://fapi.binance.com/futures/data/globalLongShortAccountRatio?symbol={symbol}&period=5m";

            // Envoyer la requête GET
            var response = await _httpClient.GetFromJsonAsync<List<LongShortRatioApiResponse>>(url);

            // Vérifier si la réponse est valide
            if (response == null || !response.Any())
                throw new Exception("Erreur lors de la récupération des données de ratio long/short.");

            return response;
        }

        public async Task<decimal> GetFuturesVolumeAsync(string symbol, KlineInterval interval, int limit = 100)
        {
            // Récupérer les Klines pour les futures
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

            if (result.Success && result.Data.Any())
            {
                // Additionner les volumes des dernières Klines
                return result.Data.Sum(k => k.Volume); // BaseVolume correspond au volume échangé
            }

            return 0m; // Retourne 0 en cas d'erreur
        }



        /// <summary>
        /// Obtient le prix actuel pour une paire de trading.
        /// </summary>
        /// <param name="symbol">Paire de trading (ex: BTCUSDT)</param>
        /// <returns>Prix actuel</returns>
        public async Task<WebCallResult<BinancePrice>> GetPriceAsync(string symbol)
        {
            var result = await _binanceClient.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol);
            if (!result.Success)
            {
                throw new Exception($"Erreur lors de la récupération du prix : {result.Error?.Message}");
            }

            return result;
        }

        /// <summary>
        /// Récupère les informations de compte, y compris les soldes disponibles.
        /// </summary>
        /// <returns>Solde disponible pour chaque actif</returns>
        public async Task<BinanceFuturesAccountInfoV3> GetAccountInfoAsync()
        {
            var result = await _binanceClient.UsdFuturesApi.Account.GetAccountInfoV3Async();
            if (!result.Success)
            {
                Console.WriteLine($"Erreur lors de la récupération des informations de compte Futures : {result.Error?.Message}");
                return null;
            }

            return result.Data;
        }
        #endregion

        #region Actions
        public async Task<BinanceFuturesOrder> PlaceFuturesOrderAsync(string symbol, decimal quantity, bool isBuy, decimal? price = null)
        {
            var side = isBuy ? OrderSide.Buy : OrderSide.Sell;

            // Type d'ordre : Market ou Limit
            var orderType = price.HasValue ? FuturesOrderType.Limit : FuturesOrderType.Market;

            var result = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol,
                side,
                orderType,
                quantity,
                price: price,
                timeInForce: price.HasValue ? TimeInForce.GoodTillCanceled : null
            );

            if (!result.Success)
            {
                throw new Exception($"Erreur lors du placement de l'ordre Futures : {result.Error?.Message}");
            }

            return result.Data;
        }


        public async Task<IEnumerable<BinanceFuturesOrder>> GetOpenFuturesOrdersAsync(string symbol)
        {
            var result = await _binanceClient.UsdFuturesApi.Trading.GetOpenOrdersAsync(symbol);

            if (!result.Success)
            {
                throw new Exception($"Erreur lors de la récupération des ordres ouverts : {result.Error?.Message}");
            }

            return result.Data;
        }

        public async Task<BinanceFuturesOrder> PlaceAdvancedOrderAsync(
            string symbol,
            OrderSide side,
            FuturesOrderType type,
            decimal quantity,
            decimal? price = null,
            decimal? stopPrice = null,
            decimal? activationPrice = null,
            TimeInForce timeInForce = TimeInForce.GoodTillCanceled)
        {
            try
            {
                // Placement de l'ordre
                var result = await _binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: symbol,
                    side: side,
                    type: type,
                    quantity: quantity,
                    price: price,
                    stopPrice: stopPrice,
                    activationPrice: activationPrice,
                    timeInForce: type == FuturesOrderType.Market ? null : timeInForce);

                // Log des résultats
                if (result.Success)
                {
                    Console.WriteLine($"Ordre {type} placé avec succès : {JsonConvert.SerializeObject(result.Data, Formatting.Indented)}");
                    return result.Data;
                }
                else
                {
                    Console.WriteLine($"Erreur lors du placement de l'ordre {type}. Message API : {result.Error?.Message}. Code API : {result.Error?.Code}. Détails : {JsonConvert.SerializeObject(result.Error)}");
                    throw new Exception($"Erreur lors du placement de l'ordre {type} : {result.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la tentative de placement d'un ordre avancé.", ex);
                throw;
            }
        }




        public async Task<IEnumerable<BinanceFuturesOrder>> GetOrderHistoryAsync(string symbol, int limit = 50)
        {
            var result = await _binanceClient.UsdFuturesApi.Trading.GetOrdersAsync(symbol: symbol, limit: limit);

            if (!result.Success)
            {
                Console.WriteLine($"Erreur lors de la récupération des ordres : {result.Error?.Message}");
                throw new Exception($"Erreur lors de la récupération des ordres : {result.Error?.Message}");
            }

            return result.Data;
        }

        public async Task<int> GetAssetPrecisionAsync(string symbol)
        {
            var exchangeInfo = await _binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            var symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);

            if (symbolInfo == null)
                throw new Exception($"Symbole {symbol} non trouvé dans l'info d'échange.");

            return symbolInfo.QuantityPrecision; // Retourne la précision pour la quantité
        }

        public async Task<decimal> GetTickSizeAsync(string symbol)
        {
            var exchangeInfo = await _binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            var symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);
            if (symbolInfo == null)
            {
                throw new Exception($"Le symbole {symbol} n'est pas pris en charge.");
            }

            return symbolInfo.Filters
                .OfType<BinanceSymbolPriceFilter>()
                .First().TickSize;
        }


        public async Task<decimal> GetMinQuantityAsync(string symbol)
        {
            var exchangeInfo = await _binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            var symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);

            if (symbolInfo == null)
                throw new Exception($"Impossible d'obtenir la quantité minimale pour {symbol}");

            return symbolInfo.Filters.OfType<BinanceSymbolMarketLotSizeFilter>().FirstOrDefault()?.MinQuantity ?? 0;
        }


        public async Task<decimal> GetMinNotionalAsync(string symbol)
        {
            var exchangeInfo = await _binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            var symbolInfo = exchangeInfo.Data.Symbols.FirstOrDefault(s => s.Name == symbol);

            if (symbolInfo == null)
                throw new Exception($"Symbole {symbol} non trouvé dans l'info d'échange.");

            return symbolInfo.Filters.OfType<BinanceSymbolMinNotionalFilter>().First().MinNotional;
        }

        public async Task SetLeverageAsync(string symbol, int leverage)
        {
            var result = await _binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);

            if (!result.Success)
            {
                throw new Exception($"Erreur lors de la configuration du levier : {result.Error?.Message}");
            }

            Console.WriteLine($"Levier défini sur {leverage}x pour le symbole {symbol}.");
        }

        public async Task SetMarginModeAsync(string symbol, FuturesMarginType marginType)
        {
            var result = await _binanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, marginType);

            if (!result.Success)
            {
                throw new Exception($"Erreur lors de la configuration du mode de marge : {result.Error?.Message}");
            }

            Console.WriteLine($"Mode de marge défini sur {marginType} pour le symbole {symbol}.");
        }

        #endregion

        public async Task LogOrderDetails(BinanceFuturesOrder order)
        {
            Console.WriteLine($"Détails de l'ordre :");
            Console.WriteLine($"- ID de l'ordre : {order.Id}");
            Console.WriteLine($"- Symbole : {order.Symbol}");
            Console.WriteLine($"- Quantité : {order.Quantity}");
            Console.WriteLine($"- Statut : {order.Status}");
            Console.WriteLine($"- Prix exécuté : {order.AveragePrice}");
        }


        /// <summary>
        /// Écoute les mises à jour de prix via WebSocket.
        /// </summary>
        /// <param name="symbol">Paire de trading (ex: BTCUSDT)</param>
        /// <param name="onPriceUpdate">Action à exécuter lors des mises à jour de prix</param>
        /// <returns>Abonnement WebSocket</returns>
        public async Task<UpdateSubscription> SubscribeToPriceUpdatesAsync(string symbol, Action<IBinanceTick> onPriceUpdate)
        {
            var result = await _socketClient.SpotApi.ExchangeData.SubscribeToTickerUpdatesAsync(symbol, data =>
            {
                onPriceUpdate(data.Data);
            });

            if (!result.Success)
            {
                throw new Exception($"Erreur lors de l'abonnement au WebSocket : {result.Error?.Message}");
            }

            return result.Data;
        }

        /// <summary>
        /// Déconnecte tous les WebSockets.
        /// </summary>
        public void DisconnectWebSocket()
        {
            _socketClient.UnsubscribeAllAsync();
        }
    }
}
