using Hangfire;
using Application.Data;
using Application.SeedWork;
using Application.Services.Account;
using Application.Services.Email;
using Application.Services.Helper.Products;
using Application.Services.Product;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Order
{
    public class OrderService
    {
        private readonly IApplicationSettings appSettings;
        private readonly IBackgroundJobClient BackgroundJobClient;
        private readonly List<string> userRoles;
        public OrderService(IApplicationSettings appSettings, IBackgroundJobClient backgroundJobClient, List<string> userRoles)
        {
            this.appSettings = appSettings;
            BackgroundJobClient = backgroundJobClient;
            this.userRoles = userRoles;
        }
        public async Task SaveOrder(OrderDto Order, string UserEmail, string UserFullName)
        {
            //:TODO Check User role for User request Which has access to the price sample for order or not
            IsValidPriceType(Order);

            DateTime Current = DateTime.Now;
            if (Order.Orders?.Count > 0)
            {
                var Ac = new AccountService();
                var Customer = await Ac.GetEmail(Order.CustomerUserName);
                if (Customer is not null)
                {
                    //Merge sum of repeated products which are ordered
                    var Orders = Order.Orders.GroupBy(x => x.ProductSlug).Select(x => new ProductOrder()
                    {
                        ProductSlug = x.Key,
                        Count = x.Sum(y => y.Count)
                    }).ToList();

                    //var PrSrv = new ProductService(appSettings);
                    List<Domain.Entity.Product> Products = null;
                    List<Domain.Entity.Product> SelectedProducts = null;
                    using var Db = new Context();
                    Products = await Db.Products.Include(x => x.Categories).ToListAsync().ConfigureAwait(false);
                    Products = await ProductService.RemoveProductsWithoutPermissionsFromLists(Products, appSettings, userRoles);

                    SelectedProducts = Products.Where(x => Orders.Any(y => y.ProductSlug == x.Slug)).ToList();
                    if (SelectedProducts?.Count > 0 && SelectedProducts.Count == Orders.Count)
                    {
                        CheckAllProductsHavePrice(Order, SelectedProducts);
                        CheckFactoriesIfPriceIsFob(Order, SelectedProducts);
                        CheckCategoriesWhichNoMoreThanTwoInFobMode(Order, SelectedProducts);
                        CheckAllProductsAreSelectedCorrectlyInSampleMode(Order, SelectedProducts);
                        CheckContainersBasedOnCubeInMixContainer(Order, SelectedProducts);
                        if (Order.PriceType == PriceType.Sac && Order.PriceType == PriceType.LandedPrice)
                        {
                            //اول محصولات ترکیبی که اولویت صفر دارند
                            OrdersComplexItemWithNoPriority(Products, SelectedProducts, Orders);
                            //سفارش محصولات ساده
                            OrdersSimpleItem(SelectedProducts, Orders);
                            //سفارش محصولات ترکیبی
                            OrdersComplexItem(Products, SelectedProducts, Orders);
                            //به روز رسانی وضعیت موجودی محصولات
                            InventoryManager.SetWHQTY(Products);
                            await Db.SaveChangesAsync();
                        }
                        string InvoiceNumber = await GenerateNewInvoiceNumber();
                        var Es = new EmailService(appSettings);
                        BackgroundJobClient.Enqueue(() => Es.SendOrder(Order, SelectedProducts, UserFullName, UserEmail, Customer, Current, Order.ConfirmedBy, InvoiceNumber, Order.PoNumber));
                    }
                    else
                        throw new System.Exception("All items are not available.");
                }
                else
                    throw new System.Exception("Customer is not found.");
            }
            else
                throw new System.Exception("There are not any orders. Please select your items");

        }
        private void OrdersComplexItemWithNoPriority(List<Domain.Entity.Product> Products, List<Domain.Entity.Product> OrderedProducts, List<ProductOrder> OrdersCount)
        {
            var OrderedComplexItemsWithNoPriority = OrderedProducts.Where(x => !string.IsNullOrWhiteSpace(x.ComplexItemPieces) && x.ComplexItemPriority == 0).ToList();
            if (OrderedComplexItemsWithNoPriority?.Count > 0)
            {
                foreach (var OrderedComplexItemWithNoPriority in OrderedComplexItemsWithNoPriority)
                {
                    double OrderCount = OrdersCount.Find(x => x.ProductSlug == OrderedComplexItemWithNoPriority.Slug).Count;
                    List<string> PiecesNames = null;
                    try
                    {
                        PiecesNames = System.Text.Json.JsonSerializer.Deserialize<List<string>>(OrderedComplexItemWithNoPriority.ComplexItemPieces);
                    }
                    catch
                    {

                    }
                    if (PiecesNames?.Count > 0)
                    {
                        foreach (var PieceName in PiecesNames)
                        {
                            var Prd = Products.Find(x => x.Name == PieceName);
                            Prd.Inventory -= OrderCount;
                            if (Prd.Inventory < 0)
                                throw new System.Exception($"Unfortunately, the product({OrderedComplexItemWithNoPriority.Name}) is out of stock!");
                        }
                    }
                    else
                        throw new System.Exception($"Unfortunately, the product({OrderedComplexItemWithNoPriority.Name}) is not available! because there is not any information about pieces of this item.");
                }
            }
        }
        private void OrdersSimpleItem(List<Domain.Entity.Product> OrderedProducts, List<ProductOrder> OrdersCount)
        {
            var OrderedSimpleItems = OrderedProducts.Where(x => string.IsNullOrWhiteSpace(x.ComplexItemPieces)).ToList();
            if (OrderedSimpleItems?.Count > 0)
            {
                foreach (var OrderedComplexItem in OrderedSimpleItems)
                {
                    double OrderCount = OrdersCount.Find(x => x.ProductSlug == OrderedComplexItem.Slug).Count;
                    OrderedComplexItem.Inventory -= OrderCount;
                    if (OrderedComplexItem.Inventory < 0)
                        throw new System.Exception($"Unfortunately, the product({OrderedComplexItem.Name}) is out of stock!");
                }
            }
        }
        private void OrdersComplexItem(List<Domain.Entity.Product> Products, List<Domain.Entity.Product> OrderedProducts, List<ProductOrder> OrdersCount)
        {
            var OrderedComplexItems = OrderedProducts.Where(x => !string.IsNullOrWhiteSpace(x.ComplexItemPieces) && x.ComplexItemPriority > 0).ToList();
            if (OrderedComplexItems?.Count > 0)
            {
                var OrderedComplexItemsWithOrderCount = OrderedComplexItems.Select(x => new { Product = x, Count = OrdersCount.Find(y => y.ProductSlug == x.Slug).Count }).OrderBy(x => (x.Product.Inventory - x.Count)).ToList();

                foreach (var OrderedComplexItemWithOrderCount in OrderedComplexItemsWithOrderCount)
                {
                    double OrderCount = OrderedComplexItemWithOrderCount.Count;
                    var Peices = InventoryManager.FindPieces(Products, OrderedComplexItemWithOrderCount.Product);
                    var Max = InventoryManager.ComputeMaxInventoryForComplexItem(Peices, OrderCount);
                    if (Max > 0)
                    {
                        foreach (var Peice in Peices)
                            Peice.Item.Inventory -= (Max * Peice.Count);
                    }
                    //اول از آیتم ها سعی می کنیم محصول ترکیبی را سفارش دهیم
                    var RemindCount = OrderCount - Max;
                    //اگر باز هم ماند از رزرو
                    if (RemindCount >= 1)
                    {
                        OrderedComplexItemWithOrderCount.Product.Inventory -= RemindCount;
                    }
                    if (OrderedComplexItemWithOrderCount.Product.Inventory < 8)
                        throw new System.Exception($"Unfortunately, the product({OrderedComplexItemWithOrderCount.Product.Name}) is out of stock!");
                }
            }
        }

        private void IsValidPriceType(OrderDto Order)
        {
            if (Order.PriceType == PriceType.Fob || Order.PriceType == PriceType.LandedPrice)
            {
                if (Order.Delivery == DeliveryType.WillCall)
                    throw new Exception("When price is Fob, delivery cannot be WillCall.");
                //throw new Exception("When price is Fob or LandedPrice, delivery cannot be WillCall.");
            }
            //if (Order.PriceType == PriceType.Sac)
            //{
            //    if (Order.Delivery == DeliveryType.CustomerForwarder)
            //        throw new Exception("When price is Sac, delivery cannot be CustomerForwarder.");
            //}
            //if (Order.PriceType != PriceType.Fob)
            //{
            //    if (Order.Tariff == TariffType.IORCustomer)
            //        throw new Exception("When price is Sac or LandedPrice, Tariff cannot be IORCustomer.");
            //}
            if (Order.PriceType == PriceType.LandedPrice)
            {
                if (Order.Cost == 0)
                    throw new Exception("When price is LandedPrice, Freight cannot be zero.");
            }

        }
        /// <summary>
        /// بررسی میزان سفارش که نباید از حد خاصی کمتر باشد در حالت
        /// Mix Container
        /// </summary>
        /// <param name="Order"></param>
        /// <param name="SelectedProducts"></param>
        /// <exception cref="Exception"></exception>
        private void CheckContainersBasedOnCubeInMixContainer(OrderDto Order, List<Domain.Entity.Product> SelectedProducts)
        {
            //Mix Container
            if (Order.PriceType == PriceType.LandedPrice)
            {
                double TotalCubes = 0;
                foreach (var orderItem in Order.Orders)
                {
                    var Prd = SelectedProducts.Find(x => x.Slug == orderItem.ProductSlug);
                    TotalCubes += Prd.Cube.HasValue ? Prd.Cube.Value * orderItem.Count : 0;
                }
                TotalCubes = Math.Round(TotalCubes, 2);
                int MinContainerCapacity = Order.CountOfCustomerShareAContainer.HasValue ? Order.CountOfCustomerShareAContainer.Value : 3400;
                if (TotalCubes < MinContainerCapacity)
                    throw new Exception($"The minimum order of 'Mix container landed to Door' container  is {MinContainerCapacity}");
            }
        }

        /// <summary>
        /// برای سفارش نوع چعارم استفاده می شود  و در صورتی 1 باشد یعنی اجازه سفارش دارد 
        /// </summary>
        private void CheckAllProductsAreSelectedCorrectlyInSampleMode(OrderDto Order, List<Domain.Entity.Product> SelectedProducts)
        {
            if (Order.PriceType == PriceType.Sample)
            {
                if (SelectedProducts.Any(x => string.IsNullOrWhiteSpace(x.IsSample) || x.IsSample.Trim() != "1"))
                    throw new Exception("In Sample, Unfortunately, you cannot order these products.");
            }
        }

        private void CheckCategoriesWhichNoMoreThanTwoInFobMode(OrderDto Order, List<Domain.Entity.Product> SelectedProducts)
        {
            if (Order.PriceType == PriceType.Fob)
            {
                List<Guid> categoriesId = new();
                foreach (var prd in SelectedProducts)
                {
                    foreach (var cat in prd.Categories)
                    {
                        if (!categoriesId.Any(x => x == cat.CategoryId))
                            categoriesId.Add(cat.CategoryId);
                    }
                }
                if (categoriesId.Count > 2)
                    throw new Exception("In China Price, You cannot add Items from more than two categories.");
            }

        }
        private void CheckFactoriesIfPriceIsFob(OrderDto Order, List<Domain.Entity.Product> SelectedProducts)
        {
            if (Order.PriceType == PriceType.Fob)
            {
                var StringFactories = SelectedProducts.Select(x => x.Factories).ToList().Distinct();
                List<string> Factories = new();
                foreach (var StringFactory in StringFactories)
                {
                    List<string> CurrentFactories = null;
                    if (!string.IsNullOrWhiteSpace(StringFactory))
                    {
                        try
                        {
                            CurrentFactories = System.Text.Json.JsonSerializer.Deserialize<List<string>>(StringFactory.Trim());
                        }
                        catch
                        {

                        }
                    }
                    if (CurrentFactories?.Count > 0)
                        Factories.AddRange(CurrentFactories.Select(x => x.ToLower()).ToList());
                }
                var FactoriesCounter = Factories?.Distinct().Select(x => new FactoryCounter() { Count = 0, Value = x }).ToList();

                foreach (var Product in SelectedProducts)
                {
                    foreach (var FactoryCounter in FactoriesCounter)
                    {
                        if (Product.Factories.ToLower().Contains(FactoryCounter.Value))
                            FactoryCounter.Count++;
                    }
                }
                //اگر price از نوع Fob باشد بایستی حتما از یک مدل Factories انتخاب شود                
                bool IsOK = false;
                foreach (var FactoryCounter in FactoriesCounter)
                {
                    if (SelectedProducts.Count == FactoryCounter.Count)
                    {
                        IsOK = true;
                        break;
                    }
                }
                if (!IsOK)
                    throw new Exception("When price is Fob, All factories of products must be the same.");
            }
        }
        private void CheckAllProductsHavePrice(OrderDto Order, List<Domain.Entity.Product> SelectedProducts)
        {
            string ProductsName = "";
            foreach (var Product in SelectedProducts)
            {
                List<ProductPriceDto> Prices = null;
                List<KeyValueDto> PricePermissions = null;

                if (!string.IsNullOrWhiteSpace(Product.Price))
                {
                    try
                    {
                        Prices = System.Text.Json.JsonSerializer.Deserialize<List<ProductPriceDto>>(Product.Price);
                        PricePermissions = System.Text.Json.JsonSerializer.Deserialize<List<KeyValueDto>>(Product.PricePermissions);
                        ProductService.RemoveProductPricesWhichDoNotHavePermissions(Prices, PricePermissions, userRoles);
                    }
                    catch
                    {

                    }
                }
                if (Prices?.Count > 0)
                {
                    switch (Order.PriceType)
                    {

                        case PriceType.Fob:
                        case PriceType.LandedPrice:
                            if (Prices[0] is null || !Prices[0].Value.HasValue)
                                ProductsName = Product.Name + ", ";
                            break;
                        case PriceType.Sac:
                        case PriceType.Sample:
                            if ((Prices[1] is null || !Prices[1].Value.HasValue) && (Prices[2] is null || !Prices[2].Value.HasValue))
                                ProductsName = Product.Name + ", ";
                            break;
                    }
                }
                else
                {
                    ProductsName = Product.Name + ", ";
                }
            }
            if (ProductsName != "")
            {
                ProductsName = ProductsName[0..^2];
                throw new Exception($"Unfortunately, you cannot order these products({ProductsName}) due to lack of prices.");
            }
        }
        //private void CheckForMinimumContainerThreshhold(OrderDto Order)
        //{
        //    if (Order.PriceType == PriceType.Fob)
        //    {

        //    } 
        //}
        private async Task<string> GenerateNewInvoiceNumber()
        {
            using var Db = new Context();
            long NewInvoiceNumber = 1;
            var LastInvoiceNumber = await Db.Settings.FirstOrDefaultAsync(x => x.Key == "InvoiceNumber").ConfigureAwait(false);
            if (LastInvoiceNumber is not null)
            {
                var PrevInvoiceNumber = Convert.ToInt64(LastInvoiceNumber.Value);
                NewInvoiceNumber = PrevInvoiceNumber + 1;
                LastInvoiceNumber.Value = NewInvoiceNumber.ToString();
            }
            else
            {
                Db.Settings.Add(new Domain.Entity.Setting()
                {
                    Key = "InvoiceNumber",
                    Value = "1"
                });
            }
            await Db.SaveChangesAsync();
            return "WH" + NewInvoiceNumber.ToString("000000");
        }
    }

    public class FactoryCounter
    {
        public string Value { get; set; }
        public int Count { get; set; }
    }
}
