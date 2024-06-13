using System;
using System.Collections.Generic;

namespace Domain.Entity
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public double? Inventory { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// Json format
        /// </summary>
        public string Price { get; set; }
        public double? Cube { get; set; }
        public double? W { get; set; }
        public double? D { get; set; }
        public double? H { get; set; }
        public double? Weight { get; set; }
        public double? BoxW { get; set; }
        public double? BoxD { get; set; }
        public double? BoxH { get; set; }
        /// <summary>
        /// [RoleName],[RoleName],[RoleName]... Excel Format
        /// ["RoleName","RoleName","RoleName"] json => Db Format
        /// </summary>
        public string Security { get; set; }
        public string WHQTY { get; set; }
        public int Order { get; set; }
        /// <summary>
        /// False => means it is a regular product
        /// true => means it is a group of products that have a behaviour like a product
        /// </summary>
        public bool IsGroup { get; set; }
        public virtual ICollection<CategoryProduct> Categories { get; set; }
        /// <summary>
        /// [Tag],[Tag],[Tag]... Excel Format
        /// ["Tag","Tag","Tag"] json => Db Format
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// [Group],[Group],[Group]... Excel Format
        /// ["Group","Group","Group"] json => Db Format
        /// </summary>
        public string Groups { get; set; }
        /// <summary>
        /// [Factory],[Factory],[Factory]... Excel Format
        /// ["Factory","Factory","Factory"] json => Db Format
        /// </summary>
        public string Factories { get; set; }
        /// <summary>
        /// تعداد اجزا
        /// </summary>
        public int PiecesCount { get; set; }
        /// <summary>
        /// [17-00-10G],[17-00-25R]
        /// ["17-00-10G","17-00-25R"] json => Db Format
        /// </summary>
        public string ComplexItemPieces { get; set; }
        public int ComplexItemPriority { get; set; }
        public string ProductDescription { get; set; }



        /// <summary>
        /// مشخصات محصول
        /// KeyValue
        /// </summary>
        public string Features { get; set; }
        /// <summary>
        /// KeyValue
        /// </summary>
        public string PricePermissions { get; set; }
        public string IsSample { get; set; }
    }
    public class KeyValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    public class ProductPrice
    {
        public string Name { get; set; }
        public decimal? Value { get; set; }
    }
}
