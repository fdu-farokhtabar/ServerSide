using Application.Data;
using Application.Services.Helper;
using Application.Services.Product;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Account
{
    public class AccountService
    {
        public async Task<AccountDto> Login(string Email, string Password)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                throw new Exception("Username(email) or password cannot be empty.");
            using var Db = new Context();
            var User = await Db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Email.ToLower() == Email.ToLower());

            if (User is null)
                throw new Exception("user is not found.");
            if (User.Password != Tools.HashData(Password))
                throw new Exception("password is wrong");
            var RolesId = User.Roles.Select(x => x.RoleId).ToList();
            var Roles = await Db.Roles.Where(x => RolesId.Contains(x.Id)).ToListAsync();
            if (Roles?.Count == 0)
                throw new Exception("user is not valid");

            return new AccountDto()
            {
                Id = User.Id,
                UserName = User.UserName,
                Email = User.Email,
                Name = User.Name,
                LastName = User.LastName,
                StoreName = User.StoreName,
                Roles = Roles.Select(x => x.Name).ToList(),
                Pages = Roles.Select(x => x.Pages)?.Distinct().ToList(),
                Buttons = GetButtons(Roles.Select(x => x.Buttons)?.Distinct().ToList())?.Distinct().ToList()
            };
        }

        private List<string> GetButtons(List<string> Buttons)
        {
            List<string> PermissionButtons = null;
            if (Buttons?.Count > 0)
            {
                PermissionButtons = new List<string>();
                foreach (var button in Buttons)
                {
                    List<KeyValueDto> Value = !string.IsNullOrWhiteSpace(button) ? System.Text.Json.JsonSerializer.Deserialize<List<KeyValueDto>>(button) : null;
                    if (Value?.Count > 0)
                    {
                        var PerButtons = Value.Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value.Trim() == "1").Select(x => x.Name).ToList();
                        if (PerButtons?.Count > 0)
                            PermissionButtons.AddRange(PerButtons);
                    }
                }
            }
            return PermissionButtons;
        }

        public async Task<CustomerDto> GetEmail(string UserName)
        {
            using var Db = new Context();
            return await Db.Users.Where(x => x.UserName == UserName).Select(x => new CustomerDto { Email = x.Email, FullName = x.Name + " " + x.LastName }).FirstOrDefaultAsync();
        }

        public async Task<List<CustomersOfRepDto>> GetCustomersOfRep(string RepUserName)
        {
            using var Db = new Context();
            var Customers = await Db.Users.Where(x => x.Rep.Contains(RepUserName)).OrderBy(x => x.StoreName).ToListAsync();
            if (Customers?.Count > 0)
            {
                return Customers.Select(x =>
                new CustomersOfRepDto()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Family = x.LastName,
                    UserName = x.UserName,
                    StoreName = x.StoreName
                }).ToList();
            }
            else
                return null;
        }
    }
}
