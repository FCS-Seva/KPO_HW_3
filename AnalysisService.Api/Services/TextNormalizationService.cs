using System.Text;
using System.Text.RegularExpressions;

namespace AnalysisService.Api.Services;

public class TextNormalizationService : ITextNormalizationService
{
    private static readonly Regex NonLetterDigit = new("[^\\p{L}\\p{Nd}]+", RegexOptions.Compiled);

    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var lower = text.ToLowerInvariant();
        var cleaned = NonLetterDigit.Replace(lower, " ");
        return cleaned.Trim();
    }
}