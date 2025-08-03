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

        /// <summary>
        /// Initializes a new instance of the <see cref="ResolveNaturalLanguageDateToolHandler"/> class.
        /// </summary>
        public ResolveNaturalLanguageDateToolHandler(ILogger<ResolveNaturalLanguageDateToolHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the tool as declared in the tool definition.
        /// </summary>
        public string ToolName => ResolveNaturalLanguageDateTool.GetTool().Name;

        /// <summary>
        /// Handles the tool call, resolving a natural language date input into a structured date.
        /// </summary>
        /// <param name="call">The function tool call information.</param>
        /// <param name="root">The input parameters as JSON.</param>
        /// <returns>A resolved date output or an error if resolution failed.</returns>
        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("naturalDate", out var dateProp)
                ? dateProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("⚠️ ResolveNaturalLanguageDate: Date input is missing.");
                return CreateError(call.Id, "❌ Date input is required.");
            }

            _logger.LogInformation("📥 ResolveNaturalLanguageDate: Received input '{Input}'", input);

            // ✅ Normalize ordinal suffixes: e.g., "21st July" → "21 July"
            input = Regex.Replace(input, @"\b(\d{1,2})(st|nd|rd|th)\b", "$1", RegexOptions.IgnoreCase);

            // 🔍 Supported custom date formats for parsing
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

            DateTime parsed = default!;
            bool success = false;

            // 📅 Try parsing with each format
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    // 🛠️ Default year handling if only day and month are provided
                    if (parsed.Year == 1)
                        parsed = parsed.AddYears(DateTime.Today.Year - 1);

                    success = true;
                    break;
                }
            }

            // 🧠 Fallback to general parsing (e.g., "next Monday", "tomorrow")
            if (!success && DateTime.TryParse(input, out parsed))
            {
                success = true;
            }

            // ❌ Parsing failed
            if (!success)
            {
                _logger.LogWarning("❗ ResolveNaturalLanguageDate: Unable to parse date from input '{Input}'", input);
                return CreateError(call.Id, $"❌ Could not resolve date from input: '{input}'");
            }

            // ✅ Return the parsed date
            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    resolvedDate = parsed.ToString("yyyy-MM-dd")
                }
            };

            _logger.LogInformation("✅ ResolveNaturalLanguageDate: Resolved '{Input}' to '{ResolvedDate}'", input, result.match.resolvedDate);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        /// <summary>
        /// Creates a standardized error result for the tool.
        /// </summary>
        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
