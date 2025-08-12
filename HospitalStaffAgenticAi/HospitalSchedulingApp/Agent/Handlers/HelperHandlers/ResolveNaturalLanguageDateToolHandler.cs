using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    /// <summary>
    /// Handler to resolve natural language date(s) into a standardized date range (yyyy-MM-dd).
    /// Supports single date or a range in any order, with or without years.
    /// </summary>
    public class ResolveNaturalLanguageDateToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveNaturalLanguageDateToolHandler> _logger;
        private readonly IEntityResolver _entityResolver;

        public ResolveNaturalLanguageDateToolHandler(ILogger<ResolveNaturalLanguageDateToolHandler> logger,
            IEntityResolver entityResolver)
        {
            _logger = logger;
            _entityResolver = entityResolver;
        }

        public string ToolName => ResolveNaturalLanguageDateTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("naturalDate", out var dateProp)
                ? dateProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveNaturalLanguageDate: Date input is missing.");
                return await Task.FromResult(CreateError(call.Id, "Date input is required."));
            }

            _logger.LogInformation("ResolveNaturalLanguageDate: Received input '{Input}'", input);

            // Normalize ordinal suffixes (14th -> 14)
            input = Regex.Replace(input, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);

            // Split possible date parts by range indicators
            string[] parts = Regex.Split(input, @"\s*(?:to|-|and|,)\s*", RegexOptions.IgnoreCase);

            // Date formats to try
            string[] formats = {
                "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy",
                "MMM d yyyy", "MMMM d yyyy", "MMM dd yyyy", "MMMM dd yyyy",
                "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy",
                "yyyy-MM-dd",
                "d MMM", "dd MMM", "d MMMM", "dd MMMM",
                "MMM d", "MMMM d", "MMM dd", "MMMM dd"
            };

            var parsedDates = new List<DateTime>();
            int currentYear = DateTime.UtcNow.Year;

            foreach (var part in parts)
            {
                string candidate = part.Trim();
                if (string.IsNullOrEmpty(candidate)) continue;

                // Try exact formats first
                if (DateTime.TryParseExact(candidate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                {
                    // If year missing, assume current or next year
                    if (dt.Year == 1)
                    {
                        dt = new DateTime(currentYear, dt.Month, dt.Day);
                        if (dt < DateTime.UtcNow.Date)
                            dt = dt.AddYears(1);
                    }
                    parsedDates.Add(dt);
                }
                else if (DateTime.TryParse(candidate, out dt))
                {
                    // If year missing in TryParse
                    if (dt.Year == 1)
                    {
                        dt = new DateTime(currentYear, dt.Month, dt.Day);
                        if (dt < DateTime.UtcNow.Date)
                            dt = dt.AddYears(1);
                    }
                    parsedDates.Add(dt);
                }
            }

            // If no matches found, try whole string
            if (!parsedDates.Any() && DateTime.TryParse(input, out DateTime singleParsed))
            {
                if (singleParsed.Year == 1)
                {
                    singleParsed = new DateTime(currentYear, singleParsed.Month, singleParsed.Day);
                    if (singleParsed < DateTime.UtcNow.Date)
                        singleParsed = singleParsed.AddYears(1);
                }
                parsedDates.Add(singleParsed);
            }

            if (!parsedDates.Any())
            {
                _logger.LogWarning("ResolveNaturalLanguageDate: Unable to parse date(s) from input '{Input}'", input);
                return await Task.FromResult(CreateError(call.Id, $"Could not resolve date(s) from input: '{input}'"));
            }

            // Sort dates so start <= end
            parsedDates = parsedDates.OrderBy(d => d).ToList();
            DateTime startDate = parsedDates.First();
            DateTime endDate = parsedDates.Last();
            var resolveResult = await _entityResolver.ResolveEntitiesAsync(input);
            var result = new
            {
                success = true,
                input,
                startDate = resolveResult.DateRange.StartDate.ToString("yyyy-MM-dd"),
                endDate = resolveResult.DateRange.EndDate.ToString("yyyy-MM-dd")
            };

            _logger.LogInformation("ResolveNaturalLanguageDate: Resolved range '{Start}' to '{End}'", result.startDate, result.endDate);
            return await Task.FromResult(new ToolOutput(call.Id, JsonSerializer.Serialize(result)));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
