using System;

namespace Domain.Entity
{
    public class Group
    // In certain situations, customers need to display products within specific categories or divisions that do not belong to them.
    // For example 
    // Situation1: A product belongs to Category A, but the customer wants it to be displayed in Category B as well. The Group class can be used to achieve this cross-category display.
    // Situation2: Customers can group products according to specific needs to display them in different contexts. This flexibility allows products to be displayed without being restricted to a single category.
    {
        public Guid Id { get; set; } // primary key
        public string Name { get; set; }
        public bool IsVisible { get; set; } 
        public int Order { get; set; }
    }
}
