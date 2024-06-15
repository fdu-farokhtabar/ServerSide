using System;

namespace Domain.Entity
{
    public class CategoryCategory
    {
        public int Id { get; set; }
        public Guid CategoryId { get; set; }
        public string CategorySlug { get; set; }
        public Guid ParentCategoryId { get; set; }
        public string ParentCategorySlug { get; set; }
        public int Order { get; set; }
    }
}
