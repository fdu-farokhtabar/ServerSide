using Application.Data;
using Application.Services.UpdateDataByExcel.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Application.Services.UpdateDataByExcel
{
    using Domain.Entity;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class UpdateCategoryByExcelService
    {
        public async Task Update(Stream stream)
        {
            if (stream is null)
                throw new Exception("Please upload correct excel file.");
            List<Category> Categories = new();
            try
            {
                var Tables = UpdateByExcelHelper.ReadExcel(stream);
                if (Tables?.Count > 0 && Tables[0].Rows?.Count > 0)
                {
                    for (int i = 0; i < Tables[0].Rows.Count; i++)
                    {
                        var Row = Tables[0].Rows[i];
                        Category NewCategory = new()
                        {
                            Id = Guid.NewGuid(),
                            Name = Row["Name"].ToString().Trim(),
                            Slug = UpdateByExcelHelper.GenerateSlug(Row["Name"].ToString(), Row["Slug"], Categories.Select(x => x.Slug).ToList()),
                            Description = Row["Description"].ToString().Trim(),
                            ShortDescription = Row["Short description"].ToString().Trim(),
                            Parameter = CreateJsonParameters(Tables[0].Columns, Row),
                            ParentsString = Row["Parents"].ToString().Trim(),
                            Order = UpdateByExcelHelper.GetInt32WithDefaultZero(Row["Position"]),
                            PublishedCatalogType = (PublishedCatalogType)UpdateByExcelHelper.GetInt32WithDefaultZero(Row["PublishedCatalogType"]),
                            Tags = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Tags"].ToString().Trim()),
                            Security = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Security"].ToString().Trim())
                        };
                        Categories.Add(NewCategory);
                    }
                    for (int i = 0; i < Categories.Count; i++)
                    {
                        var Category = Categories[i];
                        if (!string.IsNullOrWhiteSpace(Category.ParentsString))
                        {
                            var Matches = Regex.Matches(Category.ParentsString, @"(\[[^\[\]]*\])");
                            if (Matches?.Count > 0)
                            {
                                foreach (var Match in Matches)
                                {
                                    string CategoryData = Match?.ToString();
                                    if (!string.IsNullOrWhiteSpace(CategoryData))
                                    {
                                        var CatNameOrder = CategoryData.Replace("[", "").Replace("]", "").Split(",");
                                        if (CatNameOrder?.Length == 2)
                                        {
                                            if (!string.IsNullOrWhiteSpace(CatNameOrder[0]))
                                            {
                                                var ParentCategory = Categories.FirstOrDefault(x => string.Equals(x.Name, CatNameOrder[0].Trim(), StringComparison.InvariantCultureIgnoreCase));
                                                int Order = 0;
                                                try
                                                {
                                                    if (!string.IsNullOrWhiteSpace(CatNameOrder[1]))
                                                        Order = Convert.ToInt32(CatNameOrder[1]);
                                                }
                                                catch
                                                {

                                                }
                                                Category.Parents ??= new List<CategoryCategory>();
                                                Category.Parents.Add(new CategoryCategory() { CategoryId = Category.Id, CategorySlug = Category.Slug, ParentCategoryId = ParentCategory.Id, ParentCategorySlug = ParentCategory.Slug, Order = Order });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("there are some errors during reading data from excel.[" + ex.Message + "]");
            }

            await UpdateDatabase(Categories);
        }
        private static async Task UpdateDatabase(List<Category> Categories)
        {
            try
            {
                using var Db = new Context();
                using var Trans = await Db.Database.BeginTransactionAsync();
                try
                {
                    Db.Database.ExecuteSqlRaw("DELETE FROM \"Category\"");
                    Db.Categories.AddRange(Categories);
                    await Db.SaveChangesAsync();
                    Trans.Commit();
                }
                catch
                {
                    Trans.Rollback();
                    throw new Exception("Cannot update database");
                }
            }
            catch
            {
                throw new Exception("Cannot connect To the database");
            }
        }
        private string CreateJsonParameters(DataColumnCollection Columns, DataRow Row)
        {
            List<CategoryParameter> Parameters = new();
            foreach (var Column in Columns)
            {
                /// Parameter => Parameter [Name]
                /// Features =>  Parameter Features [Name]
                string ColumnName = Column.ToString();
                if (ColumnName.StartsWith("Parameter", StringComparison.OrdinalIgnoreCase))
                {
                    string Value = string.IsNullOrWhiteSpace(Row[ColumnName].ToString()) ? null : Row[ColumnName].ToString().Trim();
                    if (ColumnName.StartsWith("Parameter Features"))
                        Parameters.Add(new CategoryParameter() { Name = $"{ColumnName.Replace("Parameter Features", "", StringComparison.OrdinalIgnoreCase).Trim()}", Value = Value, IsFeature = true });
                    else
                        Parameters.Add(new CategoryParameter() { Name = $"{ColumnName.Replace("Parameter", "", StringComparison.OrdinalIgnoreCase).Trim()}", Value = Value, IsFeature = false });
                }
            }
            return System.Text.Json.JsonSerializer.Serialize(Parameters);
        }
        private string CheckParamValue(object Parameter)
        {
            if (Parameter is null)
                return null;
            string ParamStr = Parameter.ToString();
            return string.IsNullOrWhiteSpace(ParamStr) ? null : ParamStr;
        }
    }
}
