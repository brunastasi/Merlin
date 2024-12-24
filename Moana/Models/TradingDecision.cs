namespace Moana.Models
{
    public class TradingDecision
    {
        public string Action { get; set; } // BUY, SELL, HOLD
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public string Confidence { get; set; } // High, Medium, Low
    }
}
