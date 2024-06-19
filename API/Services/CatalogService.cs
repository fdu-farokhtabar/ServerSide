using Grpc.Core;
using API.Helper;
using Application.SeedWork;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class CatalogService : CatalogSrv.CatalogSrvBase
    {
        private Application.Services.Catalog.CatalogService service;
        private readonly IApplicationSettings applicationSettings;
        public CatalogService(IApplicationSettings applicationSettings)
        {
            this.applicationSettings = applicationSettings;
        }
        public override async Task<CatalogResponseMessage> GetLandedPriceCatalogUrl(LandedPriceCatalogRequestMessage request, ServerCallContext context)
        {
            if (request.Factor > 0)
            {
                NewService(context);
                List<string> CategoriesSlug = null;
                if (request.CatalogSlug != "All_Cat")
                {
                    CategoriesSlug = new() { request.CatalogSlug };
                }
                (string RelativePath, string ServerPath) = await service.Generate(CategoriesSlug, null, new List<int> { 0 }, false, request.Factor);
                var Result = new CatalogResponseMessage
                {
                    UrlCurrent = RelativePath,
                    UrlAll = null
                };
                return Result;
            }
            else
                throw new System.Exception("Factor Parameter is not valid. Needs to be more than zero.");


            //if (request.Factor > 0)
            //{
            //    string CurrentCatalogPath = applicationSettings.WwwRootPath + $@"\Catalogs\LandedPrices\0\{request.CatalogSlug}_0_LandedPrice_{request.Factor}.pdf";
            //    string AllCatalogPath = applicationSettings.WwwRootPath + $@"\Catalogs\LandedPrices\0\Catalog_0_LandedPrice_{request.Factor}.pdf";
            //    if (request.CatalogSlug != "All_Cat")
            //    {
            //        if (!System.IO.File.Exists(CurrentCatalogPath) || !System.IO.File.Exists(AllCatalogPath))
            //        {
            //            await catalogService.CreateWithLandedPrice(request.Factor, request.CatalogSlug);
            //        }
            //    }
            //    else
            //    {
            //        if (!System.IO.File.Exists(AllCatalogPath))
            //        {
            //            await catalogService.CreateWithLandedPrice(request.Factor, request.CatalogSlug);
            //        }
            //    }

            //    string LandedPrice = "_LandedPrice_" + request.Factor.ToString();
            //    string PriceType = "_0";
            //    var Result = new CatalogResponseMessage
            //    {
            //        UrlCurrent = $"LandedPrices/0/{request.CatalogSlug}{PriceType}{LandedPrice}.pdf",
            //        UrlAll = $"LandedPrices/0/Catalog{PriceType}{LandedPrice}.pdf"
            //    };
            //    return Result;
            //}
            //else
            //    throw new System.Exception("Factor Parameter is not valid. Needs to be more than zero.");
        }
        public override async Task<DownloadCatalogResponse> DownloadCatalog(DownloadCatalogRequest request, ServerCallContext context)
        {
            NewService(context);
            List<string> CategoriesSlug = null;
            if (!string.IsNullOrWhiteSpace(request.CategorySlug) &&
                !string.Equals(request.CategorySlug, "All_Cat", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.CategorySlug, "L", System.StringComparison.OrdinalIgnoreCase))
            {
                CategoriesSlug = new() { request.CategorySlug };
            }
            List<int> Prices = null;
            if (!string.IsNullOrWhiteSpace(request.Prices) && !string.Equals(request.Prices, "L", System.StringComparison.OrdinalIgnoreCase))
            {
                Prices = new List<int>();
                if (request.Prices.Length == 1)
                    Prices.Add(Convert.ToInt32(request.Prices));
                else
                {
                    var Prcs = request.Prices.Split("_");
                    foreach (var Prc in Prcs)
                    {
                        Prices.Add(Convert.ToInt32(Prc));
                    }
                }
            }
            return new DownloadCatalogResponse()
            {
                Url = (await service.Generate(CategoriesSlug, null, Prices, false, request.Factor)).RelativePath
            };
        }
        public override async Task<DownloadCatalogResponse> DownloadAdvanceCatalog(DownloadAdvanceCatalogRequest request, ServerCallContext context)
        {
            NewService(context);
            return new DownloadCatalogResponse()
            {
                Url = (await service.Generate(request.CategoriesSlug?.ToList(), request.Factories?.ToList(), request.Prices?.ToList(), request.JustAvailable, request.LandedPrice)).RelativePath
            };
        }
        private void NewService(ServerCallContext context)
        {
            service = new(applicationSettings, Tools.GetRoles(context));
        }
    }
}
