using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    /// <summary>
    /// Handler to resolve natural language or vague date inputs
    /// (like "today", "next week", "20th July", etc.)
    /// into a standard "yyyy-MM-dd" format.
    /// </summary>
    public class ResolveNaturalLanguageDateToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveNaturalLanguageDateToolHandler> _logger;

        public ResolveNaturalLanguageDateToolHandler(ILogger<ResolveNaturalLanguageDateToolHandler> logger)
        {
            _logger = logger;
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

            // Normalize ordinal suffixes
            input = Regex.Replace(input, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);

            var formats = new[]
            {
                "d-M-yyyy", "d/M/yyyy", "M-d-yyyy", "M/d/yyyy",
                "dd-MM-yyyy", "MM-dd-yyyy",
                "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy",
                "MMMM d yyyy", "MMM d yyyy",
                "MMMM dd yyyy", "MMM dd yyyy",
                "d MMM", "dd MMM", "d MMMM", "dd MMMM",
                "MMM d", "MMMM d", "MMMM dd", "MMM dd",
                "d-M", "d/M", "M-d", "M/d"
            };

            var parseResult = await Task.Run(() =>
            {
                DateTime parsed = default!;
                bool success = false;

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    {
                        if (parsed.Year == 1)
                            parsed = new DateTime(DateTime.Today.Year, parsed.Month, parsed.Day);

                        success = true;
                        return (success, parsed);
                    }
                }

                if (!success && DateTime.TryParse(input, out parsed))
                {
                    success = true;
                }

                return (success, parsed);
            });

            if (!parseResult.success)
            {
                _logger.LogWarning("ResolveNaturalLanguageDate: Unable to parse date from input '{Input}'", input);
                return await Task.FromResult(CreateError(call.Id, $"Could not resolve date from input: '{input}'"));
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    resolvedDate = parseResult.parsed.ToString("yyyy-MM-dd")
                }
            };

            _logger.LogInformation("ResolveNaturalLanguageDate: Resolved '{Input}' to '{ResolvedDate}'", input, result.match.resolvedDate);
            return await Task.FromResult(new ToolOutput(call.Id, JsonSerializer.Serialize(result)));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
