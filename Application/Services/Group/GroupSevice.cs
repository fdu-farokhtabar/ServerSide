using Application.Data;
using Application.Services.Filter;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Group
{
    public class GroupService
    {
        public async Task<List<GroupDto>> Get()
        {
            using var Db = new Context();
            var Groups = await Db.Groups.ToListAsync();
            ConcurrentBag<GroupDto> Result = new();
            Parallel.ForEach(Groups, Group =>
            {
                Result.Add(new GroupDto()
                {
                    Id = Group.Id,
                    Name = Group.Name,
                    IsVisible = Group.IsVisible,
                    Order = Group.Order
                });
            });
            return Result.OrderBy(x => x.Order).ToList();
        }
    }
}
