using System.Collections.Generic;

namespace Application.Services.Product
{
    public class ProductWithSlugCatDto : ProductDto
    {
        public List<string> CategorySlug { get; set; }
    }
}
