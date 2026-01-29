namespace SqlExcelBlazor.Server.Services.Analysis;

/// <summary>
/// Service for calculating statistics on data
/// </summary>
public class StatisticsCalculator
{
    /// <summary>
    /// Calculate median value from a list of numbers
    /// </summary>
    public decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;

        if (count % 2 == 0)
        {
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        }
        else
        {
            return sorted[count / 2];
        }
    }

    /// <summary>
    /// Calculate standard deviation
    /// </summary>
    public decimal CalculateStdDeviation(List<decimal> values, decimal average)
    {
        if (values.Count <= 1) return 0;

        var sumOfSquares = values.Sum(v => Math.Pow((double)(v - average), 2));
        return (decimal)Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    /// <summary>
    /// Calculate quartiles (Q1, Q2/Median, Q3)
    /// </summary>
    public (decimal Q1, decimal Q2, decimal Q3) CalculateQuartiles(List<decimal> values)
    {
        if (values.Count == 0) return (0, 0, 0);

        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;

        decimal q2 = CalculateMedian(values);
        
        var lowerHalf = sorted.Take(count / 2).ToList();
        var upperHalf = sorted.Skip((count + 1) / 2).ToList();

        decimal q1 = lowerHalf.Count > 0 ? CalculateMedian(lowerHalf) : sorted.First();
        decimal q3 = upperHalf.Count > 0 ? CalculateMedian(upperHalf) : sorted.Last();

        return (q1, q2, q3);
    }

    /// <summary>
    /// Detect outliers using IQR method
    /// </summary>
    public List<decimal> DetectOutliers(List<decimal> values)
    {
        if (values.Count < 4) return new List<decimal>();

        var (q1, q2, q3) = CalculateQuartiles(values);
        var iqr = q3 - q1;
        var lowerBound = q1 - (1.5m * iqr);
        var upperBound = q3 + (1.5m * iqr);

        return values.Where(v => v < lowerBound || v > upperBound).ToList();
    }
}
