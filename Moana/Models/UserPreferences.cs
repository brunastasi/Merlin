namespace Moana.Models
{
    public class UserPreferences
    {
        public decimal Budget { get; set; } // Budget total alloué
        public RiskManagement RiskManagement { get; set; }
    }

    public class RiskManagement
    {
        public decimal StopLoss { get; set; }          // Stop-loss en pourcentage
        public decimal TakeProfit { get; set; }        // Take-profit en pourcentage
        public decimal RiskRewardRatio { get; set; }   // Ratio risque/rendement
    }
}
