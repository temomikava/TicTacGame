using System.Security.Claims;
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
        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/signalr"))
            {
                var sessionId = httpContext.Request.Headers.Authorization.ToString()?.Split()?.LastOrDefault();
                var isValidSessionId = Guid.TryParse(sessionId, out var validSessionId);
                if (isValidSessionId)
                {
                    guid = validSessionId;
                }

                var dal = httpContext.RequestServices.GetRequiredService<IDatabaseConnection>();
                var id = dal.GetUserId(guid);

                if (id == -1)
                {
                    httpContext.Response.StatusCode = 401;
                    return;
                }

                var identity = new ClaimsIdentity();
                identity.AddClaim(new Claim(ClaimTypes.Name, id.ToString()));
                identity.AddClaim(new Claim(ClaimTypes.Role, validSessionId.ToString()));
                httpContext.User.AddIdentity(identity);
                await next.Invoke(httpContext);
                return;
            }
            await next.Invoke(httpContext);
        }
    }
}
