namespace Merlin.Models
{
    public class TradingDecision
    {
        public string Action { get; set; } // BUY, SELL, HOLD
        public decimal SL { get; set; }
        public decimal TP { get; set; }
        public string Confidence { get; set; } // High, Medium, Low
    }
}
