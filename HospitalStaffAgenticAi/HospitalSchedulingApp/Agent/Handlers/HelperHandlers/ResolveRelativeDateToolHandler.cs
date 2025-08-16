using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    /// <summary>
    /// Tool handler that resolves human-readable relative date phrases like 
    /// "today", "next week", "this weekend", etc. into machine-usable ISO date strings (yyyy-MM-dd).
    /// </summary>
    public class ResolveRelativeDateToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveRelativeDateToolHandler> _logger;

        public ResolveRelativeDateToolHandler(            
            ILogger<ResolveRelativeDateToolHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Name of the tool this handler serves.
        /// </summary>
        public string ToolName => ResolveRelativeDateTool.GetTool().Name;

        /// <summary>
        /// Handles incoming tool call and resolves date phrases to actual date(s).
        /// </summary>
        public Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // Ensure input contains "phrase"
                if (!root.TryGetProperty("phrase", out var phraseElement))
                {
                    _logger.LogWarning("Missing 'phrase' parameter in resolveRelativeDate tool call.");
                    return Task.FromResult<ToolOutput?>(null);
                }

                var phrase = phraseElement.GetString()?.ToLowerInvariant().Trim() ?? string.Empty;
                //var today = DateTime.UtcNow.Date;
                var today = DateTime.Now.Date;
                string resultJson;

                switch (phrase)
                {
                    case "today":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.ToString("yyyy-MM-dd") });
                        break;

                    case "tomorrow":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.AddDays(1).ToString("yyyy-MM-dd") });
                        break;

                    case "yesterday":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.AddDays(-1).ToString("yyyy-MM-dd") });
                        break;

                    case "this week":
                        var thisWeekEnd = today.AddDays(6);
                        resultJson = JsonSerializer.Serialize(new
                        {
                            startDate = today.ToString("yyyy-MM-dd"),
                            endDate = thisWeekEnd.ToString("yyyy-MM-dd")
                        });
                        break;



                    case "day after tomorrow":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.AddDays(2).ToString("yyyy-MM-dd") });
                        break;

                    case "day before yesterday":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.AddDays(-2).ToString("yyyy-MM-dd") });
                        break;

                    case "next week":
                        var nextWeekStart = today.AddDays(7);
                        var nextWeekEnd = today.AddDays(13);
                        resultJson = JsonSerializer.Serialize(new
                        {
                            startDate = nextWeekStart.ToString("yyyy-MM-dd"),
                            endDate = nextWeekEnd.ToString("yyyy-MM-dd")
                        });
                        break;

 
                    case "previous week":
                    case "last week":
                        var lastWeekStart = today.AddDays(-7);
                        var lastWeekEnd = today.AddDays(-1);
                        resultJson = JsonSerializer.Serialize(new
                        {
                            startDate = lastWeekStart.ToString("yyyy-MM-dd"),
                            endDate = lastWeekEnd.ToString("yyyy-MM-dd")
                        });
                        break;


                    case "next month":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.AddMonths(1).ToString("yyyy-MM-dd") });
                        break;

                    case "last month":
                    case "previous month":
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.AddMonths(-1).ToString("yyyy-MM-dd") });
                        break;

                    case "this weekend":
                        // Saturday of this week or upcoming
                        var nextSaturday = GetNextWeekday(today, DayOfWeek.Saturday);
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = nextSaturday.ToString("yyyy-MM-dd") });
                        break;

                    case "last weekend":
                        var lastSaturday = GetLastWeekdayBefore(today, DayOfWeek.Saturday);
                        var lastSunday = lastSaturday.AddDays(1);
                        resultJson = JsonSerializer.Serialize(new
                        {
                            startDate = lastSaturday.ToString("yyyy-MM-dd"),
                            endDate = lastSunday.ToString("yyyy-MM-dd")
                        });
                        break;

                    default:
                        // Handle "next Monday", "next Friday", etc.
                        if (phrase.StartsWith("next ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var dayPart = phrase.Substring(5).Trim();
                            if (Enum.TryParse<DayOfWeek>(dayPart, true, out var targetDay))
                            {
                                var nextDay = GetNextWeekday(today, targetDay);
                                resultJson = JsonSerializer.Serialize(new { resolvedDate = nextDay.ToString("yyyy-MM-dd") });
                                return Task.FromResult<ToolOutput?>(new ToolOutput(call.Id, resultJson));
                            }
                        }

                        // Fallback to today if unrecognized phrase
                        resultJson = JsonSerializer.Serialize(new { resolvedDate = today.ToString("yyyy-MM-dd") });
                        break;
                }

                return Task.FromResult<ToolOutput?>(new ToolOutput(call.Id, resultJson));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving relative date.");
                return Task.FromResult<ToolOutput?>(null);
            }
        }

        /// <summary>
        /// Gets the next date matching the target weekday from the given date.
        /// </summary>
        private static DateTime GetNextWeekday(DateTime from, DayOfWeek targetDay)
        {
            int daysToAdd = ((int)targetDay - (int)from.DayOfWeek + 7) % 7;
            return from.AddDays(daysToAdd == 0 ? 7 : daysToAdd); // Skip to *next* week's same day if today matches
        }

        /// <summary>
        /// Gets the previous date matching the target weekday before the given date.
        /// </summary>
        private static DateTime GetLastWeekdayBefore(DateTime from, DayOfWeek targetDay)
        {
            int daysBack = ((int)from.DayOfWeek - (int)targetDay + 7) % 7;
            return from.AddDays(daysBack == 0 ? -7 : -daysBack);
        }
    }
}
