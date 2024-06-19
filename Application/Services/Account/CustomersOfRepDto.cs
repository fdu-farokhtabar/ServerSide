using System;

namespace Application.Services.Account
{
    public class CustomersOfRepDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string UserName { get; set; }
        public string StoreName { get; set; }
    }
}
