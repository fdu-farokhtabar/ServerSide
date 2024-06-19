using Grpc.Core;
using Hangfire;
using Application.SeedWork;
using Application.Services.Helper;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class EmailService : EmailSrv.EmailSrvBase
    {
        private readonly Application.Services.Email.EmailService service;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IApplicationSettings applicationSettings;


        public EmailService(IApplicationSettings applicationSettings, IBackgroundJobClient backgroundJobClient)
        {
            service = new Application.Services.Email.EmailService(applicationSettings);
            this.backgroundJobClient = backgroundJobClient;
            this.applicationSettings = applicationSettings;
        }
        public override async Task<SendResponseMessage> SendCatalog(SendCatalogRequestMessage request, ServerCallContext context)
        {
            try
            {
                var Name = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName).Value;
                var LastName = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname).Value;
                List<int> Prices = null;
                if (!string.IsNullOrWhiteSpace(request.WhichPrice) && !string.Equals(request.WhichPrice, "L", System.StringComparison.OrdinalIgnoreCase))
                {
                    Prices = new List<int>();
                    if (request.WhichPrice.Length == 1)
                        Prices.Add(Convert.ToInt32(request.WhichPrice));
                    else
                    {
                        var Prcs = request.WhichPrice.Split("_");
                        foreach (var Prc in Prcs)
                        {
                            Prices.Add(Convert.ToInt32(Prc));
                        }
                    }
                }
                backgroundJobClient.Enqueue(() => CreateAndSend(request.CustomerFullName, request.CustomerEmail, request.CategorySlug, 0, Prices, Name, LastName, API.Helper.Tools.GetRoles(context)));
                //backgroundJobClient.Enqueue(() => service.SendCatalog(Name, LastName, request.CustomerFullName, request.CustomerEmail, request.CategorySlug, request.WhichPrice, null));
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = true });
            }
            catch
            {
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = false });
            }
        }
        public override async Task<SendResponseMessage> SendCatalogWithLandedPrice(SendCatalogWithLandedPriceRequestMessage request, ServerCallContext context)
        {
            try
            {
                var Name = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName).Value;
                var LastName = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname).Value;
                backgroundJobClient.Enqueue(() => CreateAndSend(request.CustomerFullName, request.CustomerEmail, request.CategorySlug, request.Factor, null, Name, LastName, API.Helper.Tools.GetRoles(context)));
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = true });
            }
            catch (Exception Ex)
            {
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = false });
            }
        }
        public override async Task<SendResponseMessage> SendCatalogAdvanced(SendAdvancedCatalogRequest request, ServerCallContext context)
        {
            try
            {
                var roles = API.Helper.Tools.GetRoles(context);
                var Name = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName).Value;
                var LastName = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname).Value;

                var catalogService = new Application.Services.Catalog.CatalogService(applicationSettings, roles);

                var Path = (await catalogService.Generate(request.CategoriesSlug?.ToList(), request.Factories?.ToList(), request.Prices?.ToList(), request.JustAvailable, request.LandedPrice)).ServerPath;
                backgroundJobClient.Enqueue(() => service.SendCatalog(Name, LastName, request.CustomerFullName, request.CustomBodyText, request.CustomerEmail, Path));
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = true });
            }
            catch
            {
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = false });
            }
        }
        [AllowAnonymous]
        public override async Task<SendResponseMessage> SendContactUs(SendContactUsRequestMessage request, ServerCallContext context)
        {
            try
            {
                backgroundJobClient.Enqueue(() => service.SendContactUs(request.Name, request.Family, request.Phone, request.Email, request.Comment));
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = true });
            }
            catch
            {
                return await Task.FromResult(new SendResponseMessage() { PutInEmailQueue = false });
            }
        }
        public async Task CreateAndSend(string CustomerFullName, string CustomerEmail, string CategorySlug, double Factor, List<int> Prices, string Name, string LastName, List<string> Roles)
        {
            if (string.IsNullOrWhiteSpace(CustomerFullName))
                throw new Exception("Please input customer name.");
            if (string.IsNullOrWhiteSpace(CustomerEmail))
                throw new Exception("Please input customer email.");
            if (!Tools.EmailIsValid(CustomerEmail))
                throw new Exception("Customer email is not valid.");
            if (CategorySlug.Contains("/") || CategorySlug.Contains(@"\"))
                throw new Exception("Category name is not valid.");

            List<string> CategoriesSlug = null;
            if (!string.Equals(CategorySlug, "All_Cat", StringComparison.OrdinalIgnoreCase) && !string.Equals(CategorySlug, "Catalog", StringComparison.OrdinalIgnoreCase))
            {
                CategoriesSlug = new() { CategorySlug };
            }
            var catalogService = new Application.Services.Catalog.CatalogService(applicationSettings, Roles);
            string Path = (await catalogService.Generate(CategoriesSlug, null, Prices, false, Factor)).ServerPath;


            //string CatalogsPath = applicationSettings.WwwRootPath + $@"\Catalogs\LandedPrices\0\{request.CategorySlug}_0_LandedPrice_{request.Factor}.pdf";
            //if (!System.IO.File.Exists(CatalogsPath))
            //  await catalogService.CreateWithLandedPrice(request.Factor, request.CategorySlug);

            //await service.SendCatalog(Name, LastName, request.CustomerFullName, request.CustomerEmail, request.CategorySlug, "0", request.Factor.ToString());

            await service.SendCatalog(Name, LastName, CustomerFullName, null, CustomerEmail, Path);
        }
    }
}
