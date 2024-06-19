using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Application.Services.Helper
{
    public static class ManageImages
    {
        private const string START_NAME_PRODUCT_IMAGE_FILE = "kian_usa_product_";
        private const string START_NAME_CATEGORY_IMAGE_FILE = "kian_usa_category_";
        public static string GetStartNameOfProductImageFileName(string ProductName) => $"{START_NAME_PRODUCT_IMAGE_FILE}{ProductName.Replace(" ", "").Replace("_", "").Replace("/", "").Replace(@"\", "")}";
        public static string GetStartNameOfCategoryImageFileName(string CategoryName) => $"{START_NAME_CATEGORY_IMAGE_FILE}{CategoryName.Replace(" ", "").Replace("_", "").Replace("/", "").Replace(@"\", "")}";
        public static List<string> GetProductsAndCategoriesFiles(string HostPath)
        {
            List<string> Result = new();
            var PrdResult = GetProductsImagesUrl(HostPath);
            var CatResult = GetCategoriesImagesUrl(HostPath);
            if (PrdResult is not null)
                Result.AddRange(PrdResult);
            if (CatResult is not null)
                Result.AddRange(CatResult);
            return Result;
        }

        public static List<string> GetProductsImagesUrl(string HostPath)
        {
            return Directory.GetFiles(HostPath + @"\Images\Products", $"{START_NAME_PRODUCT_IMAGE_FILE}*").Select(x => ("/Images/Products/" + Path.GetFileName(x))).ToList();
        }
        public static List<string> GetProductImagesUrl(string ProductName, string HostPath)
        {
            if (!string.IsNullOrWhiteSpace(ProductName))
                return Directory.GetFiles(HostPath + @"\Images\Products", $"{GetStartNameOfProductImageFileName(ProductName)}*").Select(x => ("/Images/Products/" + Path.GetFileName(x))).ToList();
            else
                return null;
        }
        public static List<string> GetCategoriesImagesUrl(string HostPath)
        {
            return Directory.GetFiles(HostPath + @"\Images\Products", $"{START_NAME_CATEGORY_IMAGE_FILE}*").Select(x => ("/Images/Products/" + Path.GetFileName(x))).ToList();
        }
        public static List<string> GetCategoryImagesUrl(string CategoryName, string HostPath)
        {
            if (!string.IsNullOrWhiteSpace(CategoryName))
                return Directory.GetFiles(HostPath + @"\Images\Products", $"{GetStartNameOfCategoryImageFileName(CategoryName)}*").Select(x => ("/Images/Products/" + Path.GetFileName(x))).ToList();
            else
                return null;
        }
    }
}