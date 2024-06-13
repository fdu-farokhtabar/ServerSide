using System;

namespace Domain.Entity
{
    public class UserRole
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
