using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    /// <summary>
    /// Tool handler that resolves human-readable relative date phrases like 
    /// "today", "next week", "this weekend", etc. into machine-usable ISO date strings (yyyy-MM-dd).
    /// </summary>
    public class ResolveRelativeDateToolHandler : BaseToolHandler
    {
        public ResolveRelativeDateToolHandler(ILogger<ResolveRelativeDateToolHandler> logger)
            : base(logger) { }

        public override string ToolName => ResolveRelativeDateTool.GetTool().Name;

        public override Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                string phrase = root.FetchString("phrase")?.ToLowerInvariant().Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(phrase))
                {
                    _logger.LogWarning("ResolveRelativeDate: Missing phrase parameter.");
                    return Task.FromResult(CreateError(call.Id, "❌ Date phrase is required."));
                }

                var today = DateTime.Now.Date;
                object result;

                switch (phrase)
                {
                    case "today":
                        result = new { resolvedDate = today.ToString("yyyy-MM-dd") };
                        break;

                    case "tomorrow":
                        result = new { resolvedDate = today.AddDays(1).ToString("yyyy-MM-dd") };
                        break;

                    case "yesterday":
                        result = new { resolvedDate = today.AddDays(-1).ToString("yyyy-MM-dd") };
                        break;

                    case "day after tomorrow":
                        result = new { resolvedDate = today.AddDays(2).ToString("yyyy-MM-dd") };
                        break;

                    case "day before yesterday":
                        result = new { resolvedDate = today.AddDays(-2).ToString("yyyy-MM-dd") };
                        break;

                    case "this week":
                        result = new
                        {
                            startDate = today.ToString("yyyy-MM-dd"),
                            endDate = today.AddDays(6).ToString("yyyy-MM-dd")
                        };
                        break;

                    case "next week":
                        result = new
                        {
                            startDate = today.AddDays(7).ToString("yyyy-MM-dd"),
                            endDate = today.AddDays(13).ToString("yyyy-MM-dd")
                        };
                        break;

                    case "last week":
                    case "previous week":
                        result = new
                        {
                            startDate = today.AddDays(-7).ToString("yyyy-MM-dd"),
                            endDate = today.AddDays(-1).ToString("yyyy-MM-dd")
                        };
                        break;

                    case "next month":
                        result = new { resolvedDate = today.AddMonths(1).ToString("yyyy-MM-dd") };
                        break;

                    case "last month":
                    case "previous month":
                        result = new { resolvedDate = today.AddMonths(-1).ToString("yyyy-MM-dd") };
                        break;

                    case "this weekend":
                        var nextSaturday = GetNextWeekday(today, DayOfWeek.Saturday);
                        result = new { resolvedDate = nextSaturday.ToString("yyyy-MM-dd") };
                        break;

                    case "last weekend":
                        var lastSaturday = GetLastWeekdayBefore(today, DayOfWeek.Saturday);
                        result = new
                        {
                            startDate = lastSaturday.ToString("yyyy-MM-dd"),
                            endDate = lastSaturday.AddDays(1).ToString("yyyy-MM-dd")
                        };
                        break;

                    default:
                        if (phrase.StartsWith("next ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var dayPart = phrase.Substring(5).Trim();
                            if (Enum.TryParse<DayOfWeek>(dayPart, true, out var targetDay))
                            {
                                var nextDay = GetNextWeekday(today, targetDay);
                                result = new { resolvedDate = nextDay.ToString("yyyy-MM-dd") };
                                return Task.FromResult(CreateSuccess(call.Id, "✅ Resolved relative date.", result));
                            }
                        }

                        _logger.LogWarning("ResolveRelativeDate: Unrecognized phrase '{Phrase}', defaulting to today.", phrase);
                        result = new { resolvedDate = today.ToString("yyyy-MM-dd") };
                        break;
                }

                return Task.FromResult(CreateSuccess(call.Id, "✅ Resolved relative date.", result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResolveRelativeDate: Error resolving relative date.");
                return Task.FromResult(CreateError(call.Id, "⚠️ Failed to resolve relative date."));
            }
        }

        private static DateTime GetNextWeekday(DateTime from, DayOfWeek targetDay)
        {
            int daysToAdd = ((int)targetDay - (int)from.DayOfWeek + 7) % 7;
            return from.AddDays(daysToAdd == 0 ? 7 : daysToAdd); // Skip to *next* week if today matches
        }

        private static DateTime GetLastWeekdayBefore(DateTime from, DayOfWeek targetDay)
        {
            int daysBack = ((int)from.DayOfWeek - (int)targetDay + 7) % 7;
            return from.AddDays(daysBack == 0 ? -7 : -daysBack);
        }
    }
}
