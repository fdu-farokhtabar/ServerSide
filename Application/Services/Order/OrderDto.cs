using System.Collections.Generic;

namespace Application.Services.Order
{
    public class OrderDto
    {
        public string CustomerUserName { get; set; }
        public PriceType PriceType { get; set; }
        public double Cost { get; set; }
        public DeliveryType Delivery { get; set; }
        public TariffType Tariff { get; set; }
        public List<ProductOrder> Orders { get; set; }
        public string ConfirmedBy { get; set; }
        public string PoNumber { get; set; }
        public string Description { get; set; }
        public string MarketSpecial { get; set; }
        public int? CountOfCustomerShareAContainer { get; set; }
        public bool AddDiscountToSample { get; set; }
    }

    public class ProductOrder
    {
        public string ProductSlug { get; set; }
        public double Count { get; set; }
    }

    public enum PriceType
    {
        Fob,
        Sac,
        //Mix Container
        LandedPrice,
        Sample
    }
    public enum DeliveryType
    {
        CustomerForwarder,
        WillCall,
        KIANUSA
    }
    public enum TariffType
    {
        IORKIAN,
        IORCustomer
    }
}
