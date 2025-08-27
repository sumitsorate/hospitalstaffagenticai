using Microsoft.AspNetCore.Authentication;
using System;
using System.Text.Json;

namespace HospitalSchedulingApp.Common.Extensions
{
    public static class JsonElementExtensions
    {
        public static int? FetchInt(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val))
                    return val;
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var valStr))
                    return valStr;
            }
            return null;
        }

        public static string? FetchString(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetRawText();
                if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
                    return prop.GetBoolean().ToString();
            }
            return null;
        }

        public static DateTime? FetchDateTime(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String && DateTime.TryParse(prop.GetString(), out var dt))
                    return dt;
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var ticks))
                {
                    try
                    {
                        return new DateTime(ticks);
                    }
                    catch { }
                }
            }
            return null;
        }

        public static bool? FetchBool(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
                    return prop.GetBoolean();
                if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out var b))
                    return b;
            }
            return null;
        }

        public static double? FetchDouble(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var val))
                    return val;
                if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var valStr))
                    return valStr;
            }
            return null;
        }

        public static Guid? FetchGuid(this JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out var guid))
                    return guid;
            }
            return null;
        }

        public static DateTime? FetchDateTimeOrString(this JsonElement root, string propertyName)
        {
            var dt = root.FetchDateTime(propertyName);
            if (dt != null)
                return dt;

            var str = root.GetString(propertyName);
            if (DateTime.TryParse(str, out var parsed))
                return parsed;

            return null;
        }

    }
}
