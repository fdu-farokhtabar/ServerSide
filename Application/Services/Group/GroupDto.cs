using System;

namespace Application.Services.Filter
{
    public class GroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsVisible { get; set; }
        public int Order { get; set; }
    }
}
