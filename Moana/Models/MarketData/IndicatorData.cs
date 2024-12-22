namespace Moana.Models.MarketData
{
    // Indicateurs techniques
    public class IndicatorData
    {
        public decimal RSI { get; set; }  // Relative Strength Index
        public (decimal Line, decimal Signal) MACD { get; set; }  // MACD et Ligne Signal
        public (decimal Upper, decimal Middle, decimal Lower) BollingerBands { get; set; }  // Bandes de Bollinger
        public decimal ATR { get; set; }  // Average True Range
        public (decimal TenkanSen, decimal KijunSen, decimal SenkouSpanA, decimal SenkouSpanB) Ichimoku {  get; set; } // Ichimoku Cloud
        public (decimal PercentK, decimal PercentD) Stochastic { get; set; } // Stochastic Oscillator
        public decimal[] ParabolicSAR { get; set; }
        public decimal VWAP { get; set; }
        public decimal ADX { get; set; }
        public decimal CMF { get; set; }  // Chaikin Money Flow
        public decimal RVI { get; set; }  // Relative Vigor Index
        public decimal WilliamsR { get; set; }  // Williams %R
        public decimal ADL { get; set; }  // Accumulation/Distribution Line
        public decimal CMO { get; set; }  // Chande Momentum Oscillator
        public decimal OBV { get; set; }  // On-Balance Volume
    }
}
