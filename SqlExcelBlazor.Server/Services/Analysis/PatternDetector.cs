using System.Text.RegularExpressions;

namespace SqlExcelBlazor.Server.Services.Analysis;

/// <summary>
/// Service for detecting common patterns in data
/// </summary>
public class PatternDetector
{
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"^https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,9}$", RegexOptions.Compiled);
    private static readonly Regex PostalCodeRegex = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);
    private static readonly Regex UuidRegex = new(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
    private static readonly Regex IpAddressRegex = new(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled);

    /// <summary>
    /// Detect patterns in a collection of values
    /// </summary>
    public Dictionary<string, int> DetectPatterns(IEnumerable<string> values)
    {
        var patterns = new Dictionary<string, int>
        {
            ["Email"] = 0,
            ["URL"] = 0,
            ["Phone"] = 0,
            ["PostalCode"] = 0,
            ["UUID"] = 0,
            ["IPAddress"] = 0
        };

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var trimmedValue = value.Trim();

            if (EmailRegex.IsMatch(trimmedValue))
                patterns["Email"]++;
            else if (UrlRegex.IsMatch(trimmedValue))
                patterns["URL"]++;
            else if (UuidRegex.IsMatch(trimmedValue))
                patterns["UUID"]++;
            else if (IpAddressRegex.IsMatch(trimmedValue))
                patterns["IPAddress"]++;
            else if (PhoneRegex.IsMatch(trimmedValue))
                patterns["Phone"]++;
            else if (PostalCodeRegex.IsMatch(trimmedValue))
                patterns["PostalCode"]++;
        }

        return patterns;
    }

    /// <summary>
    /// Get list of detected pattern names (with at least one match)
    /// </summary>
    public List<string> GetDetectedPatternNames(Dictionary<string, int> patternCounts)
    {
        return patternCounts.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList();
    }
}
