using System;
using System.Collections.Generic;

namespace Application.Services.Account
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string StoreName { get; set; }
        public List<string> Roles { get; set; }
        public List<string> Pages { get; set; }
        public List<string> Buttons { get; set; }
    }
}
