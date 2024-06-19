using Application.Data;
using Application.SeedWork;
using Application.Services.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Product
{
    using Application.Services.Category;
    using Domain.Entity;
    using System.Collections.Concurrent;

    public class ProductService
    {
        private readonly IApplicationSettings appSettings;
        private readonly List<string> userRoles;
        public ProductService(IApplicationSettings appSettings, List<string> userRoles)
        {
            this.appSettings = appSettings;
            this.userRoles = userRoles;
        }
        public async Task<ProductDto> Get(Guid Id)
        {
            using var Db = new Context();
            var Model = await Db.Products.AsNoTracking().Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == Id).ConfigureAwait(false);
            if (Model is null)
                throw new ValidationException("Product does not exist.");
            var catSrv = new CategoryService(appSettings, userRoles);
            var cats = await catSrv.Get().ConfigureAwait(false);
            if (!HasProductPermission(Model, cats.Select(x => x.Id).ToList(), userRoles))
                throw new ValidationException("Unfortunately you do not have permissions to see the product");
            var ImagesUrl = ManageImages.GetProductImagesUrl(Model.Name, appSettings.WwwRootPath);
            return MapTo(Model, ImagesUrl);
        }
        public async Task<ProductDto> Get(string Slug)
        {
            using var Db = new Context();
            var Model = await Db.Products.AsNoTracking().Include(x => x.Categories).FirstOrDefaultAsync(x => x.Slug == Slug.ToLower()).ConfigureAwait(false);
            if (Model is null)
                throw new ValidationException("Product does not exist.");
            var catSrv = new CategoryService(appSettings, userRoles);
            var cats = await catSrv.Get().ConfigureAwait(false);
            if (!HasProductPermission(Model, cats.Select(x => x.Id).ToList(), userRoles))
                throw new ValidationException("Unfortunately you do not have permissions to see the product");

            var ImagesUrl = ManageImages.GetProductImagesUrl(Model.Name, appSettings.WwwRootPath);
            return MapTo(Model, ImagesUrl);
        }
        public async Task<List<ProductDto>> Get()
        {
            using var Db = new Context();
            var Models = await Db.Products.AsNoTracking().Include(x => x.Categories).ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("There are not any products.");
            Models = await RemoveProductsWithoutPermissionsFromLists(Models, appSettings, userRoles);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see products.");
            return Mapto(Models, ManageImages.GetProductsImagesUrl(appSettings.WwwRootPath));
        }
        /// <summary>
        /// Page Start With 0
        /// </summary>
        /// <param name="Groups"></param>
        /// <param name="Tags"></param>
        /// <param name="PageNumber"></param>
        /// <param name="PageCount"></param>
        /// <param name="IsDesc"></param>
        /// <returns></returns>
        public async Task<ProductsWithTotalItemDto> GetByGroupAndTagsWithPaging(List<string> Groups, List<string> Tags, int PageNumber, int PageCount, bool IsAscOrder = true)
        {
            var Products = await Get();
            ConcurrentBag<ProductDto> Result = new();
            if ((Groups is null || Groups.Count == 0) && (Tags is null || Tags.Count == 0))
            {
                if (IsAscOrder)
                    return new ProductsWithTotalItemDto() { TotalItems = Products is not null ? Products.Count : 0, Products = Products?.OrderBy(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                else
                    return new ProductsWithTotalItemDto() { TotalItems = Products is not null ? Products.Count : 0, Products = Products?.OrderByDescending(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
            }
            else
            {
                //Just Tags
                if (Tags?.Count > 0 && (Groups is null || Groups.Count == 0))
                {
                    Parallel.ForEach(Products, Prd =>
                    {
                        if (Prd.Tags?.Count > 0 && Prd.Tags.Count >= Tags.Count && Tags.All(x => Prd.Tags.Contains(x)))
                            Result.Add(Prd);
                    });
                    if (IsAscOrder)
                        return new ProductsWithTotalItemDto() { TotalItems = Result is not null ? Result.Count : 0, Products = Result?.OrderBy(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                    else
                        return new ProductsWithTotalItemDto() { TotalItems = Result is not null ? Result.Count : 0, Products = Result?.OrderByDescending(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                }
                //Just Groups
                else if (Groups?.Count > 0 && (Tags is null || Tags.Count == 0))
                {
                    Parallel.ForEach(Products, Prd =>
                    {
                        if (Prd.Groups?.Count > 0 && Prd.Groups.Count >= Groups.Count && Groups.All(x => Prd.Groups.Contains(x)))
                            Result.Add(Prd);
                    });
                    if (IsAscOrder)
                        return new ProductsWithTotalItemDto() { TotalItems = Result is not null ? Result.Count : 0, Products = Result?.OrderBy(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                    else
                        return new ProductsWithTotalItemDto() { TotalItems = Result is not null ? Result.Count : 0, Products = Result?.OrderByDescending(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                }
                //Groups And Tags
                else
                {
                    Parallel.ForEach(Products, Prd =>
                    {
                        if (Prd.Groups?.Count > 0 && Prd.Groups.Count >= Groups.Count &&
                           Prd.Tags?.Count > 0 && Prd.Tags.Count >= Tags.Count &&
                           Groups.All(x => Prd.Groups.Contains(x)) &&
                           Tags.All(x => Prd.Tags.Contains(x)))
                            Result.Add(Prd);
                    });
                    if (IsAscOrder)
                        return new ProductsWithTotalItemDto() { TotalItems = Result is not null ? Result.Count : 0, Products = Result?.OrderBy(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                    else
                        return new ProductsWithTotalItemDto() { TotalItems = Result is not null ? Result.Count : 0, Products = Result?.OrderByDescending(x => x.OrderInShop).Skip(PageNumber * PageCount).Take(PageCount).ToList() };
                }
            }

        }

        public async Task<List<ProductWithSlugCatDto>> GetWithCatSlug(bool IgnorePermissions = false)
        {
            using var Db = new Context();
            var Models = await Db.Products.Include(x => x.Categories).ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("There are not any products.");

            if (!IgnorePermissions)
            {
                Models = await RemoveProductsWithoutPermissionsFromLists(Models, appSettings, userRoles);
                if (Models?.Count == 0)
                    throw new ValidationException("Unfortunately you do not have permissions to see products.");
            }

            return MapToDtoWithSlugCat(Models, ManageImages.GetProductsImagesUrl(appSettings.WwwRootPath), IgnorePermissions);
        }
        public async Task<List<ProductDto>> GetByFirstCategoryInOrder()
        {
            using var Db = new Context();
            var CategoryModel = await Db.Categories.OrderBy(x => x.Order).FirstOrDefaultAsync().ConfigureAwait(false);
            var Models = await Db.Products.AsNoTracking().Include(x => x.Categories).Where(x => x.Categories.Any(x => x.CategoryId == CategoryModel.Id)).ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("In the category there are not any products.");

            Models = await RemoveProductsWithoutPermissionsFromLists(Models, appSettings, userRoles);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see products.");

            return Mapto(Models, ManageImages.GetProductsImagesUrl(appSettings.WwwRootPath));
        }

        public async Task<List<ProductDto>> GetByCategoryId(Guid CategoryId)
        {
            using var Db = new Context();
            var Models = await Db.Products.AsNoTracking().Include(x => x.Categories).Where(x => x.Categories.Any(x => x.CategoryId == CategoryId)).ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("In the category there are not any products.");

            Models = await RemoveProductsWithoutPermissionsFromLists(Models, appSettings, userRoles);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see products.");

            return Mapto(Models, ManageImages.GetProductsImagesUrl(appSettings.WwwRootPath));
        }
        public async Task<List<ProductDto>> GetByCategorySlug(string CategorySlug)
        {
            using var Db = new Context();
            var Models = await Db.Products.AsNoTracking().Include(x => x.Categories).Where(x => x.Categories.Any(x => x.CategorySlug == CategorySlug.ToLower())).ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("In the category there are not any products.");

            Models = await RemoveProductsWithoutPermissionsFromLists(Models, appSettings, userRoles);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see products.");

            return Mapto(Models, ManageImages.GetProductsImagesUrl(appSettings.WwwRootPath));
        }
        public async Task<List<ProductDto>> GetByCategoryIds(List<Guid> Ids)
        {
            using var Db = new Context();
            var Models = await Db.Products.Include(x => x.Categories).Where(x => x.Categories.Any(x => Ids.Contains(x.CategoryId))).ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("In the category there are not any products.");

            Models = await RemoveProductsWithoutPermissionsFromLists(Models, appSettings, userRoles);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see products.");


            return Mapto(Models, ManageImages.GetProductsImagesUrl(appSettings.WwwRootPath));
        }


        private List<ProductDto> Mapto(List<Product> Models, List<string> AllImagesUrl)
        {
            ConcurrentBag<ProductDto> Result = new();
            Parallel.ForEach(Models, Model =>
            {
                List<string> ImagesUrl = null;
                if (AllImagesUrl?.Count > 0)
                    ImagesUrl = AllImagesUrl.Where(x => x.StartsWith("/Images/Products/" + ManageImages.GetStartNameOfProductImageFileName(Model.Name))).ToList();

                Result.Add(MapTo(Model, ImagesUrl));
            });
            return Result.ToList();
        }

        private ProductDto MapTo(Product Model, List<string> ImagesUrl, bool IgnorePermission = false)
        {
            var data = new ProductDto()
            {
                BoxD = Model.BoxD,
                BoxH = Model.BoxH,
                BoxW = Model.BoxW,
                Cube = Model.Cube,
                D = Model.D,
                Description = Model.Description,
                ProductDescription = Model.ProductDescription,
                H = Model.H,
                Id = Model.Id,
                IsGroup = Model.IsGroup,
                Name = Model.Name,
                Order = Model.Order,
                OrderInShop = Model.OrderInShop,
                ShortDescription = Model.ShortDescription,
                W = Model.W,
                Weight = Model.Weight,
                WHQTY = Model.WHQTY,
                Prices = !string.IsNullOrWhiteSpace(Model.Price) ? System.Text.Json.JsonSerializer.Deserialize<List<ProductPriceDto>>(Model.Price) : null,
                Securities = !string.IsNullOrWhiteSpace(Model.Security) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Security) : null,
                ImagesUrls = ImagesUrl,
                Slug = Model.Slug,
                Inventory = Model.Inventory,
                CategoryIds = Model.Categories?.Select(x => x.CategoryId).ToList(),
                Tags = !string.IsNullOrWhiteSpace(Model.Tags) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Tags) : null,
                Groups = !string.IsNullOrWhiteSpace(Model.Groups) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Groups) : null,
                Factories = !string.IsNullOrWhiteSpace(Model.Factories) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Factories) : null,
                ComplexItemPieces = !string.IsNullOrWhiteSpace(Model.ComplexItemPieces) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.ComplexItemPieces) : null,
                ComplexItemPriority = Model.ComplexItemPriority,
                PiecesCount = Model.PiecesCount,
                Features = !string.IsNullOrWhiteSpace(Model.Features) ? System.Text.Json.JsonSerializer.Deserialize<List<KeyValueDto>>(Model.Features) : null,
                PricePermissions = !string.IsNullOrWhiteSpace(Model.PricePermissions) ? System.Text.Json.JsonSerializer.Deserialize<List<KeyValueDto>>(Model.PricePermissions) : null,
                IsSample = Model.IsSample
            };
            if (!IgnorePermission)
                RemoveProductPricesWhichDoNotHavePermissions(data);
            return data;
        }
        private List<ProductWithSlugCatDto> MapToDtoWithSlugCat(List<Product> Models, List<string> AllImagesUrl, bool IgnorePermission = false)
        {
            ConcurrentBag<ProductWithSlugCatDto> Result = new();
            Parallel.ForEach(Models, Model =>
            {
                List<string> ImagesUrl = null;
                if (AllImagesUrl?.Count > 0)
                    ImagesUrl = AllImagesUrl.Where(x => x.StartsWith("/Images/Products/" + ManageImages.GetStartNameOfProductImageFileName(Model.Name))).ToList();

                Result.Add(MapToDtoWithSlugCat(Model, ImagesUrl, IgnorePermission));
            });
            return Result.ToList();
        }
        private ProductWithSlugCatDto MapToDtoWithSlugCat(Product Model, List<string> ImagesUrl, bool IgnorePermission = false)
        {
            var data = new ProductWithSlugCatDto()
            {
                CategorySlug = Model.Categories.Select(x => x.CategorySlug).ToList(),
                BoxD = Model.BoxD,
                BoxH = Model.BoxH,
                BoxW = Model.BoxW,
                Cube = Model.Cube,
                D = Model.D,
                Description = Model.Description,
                ProductDescription = Model.ProductDescription,
                H = Model.H,
                Id = Model.Id,
                IsGroup = Model.IsGroup,
                Name = Model.Name,
                Order = Model.Order,
                OrderInShop = Model.OrderInShop,
                ShortDescription = Model.ShortDescription,
                W = Model.W,
                Weight = Model.Weight,
                WHQTY = Model.WHQTY,
                Prices = !string.IsNullOrWhiteSpace(Model.Price) ? System.Text.Json.JsonSerializer.Deserialize<List<ProductPriceDto>>(Model.Price) : null,
                Securities = !string.IsNullOrWhiteSpace(Model.Security) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Security) : null,
                ImagesUrls = ImagesUrl,
                Slug = Model.Slug,
                Inventory = Model.Inventory,
                CategoryIds = Model.Categories?.Select(x => x.CategoryId).ToList(),
                Tags = !string.IsNullOrWhiteSpace(Model.Tags) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Tags) : null,
                Groups = !string.IsNullOrWhiteSpace(Model.Groups) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Groups) : null,
                Factories = !string.IsNullOrWhiteSpace(Model.Factories) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Factories) : null,
                ComplexItemPieces = !string.IsNullOrWhiteSpace(Model.ComplexItemPieces) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.ComplexItemPieces) : null,
                ComplexItemPriority = Model.ComplexItemPriority,
                PiecesCount = Model.PiecesCount,
                Features = !string.IsNullOrWhiteSpace(Model.Features) ? System.Text.Json.JsonSerializer.Deserialize<List<KeyValueDto>>(Model.Features) : null,
                PricePermissions = !string.IsNullOrWhiteSpace(Model.PricePermissions) ? System.Text.Json.JsonSerializer.Deserialize<List<KeyValueDto>>(Model.PricePermissions) : null,
                IsSample = Model.IsSample
            };
            if (IgnorePermission == false)
                RemoveProductPricesWhichDoNotHavePermissions(data);
            return data;
        }
        private void RemoveProductPricesWhichDoNotHavePermissions(ProductDto data)
        {
            //بر اساس مجوز قیمت ها نمایش یا حذف می شود
            if (data.Prices?.Count > 0)
            {
                foreach (var PricePermission in data.PricePermissions)
                {
                    if (!string.IsNullOrWhiteSpace(PricePermission.Value))
                    {
                        bool found = false;
                        foreach (var userRole in userRoles)
                        {
                            if (PricePermission.Value.ToLower().IndexOf(userRole.ToLower()) > 0 || userRole.ToLower() == "admin")
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            foreach (var price in data.Prices)
                            {
                                if (string.Equals(price.Name, PricePermission.Name, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    price.Value = null;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void RemoveProductPricesWhichDoNotHavePermissions(List<ProductPriceDto> Prices, List<KeyValueDto> PricePermissions, List<string> userRoles)
        {
            //بر اساس مجوز قیمت ها نمایش یا حذف می شود
            if (Prices?.Count > 0)
            {
                foreach (var PricePermission in PricePermissions)
                {
                    if (!string.IsNullOrWhiteSpace(PricePermission.Value))
                    {
                        bool found = false;
                        foreach (var userRole in userRoles)
                        {
                            if (PricePermission.Value.ToLower().IndexOf(userRole.ToLower()) > 0 || userRole.ToLower() == "admin")
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            foreach (var price in Prices)
                            {
                                if (string.Equals(price.Name, PricePermission.Name, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    price.Value = null;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// بر اساس مجوز محصول نمایش یا مخفی می شود
        /// </summary>
        /// <param name="products"></param>
        /// <returns></returns>
        public static async Task<List<Product>> RemoveProductsWithoutPermissionsFromLists(List<Product> products, IApplicationSettings appSettings, List<string> userRoles)
        {
            if (userRoles.Any(x => string.Equals(x, "admin", StringComparison.OrdinalIgnoreCase)))
                return products;

            var catSrv = new CategoryService(appSettings, userRoles);
            var cats = await catSrv.Get().ConfigureAwait(false);
            List<Product> productsWithPermisson = null;
            if (products?.Count > 0)
            {
                productsWithPermisson = new List<Product>();
                foreach (var product in products)
                {
                    if (HasProductPermission(product, cats.Select(x => x.Id).ToList(), userRoles))
                    {
                        productsWithPermisson.Add(product);
                    }
                }
            }
            return productsWithPermisson;
        }

        public static bool HasProductPermission(Product product, List<Guid> CatIds, List<string> userRoles)
        {
            if (userRoles.Any(x => string.Equals(x, "admin", StringComparison.OrdinalIgnoreCase)))
                return true;
            if (string.IsNullOrWhiteSpace(product.Security))
                return true;
            else
            {
                bool HasCatPermission = product.Categories.Any(x => CatIds.Contains(x.CategoryId));
                if (HasCatPermission)
                {
                    string SecuritytoLower = product.Security.ToLower();
                    foreach (var userRole in userRoles)
                    {
                        if (SecuritytoLower.IndexOf("\"" + userRole.ToLower() + "\"") > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
