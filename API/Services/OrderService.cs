using Grpc.Core;
using Hangfire;
using API.Helper;
using Application.SeedWork;
using Application.Services.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class OrderService : OrderSrv.OrderSrvBase
    {
        private Application.Services.Order.OrderService service;
        private readonly ILogger<OrderService> logger;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IApplicationSettings applicationSettings;
        public OrderService(IApplicationSettings applicationSettings, ILogger<OrderService> logger, IBackgroundJobClient backgroundJobClient)
        {
            this.logger = logger;
            this.applicationSettings = applicationSettings;
            this.backgroundJobClient = backgroundJobClient;
        }

        public async override Task<OrderResponseMessage> SendOrder(OrderRequestMessage request, ServerCallContext context)
        {
            NewService(context);
            try
            {
                OrderDto Order = new();
                Order.CustomerUserName = request.CustomerUserName;
                Order.PriceType = (Application.Services.Order.PriceType)(int)request.PriceType;
                Order.Cost = request.Cost;
                Order.Delivery = (Application.Services.Order.DeliveryType)(int)request.Delivery;
                Order.Tariff = (Application.Services.Order.TariffType)(int)request.Tariff;
                Order.PoNumber = request.PoNumber;
                if (request.Orders?.Count > 0)
                {
                    Order.Orders = new List<Application.Services.Order.ProductOrder>();
                    Order.Orders.AddRange(request.Orders.Select(x => new Application.Services.Order.ProductOrder()
                    {
                        ProductSlug = x.ProductSlug,
                        Count = x.Count
                    }).ToList());
                }
                Order.ConfirmedBy = request.ConfirmedBy;
                Order.Description = request.Description;
                Order.MarketSpecial = request.MarketSpecial;
                Order.CountOfCustomerShareAContainer = request.CountOfCustomerShareAContainer;
                Order.AddDiscountToSample = request.AddDiscountToSample;

                var Name = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName).Value;
                var LastName = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname).Value;
                var Email = context.GetHttpContext().User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email).Value;

                try
                {
                    await service.SaveOrder(Order, Email, Name + " " + LastName);
                    return await Task.FromResult(new OrderResponseMessage() { Message = "Your orders is registered successfully.", IsError = false });
                }
                catch (Exception Ex)
                {
                    return await Task.FromResult(new OrderResponseMessage() { Message = Ex.Message, IsError = true });
                }

            }
            catch
            {
                return await Task.FromResult(new OrderResponseMessage() { Message = "Unfortunately, there is a problem during order process. Please try again.", IsError = true });
            }
        }
        private void NewService(ServerCallContext context)
        {
            service = new Application.Services.Order.OrderService(applicationSettings, backgroundJobClient, Tools.GetRoles(context));
        }
    }
}
