using System;

namespace Application.Services.Category
{
    public class CategoryShortDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int Order { get; set; }
        public string ShortDescription { get; set; }
        public string Security { get; set; }
    }
}
