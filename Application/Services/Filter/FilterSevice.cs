using Application.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Filter
{
    public class FilterService
    {
        public async Task<List<FilterDto>> Get()
        {
            using var Db = new Context();
            var Filters = await Db.Filters.ToListAsync();
            ConcurrentBag<FilterDto> Result = new();
            Parallel.ForEach(Filters, Filter =>
            {
                Result.Add(new FilterDto()
                {
                    Id = Filter.Id,
                    Name = Filter.Name,
                    Order = Filter.Order,
                    Tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(Filter.Tags),
                    Groups = !string.IsNullOrWhiteSpace(Filter.Groups) ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(Filter.Groups) : null
                });
            });
            return Result.OrderBy(x => x.Order).ToList();
        }
    }
}
