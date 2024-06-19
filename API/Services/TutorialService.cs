using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using API.Configuration;
using API.Helper;
using API.Protos;
using Application.Services.Tutorial;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class TutorialService : TutorialSrv.TutorialSrvBase
    {
        private Application.Services.Tutorial.TutorialSevice service;
        public TutorialService()
        {
        }
        public override async Task<TutorialsResponseMessage> GetAll(Empty request, ServerCallContext context)
        {
            NewService(context);
            List<TutorialDto> tutorials = await service.Get().ConfigureAwait(false);
            TutorialsResponseMessage result = new();
            foreach (var tutorial in tutorials)
                result.Tutorials.Add(MapToFilter(tutorial));
            return result;
        }
        private TutorialResponseMessage MapToFilter(TutorialDto tutorial)
        {
            TutorialResponseMessage message = new()
            {
                Id = tutorial.Id,
                Title = Tools.NullStringToEmpty(tutorial.Title),
                Description = Tools.NullStringToEmpty(tutorial.Description),
                Abstract = Tools.NullStringToEmpty(tutorial.Abstract),
            };
            if (tutorial.VideoUrls?.Length > 0)
                message.VideoUrls.AddRange(tutorial.VideoUrls);
            if (tutorial.ImageUrls?.Length > 0)
                message.ImageUrls.AddRange(tutorial.ImageUrls);

            return message;
        }
        private void NewService(ServerCallContext context)
        {
            service = new(Tools.GetRoles(context));
        }
    }
}
