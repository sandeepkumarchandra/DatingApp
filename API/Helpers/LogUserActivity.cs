using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extentions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();
            if(!resultContext.HttpContext.User.Identity.IsAuthenticated) return;

            var userId = resultContext.HttpContext.User.getUserId();
            var uow = resultContext.HttpContext.RequestServices.GetService<IUnitOfWork>();
            var user = await uow.UserRepository.GetUserByIDAsync(userId);
            user.LastActive = DateTime.UtcNow;
            await uow.Completed();
        }
    }
}