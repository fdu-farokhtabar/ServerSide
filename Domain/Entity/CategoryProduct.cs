using System;

namespace Domain.Entity
{
    public class CategoryProduct
    {
        public int Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid CategoryId { get; set; }
        public string CategorySlug { get; set; }
    }
}
