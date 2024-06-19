using Application.Data;
using Application.SeedWork;
using Application.Services.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Category
{
    using Application.Services.Account;
    using Domain.Entity;
    using System.Collections.Concurrent;

    public class CategoryService
    {
        private readonly IApplicationSettings appSettings;
        private readonly List<string> userRoles;
        private readonly AuthorizationService Auth;
        public CategoryService(IApplicationSettings appSettings, List<string> userRoles)
        {
            this.appSettings = appSettings;
            this.userRoles = userRoles;
            Auth = new AuthorizationService(userRoles);
        }
        public async Task<CategoryDto> GetFirstByOrder()
        {
            using var Db = new Context();
            var Model = await Db.Categories.OrderBy(x => x.Order).FirstOrDefaultAsync().ConfigureAwait(false);
            if (Model is null)
                throw new ValidationException("There are not any Categories.");
            if (!Auth.HasUserPermissionToUseData(Model.Security))
                throw new ValidationException("Unfortunately you do not have permissions to see the category");

            var Children = await Db.CategoryCategories.Where(x => x.ParentCategoryId == Model.Id).ToListAsync();
            var ImagesUrl = ManageImages.GetCategoryImagesUrl(Model.Name, appSettings.WwwRootPath);
            return MapTo(Model, ImagesUrl, Children);
        }
        public async Task<List<CategoryDto>> GetBySlugWithChildren(string Slug)
        {
            using var Db = new Context();
            var Model = await Db.Categories.Where(x => x.Slug == Slug.ToLower().Trim()).FirstOrDefaultAsync().ConfigureAwait(false);
            if (Model is null)
                throw new ValidationException("Category does not exist.");
            var Children = await Db.CategoryCategories.Where(x => x.ParentCategorySlug == Slug.ToLower().Trim()).ToListAsync();
            var ChildrenIds = Children.ConvertAll(x => x.CategoryId);
            var Models = await Db.Categories.Where(x => ChildrenIds.Contains(x.Id)).ToListAsync().ConfigureAwait(false);
            if (Models?.Count > 0)
            {
                foreach (var ChildModel in Models)
                {
                    var ChildData = Children.FirstOrDefault(x => x.CategoryId == ChildModel.Id);
                    ChildModel.Order = ChildData.Order;
                }
            }
            else
                Models = new List<Category>();
            Model.Order = -1;
            Models.Add(Model);

            Models = RemoveCategoriesWithoutPermissionsFromLists(Models);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see these categories.");



            var AllImagesUrl = ManageImages.GetCategoriesImagesUrl(appSettings.WwwRootPath);
            ConcurrentBag<CategoryDto> Result = new();
            Parallel.ForEach(Models, Model =>
            {
                List<string> ImagesUrl = null;
                if (AllImagesUrl?.Count > 0)
                    ImagesUrl = AllImagesUrl.Where(x => x.StartsWith("/Images/Products/" + ManageImages.GetStartNameOfCategoryImageFileName(Model.Name))).ToList();
                Result.Add(MapTo(Model, ImagesUrl, null));
            });
            return Result.ToList();
        }
        public async Task<CategoryDto> Get(string Slug)
        {
            using var Db = new Context();
            var Model = await Db.Categories.Where(x => x.Slug == Slug.ToLower().Trim()).FirstOrDefaultAsync().ConfigureAwait(false);
            if (Model is null)
                throw new ValidationException("Category does not exist.");
            if (!Auth.HasUserPermissionToUseData(Model.Security))
                throw new ValidationException("Unfortunately you do not have permissions to see the category");

            var Children = await Db.CategoryCategories.Where(x => x.ParentCategorySlug == Slug.ToLower().Trim()).ToListAsync();
            var ImagesUrl = ManageImages.GetCategoryImagesUrl(Model.Name, appSettings.WwwRootPath);
            return MapTo(Model, ImagesUrl, Children);
        }
        public async Task<CategoryDto> Get(Guid Id)
        {
            using var Db = new Context();
            var Model = await Db.Categories.FindAsync(Id).ConfigureAwait(false);
            if (Model is null)
                throw new ValidationException("Category does not exist.");
            if (!Auth.HasUserPermissionToUseData(Model.Security))
                throw new ValidationException("Unfortunately you do not have permissions to see the category");

            var Children = await Db.CategoryCategories.Where(x => x.ParentCategoryId == Id).ToListAsync();
            var ImagesUrl = ManageImages.GetCategoryImagesUrl(Model.Name, appSettings.WwwRootPath);
            return MapTo(Model, ImagesUrl, Children);
        }
        public async Task<List<CategoryDto>> Get(bool IgnorePermissions = false)
        {
            using var Db = new Context();
            var Models = await Db.Categories.ToListAsync().ConfigureAwait(false);
            if (Models?.Count == 0)
                throw new ValidationException("There are not any Category.");
            if (!IgnorePermissions)
            {
                Models = RemoveCategoriesWithoutPermissionsFromLists(Models);
                if (Models?.Count == 0)
                    throw new ValidationException("Unfortunately you do not have permissions to see these categories.");
            }

            var AllImagesUrl = ManageImages.GetCategoriesImagesUrl(appSettings.WwwRootPath);
            ConcurrentBag<CategoryDto> Result = new();
            Parallel.ForEach(Models, Model =>
            {
                List<string> ImagesUrl = null;
                if (AllImagesUrl?.Count > 0)
                    ImagesUrl = AllImagesUrl.Where(x => x.StartsWith("/Images/Products/" + ManageImages.GetStartNameOfCategoryImageFileName(Model.Name))).ToList();
                Result.Add(MapTo(Model, ImagesUrl, null));
            });
            return Result.OrderBy(x => x.Order).ToList();
        }
        public async Task<List<CategoryDto>> GetWithChildren()
        {
            using var Db = new Context();
            var Models = await Db.Categories.Include(x => x.Parents).ToListAsync().ConfigureAwait(false);
            //string Queryable = @"WITH RECURSIVE CategoryHierarchy AS (
            //                    -- Base case: Select all categories that have no parent category
            //                    SELECT 
            //                        c.""Id"",
            //                        c.""Name"",
            //                        c.""Slug"",
            //                        c.""ShortDescription"",
            //                        c.""Description"",
            //                        c.""Parameter"",
            //                        c.""Order"",
            //                        c.""PublishedCatalogType"",
            //                        c.""Tags"",
            //                        c.""Security"",
            //                        NULL::uuid AS ""ParentCategoryId"",
            //                        NULL::varchar(200) AS ""ParentCategorySlug""
            //                    FROM 
            //                        public.""Category"" c
            //                    LEFT JOIN 
            //                        public.""CategoryCategory"" cc ON c.""Id"" = cc.""CategoryId""
            //                    WHERE 
            //                        cc.""CategoryId"" IS NULL

            //                    UNION ALL

            //                    -- Recursive part: Select categories and their parent categories
            //                    SELECT 
            //                        c.""Id"",
            //                        c.""Name"",
            //                        c.""Slug"",
            //                        c.""ShortDescription"",
            //                        c.""Description"",
            //                        c.""Parameter"",
            //                        c.""Order"",
            //                        c.""PublishedCatalogType"",
            //                        c.""Tags"",
            //                        c.""Security"",
            //                        cc.""ParentCategoryId"",
            //                        pc.""Slug"" AS ""ParentCategorySlug""
            //                    FROM 
            //                        public.""Category"" c
            //                    INNER JOIN 
            //                        public.""CategoryCategory"" cc ON c.""Id"" = cc.""CategoryId""
            //                    INNER JOIN 
            //                        CategoryHierarchy pc ON cc.""ParentCategoryId"" = pc.""Id""
            //                )

            //                -- Select all categories and their parent categories
            //                SELECT 
            //                    ""Id"",
            //                    ""Name"",
            //                    ""Slug"",
            //                    ""ShortDescription"",
            //                    ""Description"",
            //                    ""Parameter"",
            //                    ""Order"",
            //                    ""PublishedCatalogType"",
            //                    ""Tags"",
            //                    ""Security"",
            //                    ""ParentCategoryId"",
            //                    ""ParentCategorySlug""
            //                FROM 
            //                    CategoryHierarchy;
            //                ";
            //var Models = await Db.Categories.FromSqlRaw(Queryable).ToListAsync().ConfigureAwait(false);

            if (Models?.Count == 0)
                throw new ValidationException("There are not any Category.");
            Models = RemoveCategoriesWithoutPermissionsFromLists(Models);
            if (Models?.Count == 0)
                throw new ValidationException("Unfortunately you do not have permissions to see these categories.");

            var AllImagesUrl = ManageImages.GetCategoriesImagesUrl(appSettings.WwwRootPath);
            ConcurrentBag<CategoryDto> Result = new();
            Parallel.ForEach(Models, Model =>
            {
                List<string> ImagesUrl = null;
                if (AllImagesUrl?.Count > 0)
                    ImagesUrl = AllImagesUrl.Where(x => x.StartsWith("/Images/Products/" + ManageImages.GetStartNameOfCategoryImageFileName(Model.Name))).ToList();
                var s = Models.Where(x => x.Parents.Any(x => x.ParentCategoryId == Model.Id)).Select(y => y.Parents.Where(y => y.ParentCategoryId == Model.Id).First()).ToList();
                Result.Add(MapTo(Model, ImagesUrl, s));
            });
            return Result.OrderBy(x => x.Order).ToList();
        }

        public async Task<List<CategoryDto>> GetByTags(List<string> Tags)
        {
            List<CategoryDto> Cats = await GetWithChildren();
            ConcurrentBag<CategoryDto> Result = new();
            if (Tags?.Count > 0)
            {
                Parallel.ForEach(Cats, Cat =>
                {
                    if (Cat.Tags?.Count > 0 && Cat.Tags.Count >= Tags.Count && Cat.Tags.All(x => Tags.Contains(x)))
                    {
                        //Super Cat
                        if (Cat.Children?.Count > 0)
                        {
                            var subCats = Cats.Where(x => Cat.Children.Select(x => x.Id).Contains(x.Id)).ToList();
                            foreach (var subCat in subCats)
                                Result.Add(subCat);
                        }
                        //Cat
                        else
                            Result.Add(Cat);
                    }
                });
            }
            else
            {
                Parallel.ForEach(Cats, Cat =>
                {
                    //Super Cat
                    if (Cat.Children?.Count > 0)
                    {
                        var subCats = Cats.Where(x => Cat.Children.Select(x => x.Id).Contains(x.Id)).ToList();
                        foreach (var subCat in subCats)
                            Result.Add(subCat);
                    }
                    //Cat
                    else
                        Result.Add(Cat);
                });
            }
            //Delete Repitive data
            return Result.OrderBy(x => x.Order).Distinct().ToList();
        }

        public async Task<List<CategoryShortDto>> GetShortData(bool IgnorePermission = false)
        {
            using var Db = new Context();
            var Result = await Db.Categories.Select(x => new CategoryShortDto { Id = x.Id, Name = x.Name, Slug = x.Slug, Order = x.Order, ShortDescription = x.ShortDescription, Security = x.Security }).ToListAsync().ConfigureAwait(false);
            if (Result?.Count == 0)
                throw new ValidationException("There are not any Categories.");
            if (!IgnorePermission)
            {
                List<CategoryShortDto> categoriesWithPermisson = new();
                foreach (var category in Result)
                {
                    if (Auth.HasUserPermissionToUseData(category.Security))
                        categoriesWithPermisson.Add(category);
                }
                if (categoriesWithPermisson.Count == 0)
                    throw new ValidationException("Unfortunately you do not have permissions to see these categories.");
                return categoriesWithPermisson;
            }
            else
                return Result;
        }

        private CategoryDto MapTo(Category Model, List<string> ImagesUrl, List<CategoryCategory> Children)
        {
            return new()
            {
                Id = Model.Id,
                Name = Model.Name,
                Slug = Model.Slug,
                Order = Model.Order,
                Description = Model.Description,
                ShortDescription = Model.ShortDescription,
                Parameters = !string.IsNullOrWhiteSpace(Model.Parameter) ? (System.Text.Json.JsonSerializer.Deserialize<List<CategoryParameter>>(Model.Parameter))?.Where(x => x.IsFeature == false)?.ToList().Select(x => new CategoryParameterDto { Name = x.Name, Value = x.Value }).ToList() : null,
                Features = !string.IsNullOrWhiteSpace(Model.Parameter) ? (System.Text.Json.JsonSerializer.Deserialize<List<CategoryParameter>>(Model.Parameter))?.Where(x => x.IsFeature == true)?.ToList().Select(x => new CategoryParameterDto { Name = x.Name, Value = x.Value }).ToList() : null,
                ImagesUrl = ImagesUrl,
                Children = Children?.ConvertAll(x => new ChildCategoryDto() { Id = x.CategoryId, Slug = x.CategorySlug, Order = x.Order }),
                PublishedCatalogType = (PublishedCatalogTypeDto)(int)Model.PublishedCatalogType,
                Tags = !string.IsNullOrWhiteSpace(Model.Tags) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Tags) : null,
                Securities = !string.IsNullOrWhiteSpace(Model.Security) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Model.Security) : null
            };
        }
        private List<Category> RemoveCategoriesWithoutPermissionsFromLists(List<Category> categories)
        {
            if (userRoles.Any(x => string.Equals(x, "admin", StringComparison.OrdinalIgnoreCase)))
                return categories;

            List<Category> categoriesWithPermisson = null;
            if (categories?.Count > 0)
            {
                categoriesWithPermisson = new List<Category>();
                foreach (var category in categories)
                {
                    if (Auth.HasUserPermissionToUseData(category.Security))
                        categoriesWithPermisson.Add(category);
                }
            }
            return categoriesWithPermisson;
        }

        //private bool HasCategoryPermission(string security)
        //{
        //    if (userRoles.Any(x => string.Equals(x, "admin", StringComparison.OrdinalIgnoreCase)))
        //        return true;

        //    if (string.IsNullOrWhiteSpace(security))
        //        return true;
        //    else
        //    {
        //        string SecuritytoLower = security.ToLower();
        //        foreach (var userRole in userRoles)
        //        {
        //            if (SecuritytoLower.IndexOf("\"" + userRole.ToLower() + "\"") > 0)
        //                return true;
        //        }
        //    }
        //    return false;
        //}
    }
}
