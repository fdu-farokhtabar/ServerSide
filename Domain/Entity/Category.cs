using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entity
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public PublishedCatalogType PublishedCatalogType { get; set; }
        /// <summary>
        /// Json Format
        /// Parameter => Parameter [Name]
        /// Features =>  Parameter Features [Name]
        /// <see cref="CategoryParameter"/>
        /// </summary>
        public string Parameter { get; set; }
        public int Order { get; set; }
        /// <summary>
        /// [Tag],[Tag],[Tag]... Excel Format
        /// ["Tag","Tag","Tag"] json => Db Format
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// [RoleName],[RoleName],[RoleName]... Excel Format
        /// ["RoleName","RoleName","RoleName"] json => Db Format
        /// </summary>
        public string Security { get; set; }
        public virtual ICollection<CategoryCategory> Parents { get; set; }
        /// <summary>
        /// this is just for filling data from excel
        /// </summary>
        [NotMapped]
        public string ParentsString { get; set; }
    }
    public enum PublishedCatalogType
    {
        None,
        SingleAndMain,
        Single,
        Main
    }


    public class CategoryParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsFeature { get; set; }
    }
}
