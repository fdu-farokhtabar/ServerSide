using Grpc.Core;
using API.Protos;
using Application.SeedWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace API.Services
{
    public class AccountService : AccountSrc.AccountSrcBase
    {
        private readonly Application.Services.Account.AccountService service;
        private readonly IApplicationSettings applicationSettings;

        public AccountService(IApplicationSettings applicationSettings)
        {
            service = new Application.Services.Account.AccountService();
            this.applicationSettings = applicationSettings;
        }
        [AllowAnonymous]
        public override async Task<LoginResponseMessage> Login(LoginRequestMessage request, ServerCallContext context)
        {
            var account = await service.Login(request.Username, request.Password);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(applicationSettings.SigningKey);

            List<Claim> claims = new();
            claims.Add(new(ClaimTypes.NameIdentifier, account.Id.ToString()));
            claims.Add(new(ClaimTypes.Name, account.UserName));
            claims.Add(new(ClaimTypes.Email, account.Email));
            claims.Add(new(ClaimTypes.GivenName, account.Name));
            claims.Add(new(ClaimTypes.Surname, account.LastName));
            claims.Add(new("store_name", account.StoreName));

            foreach (var Role in account.Roles)
                if (Role is not null)
                    claims.Add(new(ClaimTypes.Role, Role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(applicationSettings.UserAuthorizationTokenExpireTimeInMin),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            LoginResponseMessage result = new()
            {
                Token = tokenHandler.WriteToken(token)
            };
            result.Pages.AddRange(account.Pages.Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
            result.Buttons.AddRange(account.Buttons.Where(x => !string.IsNullOrWhiteSpace(x)).ToList());

            return result;
        }
        public override async Task<CustomersOfRepResponseMessage> GetCustomersOfRep(CustomersOfRepRequestMessage request, ServerCallContext context)
        {
            var Customers = await service.GetCustomersOfRep(request.RepUserName).ConfigureAwait(false);
            CustomersOfRepResponseMessage result = new();
            if (Customers?.Count > 0)
            {
                result.Customers.AddRange(Customers.Select(x => new CustomerOfRepResponseMessage()
                {
                    Id = x.Id.ToString(),
                    Family = x.Family,
                    Name = x.Name,
                    UserName = x.UserName,
                    StoreName = x.StoreName
                }).ToList());
            }
            return result;
        }
    }
}
