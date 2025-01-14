namespace Merlin.Services.Utils
{
    public class CorrelationCalculations
    {
        public static decimal CalculatePearsonCorrelation(List<decimal> series1, List<decimal> series2)
        {
            if (series1.Count != series2.Count || series1.Count == 0)
                throw new ArgumentException("Les séries doivent avoir la même taille et ne pas être vides.");

            decimal avg1 = series1.Average();
            decimal avg2 = series2.Average();

            decimal numerator = series1.Zip(series2, (x, y) => (x - avg1) * (y - avg2)).Sum();
            decimal denominator = (decimal)Math.Sqrt(series1.Sum(x => Math.Pow((double)(x - avg1), 2)) * series2.Sum(y => Math.Pow((double)(y - avg2), 2)));

            return denominator == 0 ? 0 : numerator / denominator;
        }
    }
}
