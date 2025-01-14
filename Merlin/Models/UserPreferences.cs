namespace Merlin.Models
{
    public class UserPreferences
    {
        public decimal Budget { get; set; } // Budget total alloué

        public int Leverage { get; set; }
        public RiskManagement RiskManagement { get; set; }
        public List<(string Asset, string Type)> Assets { get; set; } // Liste des actifs avec leur type
        public string Symbol { get; set; } // Symbole principal pour le trading
        public bool UseAIAnalysis { get; set; } // Indique si l'analyse via l'IA doit être utilisée
    }

    public class RiskManagement
    {
        public decimal StopLoss { get; set; }          // Stop-loss en pourcentage
        public decimal TakeProfit { get; set; }        // Take-profit en pourcentage
    }
}
