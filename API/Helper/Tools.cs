using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace API.Helper
{
    public static class Tools
    {
        public static DateTime? DateStringToDateTime(string dateTime)
        {
            DateTime? result = null;
            if (!string.IsNullOrWhiteSpace(dateTime))
                return DateTime.Parse(dateTime);
            return result;
        }
        public static string DateTimeToDateString(DateTime? value, string Format = "MM-dd-yyyy")
        {
            if (value is null)
                return "";
            try
            {
                return ((DateTime)value).ToString(Format);
            }
            catch
            {

            }
            return "";
        }
        public static string DateTimeToDateTimeString(DateTime? value, string Format = "MM-dd-yyyy")
        {
            if (value is null)
                return "";
            try
            {
                return value.Value.ToString(Format) + " " + value.Value.Hour.ToString("00") + ":" + value.Value.Minute.ToString("00") + ":" + value.Value.Second.ToString("00");
            }
            catch
            {

            }
            return "";
        }
        public static string NullStringToEmpty(string Value) => Value is null ? string.Empty : Value;
        public static List<string> GetRoles(ServerCallContext context)
        {
            List<string> Roles = new();
            var RoleClaimValue = context.GetHttpContext()?.User?.Claims?.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
            if (RoleClaimValue is not null)
            {
                Roles = RoleClaimValue;
            }
            return Roles;
        }
    }
}
