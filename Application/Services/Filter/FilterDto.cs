using System;
using System.Collections.Generic;

namespace Application.Services.Filter
{
    public class FilterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Groups { get; set; }
    }
}
