using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Application.Services.Filter;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class FilterService : FilterSrv.FilterSrvBase
    {
        private readonly Application.Services.Filter.FilterService service;
        public FilterService()
        {
            service = new();
        }
        //[AllowAnonymous]
        public override async Task<FiltersResponseMessage> GetAll(Empty request, ServerCallContext context)
        {
            List<FilterDto> filters = await service.Get().ConfigureAwait(false);
            FiltersResponseMessage result = new();
            foreach (var filter in filters)
                result.Filters.Add(MapToFilter(filter));
            return result;
        }
        private FilterResponseMessage MapToFilter(FilterDto Filter)
        {
            FilterResponseMessage message = new()
            {
                Id = Filter.Id.ToString(),
                Name = Filter.Name,
                Order = Filter.Order
            };
            if (Filter.Tags?.Count > 0)
                message.Tags.AddRange(Filter.Tags);
            if (Filter.Groups?.Count > 0)
                message.Groups.AddRange(Filter.Groups);

            return message;
        }
    }
}
