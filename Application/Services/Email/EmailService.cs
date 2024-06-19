using Application.Data;
using Application.SeedWork;
using Application.Services.Account;
using Application.Services.Helper;
using Application.Services.Order;
using Application.Services.Product;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services.Email
{
    public class EmailService
    {
        private readonly IApplicationSettings settings;
        private readonly EmailProvider Provider;
        public EmailService(IApplicationSettings settings)
        {
            this.settings = settings;
            Provider = new();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserFirstName"></param>
        /// <param name="UserLastName"></param>
        /// <param name="CustomerFullName"></param>
        /// <param name="CustomerEmail"></param>
        /// <param name="CatalogUrl"></param>
        /// <returns></returns>
        public async Task SendCatalog(string UserFirstName, string UserLastName, string CustomerFullName, string CustomBodyText, string CustomerEmail, string CatalogUrl)// string CategorySlug, string WhichPrice, string LandedPriceFactor = null)
        {
            if (string.IsNullOrWhiteSpace(CustomerFullName))
                throw new Exception("Please input customer name or custom text.");
            if (string.IsNullOrWhiteSpace(CustomerEmail))
                throw new Exception("Please input customer email.");
            if (!Tools.EmailIsValid(CustomerEmail))
                throw new Exception("Customer email is not valid.");
            //if (CategorySlug.Contains("/") || CategorySlug.Contains(@"\"))
            //    throw new Exception("Category name is not valid.");

            //string CatalogUrl = $"{CategorySlug}";
            //if (!string.IsNullOrWhiteSpace(LandedPriceFactor))
            //{
            //    if (!string.IsNullOrWhiteSpace(WhichPrice))
            //        CatalogUrl = $"LandedPrices/0/{CategorySlug}_0_LandedPrice_{LandedPriceFactor}";
            //    else
            //        CatalogUrl = $"LandedPrices/{WhichPrice}/{CategorySlug}_{WhichPrice}_LandedPrice_{LandedPriceFactor}";
            //}
            //else if (!string.IsNullOrWhiteSpace(WhichPrice))
            //{
            //    CatalogUrl = $"{WhichPrice}/{CategorySlug}_{WhichPrice}";
            //}

            using var Db = new Context();
            var Result = await Db.Settings.FindAsync(settings.CatalogEmailSetting).ConfigureAwait(false);
            if (Result is not null)
            {
                try
                {
                    EmailSetting Setting = System.Text.Json.JsonSerializer.Deserialize<EmailSetting>(Result.Value);
                    if (!string.IsNullOrWhiteSpace(CustomBodyText))
                    {
                        CustomBodyText = CustomBodyText.Replace(System.Environment.NewLine, "<br/>");
                        CustomBodyText = CustomBodyText.Replace("\n", "<br/>");
                    }

                    string Body = Setting.BodyTemplate.Replace("{CustomerName}", CustomerFullName).Replace("{CatalogSlug}", $"{CatalogUrl}.pdf?id={new Random(Guid.NewGuid().GetHashCode()).Next(1, 999999999)}")
                                                      .Replace("{User_FirstName}", UserFirstName)
                                                      .Replace("{User_LastName}", UserLastName)
                                                      .Replace("{Additional_Content}", CustomBodyText)
                                                      .Replace("{CurrentDate}", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());




                    if (string.IsNullOrEmpty(CatalogUrl))
                        await Provider.SendMailAsync(Setting, Setting.SubjectTemplate, CustomerEmail, Body);
                    else
                        await Provider.SendMailAsync(Setting, Setting.SubjectTemplate, CustomerEmail, Body, new List<string>() { CatalogUrl });

                }
                catch
                {
                    throw new Exception("Email setting is not valid.");
                }
            }
        }
        public async Task SendContactUs(string Name, string Family, string Phone, string Email, string Comment)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new Exception("Please input name.");
            if (string.IsNullOrWhiteSpace(Family))
                throw new Exception("Please input family.");

            if (!string.IsNullOrWhiteSpace(Phone))
            {
                if (!Tools.PhoneIsValid(Phone))
                    throw new Exception("phone is not valid.");
            }
            if (string.IsNullOrWhiteSpace(Email))
                throw new Exception("Please input email.");
            if (!Tools.EmailIsValid(Email))
                throw new Exception("email is not valid.");

            if (string.IsNullOrWhiteSpace(Comment))
                throw new Exception("Please input Comment.");

            using var Db = new Context();
            var Result = await Db.Settings.FindAsync(settings.ContactUsEmailSetting).ConfigureAwait(false);
            if (Result is not null)
            {
                try
                {
                    EmailSetting Setting = System.Text.Json.JsonSerializer.Deserialize<EmailSetting>(Result.Value);
                    string Body = Setting.BodyTemplate.Replace("{Name}", Name).Replace("{Family}", Family).Replace("{Phone}", Phone)
                                                      .Replace("{Email}", Email).Replace("{Comment}", Comment)
                                                      .Replace("{CurrentDate}", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
                    await Provider.SendMailAsync(Setting, Setting.SubjectTemplate, Setting.UserName, Body, null, Setting.Cc, Setting.Bcc);

                }
                catch
                {
                    throw new Exception("Email setting is not valid.");
                }
            }
            else
                throw new Exception("Email setting is not valid.");
        }

        public async Task SendOrder(OrderDto Order, List<Domain.Entity.Product> Products, string UserFullName, string RepEmail, CustomerDto Customer, DateTime OrderDate, string ConfirmedBy, string InvoiceNumber, string PoNumber)
        {
            string AssetCatalogPath = settings.WwwRootPath + @"\Assets\Email\";
            string OrderEmailTemplate = await File.ReadAllTextAsync($"{AssetCatalogPath}OrderEmailTemplate.html");
            string OrderEmailTableBodyTemplate = await File.ReadAllTextAsync($"{AssetCatalogPath}OrderEmailTableBodyTemplate.html");
            string PoNumberContent = "";
            if (!string.IsNullOrWhiteSpace(PoNumber))
                PoNumberContent = $"<br/><span style=\"color: black;\">P.O. Number: {PoNumber}</span>";
            OrderEmailTemplate = OrderEmailTemplate.Replace("{InvoiceNumber}", InvoiceNumber)
                                                   .Replace("{PoNumber}", PoNumberContent)
                                                   .Replace("{CustomerName}", Customer.FullName);

            string Rows = "";
            double TotalPieces = 0;
            decimal TotalPrices = 0;
            decimal totalDiscount = 0;
            double TotalCount = 0;
            double TotalCubes = 0;
            double TotalWeight = 0;
            double TotalContainers = 0;
            double TotalContainersRound = 0;
            int i = 1;
            foreach (var orderItem in Order.Orders)
            {
                var Prd = Products.Find(x => x.Slug == orderItem.ProductSlug);
                (decimal finalPrice, decimal firstPrice) = GetPrice(Prd, Order.PriceType, Order.Tariff, Order.Cost, Order.MarketSpecial, Order.AddDiscountToSample);

                decimal totalFirstPrice = firstPrice * (decimal)orderItem.Count;
                decimal totalFinalPrice = finalPrice * (decimal)orderItem.Count;

                string showPrice = Tools.GetPriceFormat(finalPrice);
                if (finalPrice != firstPrice)
                    showPrice = "<strong>" + showPrice + "</strong> " + "<s>" + Tools.GetPriceFormat(firstPrice) + "</s>";
                Rows += OrderEmailTableBodyTemplate.Replace("{Item Number}", Prd.Name)
                                                   .Replace("{Qty}", orderItem.Count.ToString())
                                                   .Replace("{Price}", showPrice)
                                                   .Replace("{Total Price}", Tools.GetPriceFormat(totalFinalPrice))
                                                   .Replace("{RowStyle}", GetRowStyle(i, Order.Orders.Count));
                TotalPrices += totalFinalPrice;
                totalDiscount += (totalFirstPrice - totalFinalPrice);
                TotalPieces += (Prd.PiecesCount * orderItem.Count);
                TotalCubes += Prd.Cube.HasValue ? Prd.Cube.Value * orderItem.Count : 0;
                TotalWeight += Prd.Weight.HasValue ? Prd.Weight.Value * orderItem.Count : 0;
                TotalCount += orderItem.Count;
                i++;
            }
            TotalCubes = Math.Round(TotalCubes, 2);
            TotalContainers = Math.Round(TotalCubes / 2400, 2);
            TotalContainersRound = Math.Ceiling(TotalCubes / 2400);
            Rows += OrderEmailTableBodyTemplate
                                   .Replace("{Item Number}", "")
                                   .Replace("{Qty}", TotalCount.ToString())
                                   .Replace("{Price}", "")
                                   .Replace("{Total Price}", Tools.GetPriceFormat(TotalPrices))
                                   .Replace("{RowStyle}", GetRowStyle(i, Order.Orders.Count));

            string discount = "$0";
            if (totalDiscount != 0)
                discount = "-" + Tools.GetPriceFormat(totalDiscount);
            OrderEmailTemplate = OrderEmailTemplate.Replace("{OrderTableBody}", Rows)
                                                    .Replace("{TotalDiscount}", discount)
                                                    .Replace("{TotalPricesBelow}", Tools.GetPriceFormat(TotalPrices))
                                                    .Replace("{TotalPieces}", TotalPieces.ToString())
                                                    .Replace("{TotalCubes}", TotalCubes.ToString())
                                                    .Replace("{TotalWeight}", TotalWeight.ToString())
                                                    .Replace("{Description}", !string.IsNullOrWhiteSpace(Order.Description) ? "Note: " + Order.Description : "");

            OrderEmailTemplate = OrderEmailTemplate.Replace("{CurrentDate}", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
            using var Db = new Context();
            var Result = await Db.Settings.FindAsync(settings.OrderEmailSetting).ConfigureAwait(false);
            if (Result is not null)
            {
                try
                {
                    EmailSetting Setting = System.Text.Json.JsonSerializer.Deserialize<EmailSetting>(Result.Value);
                    string Body = OrderEmailTemplate;
                    string CC = !string.IsNullOrWhiteSpace(Setting.Cc) ? ("," + Setting.Cc) : "";
                    if (RepEmail.Trim() != Customer.Email.Trim())
                        await Provider.SendMailAsync(Setting, Setting.SubjectTemplate, Setting.UserName, Body, null, RepEmail + "," + Customer.Email + CC, Setting.Bcc);
                    else
                        await Provider.SendMailAsync(Setting, Setting.SubjectTemplate, Setting.UserName, Body, null, RepEmail + CC, Setting.Bcc);
                }
                catch
                {
                    throw new Exception("Email setting is not valid.");
                }
            }
            else
                throw new Exception("Email setting is not valid.");
        }
        private (decimal finalPrice, decimal firstPrice) GetPrice(Domain.Entity.Product Model, PriceType Type, TariffType Tariff, double Cost, string MarketSpecial, bool AddDiscountToSample)
        {
            decimal firstPrice = 0;
            decimal finalPrice = 0;
            List<ProductPriceDto> Prices = !string.IsNullOrWhiteSpace(Model.Price) ? System.Text.Json.JsonSerializer.Deserialize<List<ProductPriceDto>>(Model.Price) : null;
            switch (Type)
            {
                //تخفیف نمایش داده نشود
                case PriceType.Fob:
                    if (Tariff == TariffType.IORCustomer && Type == PriceType.Fob)
                        finalPrice = Prices[0].Value.HasValue ? Prices[0].Value.Value - ((Prices[0].Value.Value * 20) / 100) : 0;
                    else
                        finalPrice = Prices[0].Value.HasValue ? Prices[0].Value.Value : 0;
                    firstPrice = finalPrice;
                    break;
                case PriceType.Sac:
                    if (Prices[1].Value.HasValue)
                        firstPrice = Prices[1].Value.Value;

                    if (Prices[2].Value.HasValue && Prices[2].Value.Value > 0)
                        finalPrice = Prices[2].Value.Value;
                    else
                    {
                        if (Prices[1].Value.HasValue && Prices[1].Value.Value > 0)
                        {
                            if (!string.IsNullOrWhiteSpace(MarketSpecial))
                            {
                                //Just For Las Vegas
                                //if (MarketSpecial == "1")
                                //    finalPrice = Prices[1].Value.Value - ((Prices[1].Value.Value * 5) / 100);
                                //else if (MarketSpecial == "2")
                                //    finalPrice = Prices[1].Value.Value - ((Prices[1].Value.Value * 10) / 100);
                                //else
                                finalPrice = Prices[1].Value.Value;
                            }
                            else
                                finalPrice = Prices[1].Value.Value;
                        }
                    }
                    break;
                //تخفیف نمایش داده شود
                case PriceType.LandedPrice:
                    if (Cost > 0 && Model.Cube.HasValue && Model.Cube > 0)
                    {
                        finalPrice = Prices[0].Value.Value + (decimal)(Model.Cube.Value * (Cost / 2350));
                        firstPrice = finalPrice;
                    }
                    break;
                //تخفیف نمایش داده شود
                case PriceType.Sample:
                    if (Prices[1].Value.HasValue)
                        firstPrice = Prices[1].Value.Value;

                    if (Prices[2].Value.HasValue && Prices[2].Value.Value > 0)
                        finalPrice = Prices[2].Value.Value;
                    else
                    {
                        if (Prices[1].Value.HasValue && Prices[1].Value.Value > 0)
                        {
                            if (AddDiscountToSample)
                                finalPrice = Prices[1].Value.Value - ((Prices[1].Value.Value * 10) / 100);
                            else
                                finalPrice = Prices[1].Value.Value;
                        }
                    }
                    break;
            }
            return (finalPrice, firstPrice);
        }
        private string GetRowStyle(int i, int OrdersCount)
        {
            var RowStyle = "style='border-bottom: solid black 1px;text-align: center;'";
            if (i == 1)
            {
                RowStyle = "style='padding-top:10px;border-bottom: solid black 1px;text-align: center;'";
            }
            else
            {
                if (i == OrdersCount)
                {
                    RowStyle = "style='text-align: center;padding-bottom:10px;'";
                }
            }
            if (i == 1 && i == OrdersCount)
            {
                RowStyle = "style='padding-top:10px;padding-bottom:10px;text-align: center;'";
            }
            return RowStyle;
        }
    }
}
