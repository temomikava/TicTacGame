﻿using System.Security.Claims;
using WebAPI.Core.Interface;

namespace WebAPI
{
    public class CustomMiddleware
    {
        private readonly RequestDelegate next;
        public CustomMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        Guid guid = new Guid();
        int id = 0;
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/signalr"))
            {
                var sessionId = httpContext.Request.Headers.Authorization.ToString()?.Split()?.LastOrDefault();
                var isValidSessionId = Guid.TryParse(sessionId, out var validSessionId);
                if (isValidSessionId)
                {
                    guid = validSessionId;
                    var dal = httpContext.RequestServices.GetRequiredService<IDatabaseConnection>();
                    id = dal.GetUserId(guid);
                }


                if (id == -1)
                {
                    httpContext.Response.StatusCode = 401;
                }

                var identity = new ClaimsIdentity();
                identity.AddClaim(new Claim(ClaimTypes.Name, id.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, guid.ToString()));
                httpContext.User.AddIdentity(identity);
                return next.Invoke(httpContext);
            }
           return next.Invoke(httpContext);
        }
    }
}
