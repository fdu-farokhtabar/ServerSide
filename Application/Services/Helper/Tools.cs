using System;
using System.Text.RegularExpressions;

namespace Application.Services.Helper
{
    public static class Tools
    {
        public static string HashData(string Data)
        {
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(Data);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
        //public static List<string> SecurityToList(string Security) => string.IsNullOrWhiteSpace(Security) ? null : Security.Split(",").ToList();

        public static bool EmailIsValid(string email)
        {
            Regex regex = new(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            return regex.IsMatch(email);
        }
        public static bool PhoneIsValid(string phone)
        {
            Regex regex = new(@"^[0-9]{3}[- ][0-9]{3}[- ][0-9]{4}$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            return regex.IsMatch(phone);
        }

        public static string GetPriceFormat(decimal? Value)
        {
            if (Value.HasValue)
                return "$" + Decimal.Ceiling(Value.Value).ToString("G29");
            else
                return "";
            //if (Value.HasValue)
            //    return Value.Value.ToString("C");
            //else
            //    return "";
        }
    }
}
