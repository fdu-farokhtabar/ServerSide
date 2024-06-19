using System;
using System.Collections.Generic;

namespace Application.Services.Product
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public List<Guid> CategoryIds { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public double? Inventory { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// توضیحات برای صفحه تکی محصول
        /// </summary>
        public string ProductDescription { get; set; }
        /// <summary>
        /// Json format
        /// </summary>
        public List<ProductPriceDto> Prices { get; set; }
        public double? Cube { get; set; }
        public double? W { get; set; }
        public double? D { get; set; }
        public double? H { get; set; }
        public double? Weight { get; set; }
        public double? BoxW { get; set; }
        public double? BoxD { get; set; }
        public double? BoxH { get; set; }
        public List<string> Securities { get; set; }
        public string WHQTY { get; set; }
        public int Order { get; set; }
        public int OrderInShop { get; set; }
        public List<string> ImagesUrls { get; set; }
        /// <summary>
        /// False => means it is a regular product
        /// true => means it is a group of products that have a behaviour like a product
        /// </summary>
        public bool IsGroup { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Groups { get; set; }
        public List<string> Factories { get; set; }
        /// <summary>
        /// تعداد اجزا
        /// </summary>
        public int PiecesCount { get; set; }
        /// <summary>
        /// اجزا این محصول
        /// دقت شود لیست محصولات است
        /// [17-00-10G],[17-00-25R]
        /// ["17-00-10G","17-00-25R"] json => Db Format
        /// </summary>
        public List<string> ComplexItemPieces { get; set; }
        /// <summary>
        /// اولویت اجاد محصولات پیچیده
        /// </summary>
        public int ComplexItemPriority { get; set; }

        public List<KeyValueDto> Features { get; set; }
        public List<KeyValueDto> PricePermissions { get; set; }
        /// <summary>
        /// برای سفارش نوع چعارم استفاده می شود  و در صورتی 1 باشد یعنی اجازه سفارش دارد 
        /// </summary>
        public string IsSample { get; set; }
    }
    public class ProductPriceDto
    {
        public string Name { get; set; }
        public decimal? Value { get; set; }
    }
    public class KeyValueDto
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
