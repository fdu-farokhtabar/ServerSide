using Application.Data;
using Application.SeedWork;
using Application.Services.Account;
using Application.Services.Category;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Tutorial
{
    public class TutorialSevice
    {        
        private readonly List<string> userRoles;
        private readonly AuthorizationService Auth;
        public TutorialSevice(List<string> userRoles)
        {            
            this.userRoles = userRoles;
            Auth = new AuthorizationService(userRoles);
        }

        public async Task<List<TutorialDto>> Get()
        {
            using var Db = new Context();
            var Tutorials = RemoveTutorialsWithoutPermissionsFromLists(await Db.Tutorials.ToListAsync());
            ConcurrentBag<TutorialDto> Result = new();
            Parallel.ForEach(Tutorials, tutorial =>
            {
                Result.Add(new TutorialDto()
                {
                    Id = tutorial.Id,
                    Title = tutorial.Title,
                    Description = tutorial.Description,
                    Abstract = tutorial.Abstract,
                    VideoUrls = tutorial.VideoUrls,
                    ImageUrls = tutorial.ImageUrls,
                });
            });
            return Result.OrderBy(x => x.Id).ToList();
        }
        /// <summary>
        /// بر اساس مجوز محصول نمایش یا مخفی می شود
        /// </summary>
        /// <param name="products"></param>
        /// <returns></returns>
        public List<Domain.Entity.Tutorial> RemoveTutorialsWithoutPermissionsFromLists(List<Domain.Entity.Tutorial> tutorials)
        {
            if (userRoles.Any(x => string.Equals(x, "admin", StringComparison.OrdinalIgnoreCase)))
                return tutorials;           
            List<Domain.Entity.Tutorial> tutorialsWithPermisson = null;
            if (tutorials?.Count > 0)
            {
                tutorialsWithPermisson = new List<Domain.Entity.Tutorial>();
                foreach (var tutorial in tutorials)
                {                    
                    if (Auth.HasUserPermissionToUseData(tutorial.Roles))
                        tutorialsWithPermisson.Add(tutorial);
                }
            }

            return tutorialsWithPermisson;
        }
    }
}
