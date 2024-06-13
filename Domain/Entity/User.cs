using System;
using System.Collections.Generic;

namespace Domain.Entity
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// MD5
        /// </summary>
        public string Password { get; set; }
        public string LastName { get; set; }
        public string StoreName { get; set; }
        public string Rep { get; set; }
        public virtual ICollection<UserRole> Roles { get; set; }

        public string ShippingAddress1 { get; set; }
        public string ShippingAddress2 { get; set; }
        public string ShippingCountry { get; set; }
        public string ShippingState { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingZipCode { get; set; }


        public string StoreAddress1 { get; set; }
        public string StoreAddress2 { get; set; }
        public string StoreCountry { get; set; }
        public string StoreState { get; set; }
        public string StoreCity { get; set; }
        public string StoreZipCode { get; set; }
        public string TaxId { get; set; }
    }
}
