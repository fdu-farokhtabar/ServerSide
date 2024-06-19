using System.Collections.Generic;

namespace Application.Services.Product
{
    public class ProductsWithTotalItemDto
    {
        public List<ProductDto> Products { get; set; }
        public int TotalItems { get; set; }
    }
}
