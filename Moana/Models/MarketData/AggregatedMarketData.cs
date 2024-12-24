namespace Moana.Models.MarketData
{
    public class AggregatedMarketData
    {
        public string Symbol { get; set; }
        public VolumeData VolumeData { get; set; }
        public TrendData TrendData { get; set; }
        public SentimentData SentimentData { get; set; }
        public DerivativesData DerivativesData { get; set; }
        public IndicatorData IndicatorData { get; set; }
        public LiquidityData LiquidityData { get; set; }
        public List<EconomicEventData> EconomicEvents { get; set; }
        public List<CorrelationData> Correlations { get; set; }
    }
}
