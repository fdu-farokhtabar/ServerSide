using ExcelDataReader;
using Application.Services.Helper;
using Domain.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Services.UpdateDataByExcel.Helper
{
    public static class UpdateByExcelHelper
    {
        public static DataTableCollection ReadExcel(Stream stream)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);
                DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });
                reader.Close();
                return result.Tables;
            }
            catch
            {
                return null;
            }
        }
        public static int? GetInt32(object Value)
        {
            if (Value is null)
                return null;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return null;
            try
            {
                return Convert.ToInt32(ValueString);
            }
            catch
            {
                return null;
            }

        }
        public static int GetInt32WithDefaultZero(object Value)
        {
            if (Value is null)
                return 0;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return 0;
            try
            {
                return Convert.ToInt32(ValueString);
            }
            catch
            {
                return 0;
            }

        }
        public static int GetInt32WhenMorethanZero(object Value, int Default)
        {
            if (Value is null)
                return Default;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return Default;
            try
            {
                int Result = Convert.ToInt32(ValueString);
                return Result > 0 ? Result : Default;
            }
            catch
            {
                return Default;
            }

        }
        public static bool GetBoolWithDefaultFalse(object Value)
        {
            if (Value is null)
                return false;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return false;
            try
            {
                var ValueNum = Convert.ToInt32(ValueString);
                if (ValueNum == 0)
                    return false;
                else
                    return true;
            }
            catch
            {

            }
            try
            {
                return Convert.ToBoolean(ValueString);
            }
            catch
            {
                return false;
            }

        }
        public static double? GetDouble(object Value)
        {
            if (Value is null)
                return null;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return null;
            try
            {
                return Math.Round(Convert.ToDouble(ValueString), 2, MidpointRounding.ToZero);
            }
            catch
            {
                return null;
            }

        }
        public static double GetDoubleWithdefaultValue(object Value, double Default)
        {
            if (Value is null)
                return Default;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return Default;
            try
            {
                return Math.Round(Convert.ToDouble(ValueString), 2, MidpointRounding.ToZero);
            }
            catch
            {
                return Default;
            }

        }
        public static decimal? GetDecimal(object Value)
        {
            if (Value is null)
                return null;
            string ValueString = Value.ToString();
            if (string.IsNullOrWhiteSpace(ValueString))
                return null;
            try
            {
                return Convert.ToDecimal(ValueString);
            }
            catch
            {
                return null;
            }
        }
        public static string GenerateSlug(string Name, object Slug, List<string> Slugs)
        {
            int SlugIndex = 1;
            string RealSlug = Slug is null || string.IsNullOrWhiteSpace(Slug.ToString()) ? Name.Slugify() : Slug.ToString().Slugify();
            while (Slugs.Find(x => x.Equals(RealSlug)) is not null)
            {
                RealSlug = $"{RealSlug}_{SlugIndex}";
                SlugIndex++;
            }
            return RealSlug;
        }
        public static string ConvertStringWithbracketsToJsonArrayString(string Value)
        {
            List<string> Result = new List<string>();
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var Matches = Regex.Matches(Value, @"(\[[^\[\]]*\])");
                if (Matches?.Count > 0)
                {
                    foreach (var Match in Matches)
                    {
                        string Expression = Match?.ToString();
                        if (!string.IsNullOrWhiteSpace(Expression))
                        {
                            Expression = Expression.Replace("[", "").Replace("]", "");
                            if (!string.IsNullOrWhiteSpace(Expression))
                                Result.Add(Expression);
                        }
                    }
                }
            }
            return Result.Count > 0 ? JsonSerializer.Serialize(Result.ToArray()) : null;
        }
        public static string ConvertStringWithbracketsToJsonArrayInt(string Value)
        {
            List<int> Result = new();
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var Matches = Regex.Matches(Value, @"(\[[^\[\]]*\])");
                if (Matches?.Count > 0)
                {
                    foreach (var Match in Matches)
                    {
                        string Expression = Match?.ToString();
                        if (!string.IsNullOrWhiteSpace(Expression))
                        {
                            Expression = Expression.Replace("[", "").Replace("]", "");
                            if (Int32.TryParse(Expression, out int Number))
                                Result.Add(Number);
                        }
                    }
                }
            }
            return Result.Count > 0 ? JsonSerializer.Serialize(Result.ToArray()) : null;
        }
        public static string CreateJsonKeyValue(DataColumnCollection Columns, DataRow Row, string StartColumnName)
        {
            List<KeyValue> DataList = new();
            foreach (var Column in Columns)
            {
                string ColumnName = Column.ToString();
                if (ColumnName.StartsWith(StartColumnName, StringComparison.OrdinalIgnoreCase))
                    DataList.Add(new KeyValue() { Name = $"{ColumnName.Replace(StartColumnName, "", StringComparison.OrdinalIgnoreCase).Trim()}", Value = Row[ColumnName]?.ToString().Trim() });
            }
            return System.Text.Json.JsonSerializer.Serialize(DataList);
        }

        public static string DateTimeToDateString(object value, string Format = "MM-dd-yyyy")
        {
            if (value is null || value is not DateTime)
                return null;
            try
            {
                return ((DateTime)value).ToString(Format);
            }
            catch
            {

            }
            return null;
        }
        public static DateTime? GetDateTime(object value)
        {
            if (value is null || value is not DateTime)
                return null;
            return (DateTime?)value;
        }

    }
}
