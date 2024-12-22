namespace Moana.Models
{
    // Signaux de trading
    public class TradeSignal
    {
        public string Signal { get; set; }  // BUY, SELL, HOLD
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
