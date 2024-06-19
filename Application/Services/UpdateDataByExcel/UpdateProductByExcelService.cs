using Application.Data;
using Application.Services.UpdateDataByExcel.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services.UpdateDataByExcel
{
    using Application.SeedWork;
    using Application.Services.Category;
    using Application.Services.Helper.Products;
    using Domain.Entity;

    public class UpdateProductByExcelService
    {
        private readonly IApplicationSettings appSettings;
        public UpdateProductByExcelService(IApplicationSettings appSettings)
        {
            this.appSettings = appSettings;
        }
        public async Task Update(Stream stream)
        {
            if (stream is null)
                throw new Exception("Please upload correct excel file.");
            List<Product> Products = new();
            //List<Product> ProductsForComputingInventory = new();
            try
            {
                var Tables = UpdateByExcelHelper.ReadExcel(stream);
                if (Tables?.Count > 0 && Tables[0].Rows?.Count > 0)
                {
                    CategoryService Service = new(appSettings, null);
                    List<CategoryShortDto> Categories = null;
                    try { Categories = (await Service.GetShortData(true).ConfigureAwait(false)); } catch { }
                    for (int i = 0; i < Tables[0].Rows.Count; i++)
                    {
                        var Row = Tables[0].Rows[i];
                        Guid Id = Guid.NewGuid();
                        Product NewProduct = CreateProduct(Products, Tables, Categories, Row, Id);
                        Products.Add(NewProduct);
                        //ProductsForComputingInventory.Add(CreateProduct(Products, Tables, Categories, Row, Id));
                    }
                    SetPieces(Products);
                    InventoryManager.SetInventory(Products);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("there are some errors during reading data from excel.[" + ex.Message + "]");
            }
            await UpdateDatabase(Products);
        }

        private Product CreateProduct(List<Product> Products, DataTableCollection Tables, List<CategoryShortDto> Categories, DataRow Row, Guid Id)
        {
            return new()
            {
                Id = Id,
                Name = Row["Name"].ToString().Trim(),
                Slug = UpdateByExcelHelper.GenerateSlug(Row["Name"].ToString(), Row["Slug"], Products.ConvertAll(x => x.Slug)),
                Description = Row["Description"].ToString().Trim(),
                ProductDescription = Row["ProductDescription"].ToString().Trim(),
                ShortDescription = Row["Short description"].ToString().Trim(),
                BoxD = UpdateByExcelHelper.GetDouble(Row["Box D"]),
                BoxW = UpdateByExcelHelper.GetDouble(Row["Box W"]),
                BoxH = UpdateByExcelHelper.GetDouble(Row["Box H"]),
                WHQTY = Row["WH QTY"].ToString().Trim(),
                Cube = UpdateByExcelHelper.GetDouble(Row["Cube"]),
                D = UpdateByExcelHelper.GetDouble(Row["D"]),
                H = UpdateByExcelHelper.GetDouble(Row["H"]),
                W = UpdateByExcelHelper.GetDouble(Row["W"]),
                Weight = UpdateByExcelHelper.GetDouble(Row["Weight (lbs)"]),
                Security = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Security"].ToString().Trim()),
                Categories = GetCategoriesByName(Id, Row["Categories"]?.ToString(), Categories),
                Price = CreateJsonPrices(Tables[0].Columns, Row),
                Order = UpdateByExcelHelper.GetInt32WithDefaultZero(Row["Position"]),
                OrderInShop = UpdateByExcelHelper.GetInt32WithDefaultZero(Row["PositionInShop"]),
                Inventory = UpdateByExcelHelper.GetDoubleWithdefaultValue(Row["Inventory"], 0),
                Groups = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Groups"].ToString().Trim()),
                Factories = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Factories"].ToString().Trim()),
                Tags = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Tags"].ToString().Trim()),
                ComplexItemPieces = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Peace of Complex Item"].ToString().Trim()),
                ComplexItemPriority = UpdateByExcelHelper.GetInt32WithDefaultZero(Row["Complex Item Priority"]),
                Features = UpdateByExcelHelper.CreateJsonKeyValue(Tables[0].Columns, Row, "Feature"),
                PricePermissions = UpdateByExcelHelper.CreateJsonKeyValue(Tables[0].Columns, Row, "Permission Price"),
                IsSample = Row["Is Sample"].ToString().Trim(),
            };
        }

        private static void SetPieces(List<Product> Products)
        {
            foreach (var Product in Products)
            {
                if (!string.IsNullOrWhiteSpace(Product.ComplexItemPieces))
                {
                    var PiecesNames = System.Text.Json.JsonSerializer.Deserialize<List<string>>(Product.ComplexItemPieces);
                    Product.PiecesCount = PiecesNames?.Count > 0 ? PiecesNames.Count : 1;
                }
                else
                    Product.PiecesCount = 1;
            }
        }

        private static async Task UpdateDatabase(List<Product> Products)
        {
            try
            {
                using var Db = new Context();
                using var Trans = await Db.Database.BeginTransactionAsync();
                try
                {
                    Db.Database.ExecuteSqlRaw("DELETE FROM \"Product\"");
                    Db.Products.AddRange(Products);
                    await Db.SaveChangesAsync();
                    Trans.Commit();
                }
                catch (Exception Ex)
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

        private string CreateJsonPrices(DataColumnCollection Columns, DataRow Row)
        {
            List<ProductPrice> PricesList = new();
            foreach (var Column in Columns)
            {
                string ColumnName = Column.ToString();
                if (ColumnName.StartsWith("Price", StringComparison.OrdinalIgnoreCase))
                {
                    decimal? Price = UpdateByExcelHelper.GetDecimal(Row[ColumnName]);
                    PricesList.Add(new ProductPrice() { Name = $"{ColumnName.Replace("Price", "", StringComparison.OrdinalIgnoreCase).Trim()}", Value = Price });
                }
            }
            return System.Text.Json.JsonSerializer.Serialize(PricesList);
        }
        private List<CategoryProduct> GetCategoriesByName(Guid ProductId, string CategoryNames, List<CategoryShortDto> Categories)
        {
            if (!string.IsNullOrWhiteSpace(CategoryNames))
            {
                List<CategoryProduct> CategoriesProducts = new();
                string[] CatNames = CategoryNames.ToString().Split(",");
                foreach (var CatName in CatNames)
                {
                    var Cat = Categories.Find(x => x.Name.Equals(CatName, StringComparison.OrdinalIgnoreCase));
                    if (Cat is not null)
                        CategoriesProducts.Add(new CategoryProduct() { CategoryId = Cat.Id, ProductId = ProductId, CategorySlug = Cat.Slug });
                }
                return CategoriesProducts;

            }
            return null;
        }
    }
}
