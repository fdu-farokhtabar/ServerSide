using iText.Html2pdf;
using Application.SeedWork;
using Application.Services.Category;
using Application.Services.Helper;
using Application.Services.Product;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Catalog
{
    public class CatalogService
    {
        private readonly IApplicationSettings appSettings;
        private readonly CategoryService categoryService;
        private readonly ProductService productService;
        private static long FileNamecounter = 0;
        public CatalogService(IApplicationSettings appSettings, List<string> userRoles = null)
        {
            this.appSettings = appSettings;
            categoryService = new CategoryService(appSettings, userRoles);
            productService = new ProductService(appSettings, userRoles);
        }


        //public async Task<string> Generate(List<Guid> ProductsId, List<string> CategoriesSlug, List<string> Factories, List<int> Prices, double LandedPrice = 0)
        //{
        //    //لیست دسته ها و محصولات
        //    //Task<List<CategoryDto>> TaskCategories = categoryService.Get(false);
        //    //Task<List<ProductWithSlugCatDto>> TaskProducts = productService.GetWithCatSlug(false);
        //    //await Task.WhenAll(TaskCategories, TaskProducts);
        //    //List<CategoryDto> Categories = TaskCategories.Result;
        //    //List<ProductWithSlugCatDto> Products = TaskProducts.Result;


        //    List<CategoryDto> Categories = await categoryService.Get();
        //    if (CategoriesSlug?.Count > 0)
        //        Categories = Categories.Where(x => CategoriesSlug.Contains(x.Slug)).ToList();

        //    List<ProductWithSlugCatDto> Products = await productService.GetWithCatSlug();

        //    if (ProductsId?.Count > 0)
        //        Products = Products.Where(x => ProductsId.Contains(x.Id)).ToList();

        //    if (Products?.Count > 0)
        //    {
        //        if (Factories?.Count > 0)
        //        {
        //            foreach (var factory in Factories)
        //                Products = Products.Where(x => x.Factories?.Count > 0 && x.Factories.Contains(factory)).ToList();
        //        }
        //        List<CategoryDto> activeCategories = new();
        //        foreach (var cat in Categories)
        //        {
        //            if (Products.Any(x => x.CategoryIds.Contains(cat.Id)))
        //                activeCategories.Add(cat);
        //        }

        //        return await CreateOnePdfBasedOnProducts(Prices.ToArray(), Products, activeCategories, LandedPrice);
        //    }
        //    else
        //        throw new Exception("There are not any products.");
        //}
        public async Task<(string RelativePath, string ServerPath)> Generate(List<string> CategoriesSlug, List<string> Factories, List<int> Prices, bool JustAvailable = false, double LandedPrice = 0)
        {
            List<CategoryDto> Categories = await categoryService.Get();
            if (CategoriesSlug?.Count > 0)
                Categories = Categories.Where(x => CategoriesSlug.Contains(x.Slug)).ToList();

            List<ProductWithSlugCatDto> Products = await productService.GetWithCatSlug();
            if (Products?.Count > 0)
            {
                if (Factories?.Count > 0)
                {
                    foreach (var factory in Factories)
                        Products = Products.Where(x => x.Factories?.Count > 0 && x.Factories.Contains(factory)).ToList();
                }

                List<ProductWithSlugCatDto> activeProducts = new();
                foreach (var prd in Products)
                {
                    var isActive = true;
                    if (Factories?.Count > 0)
                        isActive = Factories.Any(x => prd.Factories.Contains(x));
                    if (isActive && CategoriesSlug?.Count > 0)
                        isActive = Categories.Any(x => prd.CategoryIds.Contains(x.Id));
                    //0 => FOB
                    if (isActive && JustAvailable && ((Prices is not null && !Prices.Any(x => x == 0)) || (Prices is null && LandedPrice > 0)))
                        isActive = string.Equals(prd.WHQTY, "Available", StringComparison.OrdinalIgnoreCase);
                    if (isActive)
                        activeProducts.Add(prd);
                }

                return await CreateOnePdfBasedOnProducts(Prices?.ToArray(), activeProducts, Categories, LandedPrice, (CategoriesSlug?.Count > 0));
            }
            else
                throw new Exception("There are not any products.");
        }
        public async Task Create()
        {
            string CatalogsPath = appSettings.WwwRootPath + @"\Catalogs\";
            DirectoryInfo CatalogRoot = new(CatalogsPath);
            foreach (var Dir in CatalogRoot.GetDirectories())
                Dir.Delete(true);
            foreach (var File in CatalogRoot.GetFiles())
                File.Delete();

            await Create(null);
            await Create(new int[] { 0 });
            await Create(new int[] { 0, 1 });
            await Create(new int[] { 1, 2 });
            await Create(new int[] { 0, 1, 2 });
        }

        public async Task CreateWithLandedPrice(double Cost, string CategorySlug)
        {
            string CatalogsPath = appSettings.WwwRootPath + @"\Catalogs\LandedPrices\";
            DirectoryInfo CatalogRoot = new(CatalogsPath);
            //if (CatalogRoot.Exists)
            //{
            //    foreach (var Dir in CatalogRoot.GetDirectories())
            //        Dir.Delete(true);
            //    foreach (var File in CatalogRoot.GetFiles())
            //        File.Delete();
            //}
            //else
            //    CatalogRoot.Create();
            if (!CatalogRoot.Exists)
                CatalogRoot.Create();

            await Create(new int[] { 0 }, Cost, CategorySlug);
        }


        private async Task Create(int[] PriceRange, double Cost = 0, string CategorySlug = "")
        {
            string CurrentDateTime = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");
            string CatalogsPath = appSettings.WwwRootPath + @"\Catalogs\" + (Cost > 0 ? @"LandedPrices\" : "");
            string AssetCatalogPath = appSettings.WwwRootPath + @"\Assets\Catalog\";
            Task<List<CategoryDto>> TaskCategories = categoryService.Get(true);
            Task<List<ProductWithSlugCatDto>> TaskProducts = productService.GetWithCatSlug(true);
            Task<string> TaskStyle = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogStyle.css");
            Task<string> TaskTemplateFirstPage = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogFirstPage.html");
            Task<string> TaskTemplateBody = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogBody.html");
            Task<string> TaskTemplateSingleBanner = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogSingleBanner.html");
            Task<string> TaskCatalogDetailsTable = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogDetailsTable.html");
            Task<string> TaskCatalogDetailsTableRow = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogDetailsTableRow.html");
            Task<string> TaskCatalogMeasureTable = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTable.html");
            Task<string> TaskCatalogMeasureTableRow = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTableRow.html");
            Task<string> TaskCatalogMeasureTablePriceHeader = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTablePriceHeader.html");
            Task<string> TaskCatalogMeasureTablePriceCellValue = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTablePriceCellValue.html");
            Task<string> TaskCatalogFeaturesTable = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogFeaturesTable.html");
            Task<string> TaskCatalogFeaturesTableRow = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogFeaturesTableRow.html");

            Task<string> TaskTemplateCatalog = File.ReadAllTextAsync($"{AssetCatalogPath}TemplateCatalog.html");

            await Task.WhenAll(TaskCategories, TaskProducts, TaskStyle, TaskTemplateFirstPage, TaskTemplateBody,
                               TaskTemplateSingleBanner, TaskCatalogDetailsTable, TaskCatalogDetailsTableRow,
                               TaskCatalogMeasureTable, TaskCatalogMeasureTableRow, TaskCatalogFeaturesTable,
                               TaskCatalogFeaturesTableRow, TaskCatalogMeasureTablePriceHeader).ConfigureAwait(false);

            List<CategoryDto> Categories = TaskCategories.Result;
            List<ProductWithSlugCatDto> Products = TaskProducts.Result;
            string Style = TaskStyle.Result;
            string TemplateFirstPage = TaskTemplateFirstPage.Result.Replace("{Logo}", $"{AssetCatalogPath}Images\\kian_usa_logo.jpg").Replace("{TitleLogo}", $"{AssetCatalogPath}Images\\kian_usa_title_logo.jpg");
            string TemplateSingleBanner = TaskTemplateSingleBanner.Result;
            string TemplateBody = TaskTemplateBody.Result;
            string TemplateCatalogDetailsTable = TaskCatalogDetailsTable.Result;
            string TemplateCatalogDetailsTableRow = TaskCatalogDetailsTableRow.Result;
            string TemplateCatalogMeasureTable = TaskCatalogMeasureTable.Result;
            string TemplateCatalogMeasureTableRow = TaskCatalogMeasureTableRow.Result;
            string TemplateCatalogFeaturesTable = TaskCatalogFeaturesTable.Result;
            string TemplateCatalogFeaturesTableRow = TaskCatalogFeaturesTableRow.Result;
            string TemplateCatalogMeasureTablePriceCellValue = TaskCatalogMeasureTablePriceCellValue.Result;
            string TemplateCatalogMeasureTablePriceHeader = TaskCatalogMeasureTablePriceHeader.Result;
            string TemplateCatalog = TaskTemplateCatalog.Result;

            await Task.Run(() =>
            {
                ConcurrentBag<(int Order, string Body, string Name)> Catalogs = new();
                string LandedPrice = Cost > 0 ? "_LandedPrice_" + Cost.ToString() : "";
                string PriceType = "";
                string PriceTypeDirectory = "";
                if (PriceRange?.Length > 0)
                {
                    PriceType = string.Join("_", PriceRange);
                    if (PriceType != "")
                    {
                        if (!Directory.Exists($"{CatalogsPath}{PriceType}"))
                            Directory.CreateDirectory($"{CatalogsPath}{PriceType}");
                        PriceTypeDirectory = PriceType + "\\";
                        PriceType = "_" + PriceType;
                    }
                }

                Parallel.ForEach(Categories, Category =>
                {
                    if (Category.PublishedCatalogType != PublishedCatalogTypeDto.None)
                    {
                        string Body = TemplateBody;
                        Body = Body.Replace("{Banner}", CreateSingleBanner(Category, TemplateSingleBanner, AssetCatalogPath));
                        Body = Body.Replace("{DetailsTable}", CreateDetailsTable(Category, TemplateCatalogDetailsTable, TemplateCatalogDetailsTableRow));
                        Body = Body.Replace("{FeaturesTable}", CreateFeaturesTable(Category, TemplateCatalogFeaturesTable, TemplateCatalogFeaturesTableRow));
                        Body = Body.Replace("{MeasureTable}", CreateMeasureTable(Category, Products, TemplateCatalogMeasureTable, TemplateCatalogMeasureTableRow, TemplateCatalogMeasureTablePriceHeader, TemplateCatalogMeasureTablePriceCellValue, PriceRange, Cost));
                        if (Category.PublishedCatalogType == PublishedCatalogTypeDto.Main || Category.PublishedCatalogType == PublishedCatalogTypeDto.SingleAndMain)
                        {
                            Catalogs.Add((Category.Order, Body, Category.Name));
                        }
                        Body = Body.Replace("{PageNumber}", "1");
                        Body = Body.Replace("{DateTime}", CurrentDateTime);
                        Body = Body.Replace("{CategoryName}", Category.Name);
                        if (Category.PublishedCatalogType != PublishedCatalogTypeDto.Main)
                        {
                            if (Cost == 0 || (Cost > 0 && string.Equals(Category.Slug, CategorySlug, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                using FileStream pdfDest = File.Open($"{CatalogsPath}{PriceTypeDirectory}{Category.Slug}{PriceType}{LandedPrice}.pdf", FileMode.Create);
                                HtmlConverter.ConvertToPdf(TemplateCatalog.Replace("{Style}", Style).Replace("{Body}", TemplateFirstPage + Body), pdfDest);
                            }
                        }
                    }
                });

                //Create MainCatalog
                if (Catalogs?.Count > 0)
                {
                    using FileStream AllInOnePdf = File.Open($"{CatalogsPath}{PriceTypeDirectory}Catalog{PriceType}{LandedPrice}.pdf", FileMode.Create, FileAccess.ReadWrite);
                    StringBuilder All = new();
                    var SortedCatalogs = Catalogs.OrderBy(x => x.Order).ToList();
                    for (int i = 0; i < SortedCatalogs.Count; i++)
                    {
                        All.Append(SortedCatalogs[i].Body.Replace("{PageNumber}", (i + 1).ToString()).Replace("{DateTime}", CurrentDateTime).Replace("{CategoryName}", SortedCatalogs[i].Name));
                    }
                    HtmlConverter.ConvertToPdf(TemplateCatalog.Replace("{Style}", Style).Replace("{Body}", TemplateFirstPage + All.ToString()), AllInOnePdf);
                }
            });

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PriceRange"></param>
        /// <param name="Products"></param>
        /// <param name="Categories"></param>
        /// <param name="Cost"></param>
        /// <param name="IsSingular">به معنی این است که چند یا یک کاتالوگ به صورت تکی جهت ایجاد چی دی اف انتخاب شده است</param>
        /// <returns></returns>
        private async Task<(string RelativePath, string ServerPath)> CreateOnePdfBasedOnProducts(int[] PriceRange, List<ProductWithSlugCatDto> Products, List<CategoryDto> Categories, double Cost, bool IsSingular)
        {
            string RelativePath = null;
            string PhysicalServerPath = null;

            string CurrentDateTime = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");
            string CatalogsPath = appSettings.WwwRootPath + @"\Catalogs\" + (Cost > 0 ? @"LandedPrices\" : "");
            string AssetCatalogPath = appSettings.WwwRootPath + @"\Assets\Catalog\";
            Task<string> TaskStyle = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogStyle.css");
            Task<string> TaskTemplateFirstPage = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogFirstPage.html");
            Task<string> TaskTemplateBody = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogBody.html");
            Task<string> TaskTemplateSingleBanner = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogSingleBanner.html");
            Task<string> TaskCatalogDetailsTable = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogDetailsTable.html");
            Task<string> TaskCatalogDetailsTableRow = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogDetailsTableRow.html");
            Task<string> TaskCatalogMeasureTable = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTable.html");
            Task<string> TaskCatalogMeasureTableRow = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTableRow.html");
            Task<string> TaskCatalogMeasureTablePriceHeader = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTablePriceHeader.html");
            Task<string> TaskCatalogMeasureTablePriceCellValue = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogMeasureTablePriceCellValue.html");
            Task<string> TaskCatalogFeaturesTable = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogFeaturesTable.html");
            Task<string> TaskCatalogFeaturesTableRow = File.ReadAllTextAsync($"{AssetCatalogPath}CatalogFeaturesTableRow.html");

            Task<string> TaskTemplateCatalog = File.ReadAllTextAsync($"{AssetCatalogPath}TemplateCatalog.html");

            await Task.WhenAll(TaskStyle, TaskTemplateFirstPage, TaskTemplateBody,
                               TaskTemplateSingleBanner, TaskCatalogDetailsTable, TaskCatalogDetailsTableRow,
                               TaskCatalogMeasureTable, TaskCatalogMeasureTableRow, TaskCatalogFeaturesTable,
                               TaskCatalogFeaturesTableRow, TaskCatalogMeasureTablePriceHeader).ConfigureAwait(false);

            string Style = TaskStyle.Result;
            string TemplateFirstPage = TaskTemplateFirstPage.Result.Replace("{Logo}", $"{AssetCatalogPath}Images\\kian_usa_logo.jpg").Replace("{TitleLogo}", $"{AssetCatalogPath}Images\\kian_usa_title_logo.jpg");
            string TemplateSingleBanner = TaskTemplateSingleBanner.Result;
            string TemplateBody = TaskTemplateBody.Result;
            string TemplateCatalogDetailsTable = TaskCatalogDetailsTable.Result;
            string TemplateCatalogDetailsTableRow = TaskCatalogDetailsTableRow.Result;
            string TemplateCatalogMeasureTable = TaskCatalogMeasureTable.Result;
            string TemplateCatalogMeasureTableRow = TaskCatalogMeasureTableRow.Result;
            string TemplateCatalogFeaturesTable = TaskCatalogFeaturesTable.Result;
            string TemplateCatalogFeaturesTableRow = TaskCatalogFeaturesTableRow.Result;
            string TemplateCatalogMeasureTablePriceCellValue = TaskCatalogMeasureTablePriceCellValue.Result;
            string TemplateCatalogMeasureTablePriceHeader = TaskCatalogMeasureTablePriceHeader.Result;
            string TemplateCatalog = TaskTemplateCatalog.Result;

            await Task.Run(() =>
            {
                ConcurrentBag<(int Order, string Body, string Name)> Catalogs = new();
                string LandedPrice = Cost > 0 ? "_LandedPrice_" + Cost.ToString() : "";
                string PriceType = "";
                string PriceTypeDirectory = "";
                if (PriceRange?.Length > 0)
                {
                    PriceType = string.Join("_", PriceRange);
                    if (PriceType != "")
                    {
                        if (!Directory.Exists($"{CatalogsPath}{PriceType}"))
                            Directory.CreateDirectory($"{CatalogsPath}{PriceType}");
                        PriceTypeDirectory = PriceType + "\\";
                        PriceType = "_" + PriceType;
                    }
                }

                Parallel.ForEach(Categories, Category =>
                {
                    if (Category.PublishedCatalogType != PublishedCatalogTypeDto.None &&
                    (Categories.Count <= 1 || Products.Any(x => x.CategoryIds.Contains(Category.Id))))
                    {
                        string Body = TemplateBody;
                        Body = Body.Replace("{Banner}", CreateSingleBanner(Category, TemplateSingleBanner, AssetCatalogPath));
                        Body = Body.Replace("{DetailsTable}", CreateDetailsTable(Category, TemplateCatalogDetailsTable, TemplateCatalogDetailsTableRow));
                        Body = Body.Replace("{FeaturesTable}", CreateFeaturesTable(Category, TemplateCatalogFeaturesTable, TemplateCatalogFeaturesTableRow));
                        Body = Body.Replace("{MeasureTable}", CreateMeasureTable(Category, Products, TemplateCatalogMeasureTable, TemplateCatalogMeasureTableRow, TemplateCatalogMeasureTablePriceHeader, TemplateCatalogMeasureTablePriceCellValue, PriceRange, Cost));
                        if (Category.PublishedCatalogType == PublishedCatalogTypeDto.Main || Category.PublishedCatalogType == PublishedCatalogTypeDto.SingleAndMain ||
                            (IsSingular && Category.PublishedCatalogType == PublishedCatalogTypeDto.Single))
                        {
                            Catalogs.Add((Category.Order, Body, Category.Name));
                        }
                        Body = Body.Replace("{PageNumber}", "1");
                        Body = Body.Replace("{DateTime}", CurrentDateTime);
                        Body = Body.Replace("{CategoryName}", Category.Name);
                    }
                });

                //Create MainCatalog                
                if (Catalogs?.Count > 0)
                {
                    Interlocked.Increment(ref FileNamecounter);
                    RelativePath = $"{PriceTypeDirectory}Catalog{PriceType}{LandedPrice}_{GetFullDateTimeString()}_{FileNamecounter}.pdf";
                    PhysicalServerPath = $"{CatalogsPath}{RelativePath}";
                    using FileStream AllInOnePdf = File.Open(PhysicalServerPath, FileMode.Create, FileAccess.ReadWrite);
                    StringBuilder All = new();
                    var SortedCatalogs = Catalogs.OrderBy(x => x.Order).ToList();
                    for (int i = 0; i < SortedCatalogs.Count; i++)
                    {
                        All.Append(SortedCatalogs[i].Body.Replace("{PageNumber}", (i + 1).ToString()).Replace("{DateTime}", CurrentDateTime).Replace("{CategoryName}", SortedCatalogs[i].Name));
                    }
                    HtmlConverter.ConvertToPdf(TemplateCatalog.Replace("{Style}", Style).Replace("{Body}", TemplateFirstPage + All.ToString()), AllInOnePdf);
                }
            });

            return ((Cost > 0 ? @"LandedPrices/" : "") + RelativePath, PhysicalServerPath);
        }
        private string GetFullDateTimeString()
        {
            var Current = DateTime.Now;
            return Current.Year.ToString("0000") + Current.Month.ToString("00") + Current.Day.ToString("00") + Current.Hour.ToString("00") + Current.Minute.ToString("00") + Current.Second.ToString("00") + Current.Millisecond.ToString("00");
        }
        private static string CreateMeasureTable(CategoryDto Category, List<ProductWithSlugCatDto> Products, string TemplateCatalogMeasureTable, string TemplateCatalogMeasureTableRow, string TemplateCatalogMeasureTablePriceHeader, string TemplateCatalogMeasureTablePriceCellValue, int[] PriceRange, double Cost)
        {
            var CurrentProducts = Products.Where(x => x.CategorySlug.Contains(Category.Slug)).OrderBy(x => x.Order).ToList();
            if (CurrentProducts?.Count > 0)
            {
                (List<(int Idx, string Color)> AcceptablePriceIndexesWithColors, string AcceptablePriceHeaders) = GetAcceptablePriceIndexesAndHeaders(CurrentProducts, TemplateCatalogMeasureTablePriceHeader, PriceRange, Cost);
                string Rows = "";
                foreach (var CurrentProduct in CurrentProducts)
                {
                    string AcceptablePriceCells = "";
                    if (AcceptablePriceIndexesWithColors?.Count > 0)
                    {
                        foreach (var Index in AcceptablePriceIndexesWithColors)
                            AcceptablePriceCells += TemplateCatalogMeasureTablePriceCellValue.Replace("{ProductPriceValue}", Tools.GetPriceFormat(ComputeLandedPrice(Index.Idx, CurrentProduct, Cost))).Replace("{PriceCellColor}", Index.Color);
                    }
                    string RowStyle = "";
                    if (CurrentProduct.Name.Contains("S/L", StringComparison.InvariantCultureIgnoreCase) || CurrentProduct.Name.Contains("Sec", StringComparison.InvariantCultureIgnoreCase))
                        RowStyle = " style='font-weight:bold;'";

                    Rows += TemplateCatalogMeasureTableRow.Replace("{RowStyle}", RowStyle)
                                                          .Replace("{Product.Name}", CurrentProduct.Name)
                                                          .Replace("{Product.ShortDescription}", CurrentProduct.ShortDescription)
                                                          .Replace("{PriceValues}", AcceptablePriceCells)
                                                          .Replace("{Product.Weight}", CurrentProduct.Weight.ToString())
                                                          .Replace("{Product.ItemWidth}", CurrentProduct.W.ToString())
                                                          .Replace("{Product.ItemDepth}", CurrentProduct.D.ToString())
                                                          .Replace("{Product.ItemHeight}", CurrentProduct.H.ToString())
                                                          .Replace("{Product.BoxWidth}", CurrentProduct.BoxW.ToString())
                                                          .Replace("{Product.BoxDepth}", CurrentProduct.BoxD.ToString())
                                                          .Replace("{Product.BoxHeight}", CurrentProduct.BoxH.ToString())
                                                          .Replace("{Product.Cube}", CurrentProduct.Cube.ToString());
                }
                return TemplateCatalogMeasureTable.Replace("{PriceHeaders}", AcceptablePriceHeaders).Replace("{CatalogMeasureTableRows}", Rows);
            }
            return "";
        }
        private static decimal? ComputeLandedPrice(int Idx, ProductWithSlugCatDto CurrentProduct, double Cost)
        {
            try
            {
                //Landed Price
                if (Idx == 999)
                {
                    if (Cost > 0 && CurrentProduct.Cube.HasValue && CurrentProduct.Cube > 0 && CurrentProduct.Prices[0].Value.HasValue)
                        return CurrentProduct.Prices[0].Value.Value + (decimal)(CurrentProduct.Cube.Value * (Cost / 2350));
                    else
                        return null;
                }
                else
                {
                    if (!CurrentProduct.Prices[Idx].Value.HasValue)
                        return null;
                    else
                        return CurrentProduct.Prices[Idx].Value.Value;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Price Problem");
            }
        }
        private static (List<(int Idx, string Color)>, string) GetAcceptablePriceIndexesAndHeaders(List<ProductWithSlugCatDto> CurrentProducts, string TemplateHeader, int[] PriceRange, double Cost)
        {
            List<(int, string)> AcceptablePriceIndexes = null;
            string Headers = "";
            var FirstProduct = CurrentProducts[0];
            if (FirstProduct.Prices?.Count > 0)
            {
                AcceptablePriceIndexes = new List<(int, string)>();
                //Other Price 0,1,2
                for (int i = 0; i < FirstProduct.Prices.Count; i++)
                {
                    // Null PriceRange Show all prices => if (PriceRange is null || PriceRange.Length == 0 || (PriceRange?.Length > 0 && PriceRange.Contains(i)))
                    if (PriceRange?.Length > 0 && PriceRange.Contains(i))
                    {
                        foreach (var Product in CurrentProducts)
                        {
                            if (Product.Prices[i].Value != null)
                            {
                                AcceptablePriceIndexes.Add((i, GetPriceColor(i)));
                                Headers += TemplateHeader.Replace("{PriceHeaderName}", Product.Prices[i].Name).Replace("{PriceHeaderColor}", "");
                                break;
                            }
                        }
                    }
                }
                //Landed Price
                if (Cost > 0)
                {
                    foreach (var Product in CurrentProducts)
                    {
                        if (Product.Prices[0].Value != null)
                        {
                            AcceptablePriceIndexes.Add((999, GetPriceColor(0, Cost)));
                            Headers += TemplateHeader.Replace("{PriceHeaderName}", "Mix Container <br/> Landed").Replace("{PriceHeaderColor}", "");
                            break;
                        }
                    }
                }

            }
            return (AcceptablePriceIndexes, Headers);
        }

        private static string GetPriceColor(int Index, double Factor = 0)
        {
            switch (Index)
            {
                case 0:
                    if (Factor > 0)
                        return "background-color:#fbf1cc;color:black;";
                    else
                        return "background-color:#b4c4e7;color:black;";
                case 1:
                case 2:
                    return "background-color:#a9cd8d;color:black;";
                default:
                    break;
            }
            return "";
        }

        private string CreateSingleBanner(CategoryDto Category, string TemplateSingleBanner, string AssetCatalogPath)
        {
            if (Category.ImagesUrl?.Count > 0)
            {
                //Category.ImagesUrl.Sort();
                foreach (var ImageUrl in Category.ImagesUrl)
                {
                    if (System.Convert.ToInt32(ImageUrl.Substring(ImageUrl.LastIndexOf("_") + 1, 4)) >= appSettings.StartIndexOfImageForUsingInCatalog)
                        return TemplateSingleBanner.Replace("{BannerSrc}", appSettings.WwwRootPath + ImageUrl.Replace("/", "\\"));
                }
            }
            return TemplateSingleBanner.Replace("{BannerSrc}", $"{AssetCatalogPath}Images\\kian_usa_comming_soon.jpg");
        }

        private static string CreateDetailsTable(CategoryDto Category, string TemplateCatalogDetailsTable, string TemplateCatalogDetailsTableRow)
        {
            if (Category.Parameters?.Count > 0 && Category.Parameters.Any(x => !string.IsNullOrWhiteSpace(x.Value)))
            {
                string Rows = "";
                foreach (var Parameter in Category.Parameters)
                {
                    if (!string.IsNullOrEmpty(Parameter.Value))
                        Rows += TemplateCatalogDetailsTableRow.Replace("{DetailsTableTitle}", Parameter.Name).Replace("{DetailsTableValue}", Parameter.Value);
                }
                return TemplateCatalogDetailsTable.Replace("{CatalogDetailsRowsTable}", Rows);
            }
            return "";
        }

        private static string CreateFeaturesTable(CategoryDto Category, string TemplateCatalogFeaturesTable, string TemplateCatalogFeaturesTableRow)
        {
            if (Category.Features?.Count > 0 && Category.Features.Any(x => !string.IsNullOrWhiteSpace(x.Value)))
            {
                string Rows = "";
                foreach (var Feature in Category.Features)
                {
                    if (!string.IsNullOrEmpty(Feature.Value))
                        Rows += TemplateCatalogFeaturesTableRow.Replace("{FeaturesTableRowValue}", Feature.Value);
                }
                return TemplateCatalogFeaturesTable.Replace("{CatalogFeaturesTableRows}", Rows);
            }
            return "";
        }
    }
}
