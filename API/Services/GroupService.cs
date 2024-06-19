using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Application.Services.Filter;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class GroupService : GroupSrv.GroupSrvBase
    {
        private readonly Application.Services.Group.GroupService service;
        public GroupService()
        {
            service = new();
        }
        //[AllowAnonymous]
        public override async Task<GroupsResponseMessage> GetAll(Empty request, ServerCallContext context)
        {
            List<GroupDto> groups = await service.Get().ConfigureAwait(false);
            GroupsResponseMessage result = new();
            foreach (var group in groups)
                result.Groups.Add(MapToGroup(group));
            return result;
        }
        private GroupResponseMessage MapToGroup(GroupDto Group)
        {
            GroupResponseMessage message = new()
            {
                Id = Group.Id.ToString(),
                Name = Group.Name,
                IsVisible = Group.IsVisible,
                Order = Group.Order
            };
            return message;
        }
    }
}
