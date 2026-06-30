using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.Common;

public class OpenShockControllerBase : ControllerBase
{
    [NonAction]
    protected ObjectResult Problem(OpenShockProblem problem) => problem.ToObjectResult(HttpContext);
    
    [NonAction]
    protected OkObjectResult LegacyDataOk<T>(T data, string message = "")
    {
        return Ok(new LegacyDataResponse<T>(data, message));
    }

    [NonAction]
    protected CreatedResult LegacyDataCreated<T>(string? uri, T data)
    {
        return Created(uri, new LegacyDataResponse<T>(data));
    }

    [NonAction]
    protected OkObjectResult LegacyEmptyOk(string message = "")
    {
        return Ok(new LegacyEmptyResponse(message));
    }

    [NonAction]
    protected string? GetCurrentCookieDomain()
    {
        var options = HttpContext.RequestServices
            .GetRequiredService<FrontendOptions>();

        var host = HttpContext.Request.Host.Host;

        var domain = DomainUtils.GetBestMatchingCookieDomain(host, options.CookieDomains);

        if (domain != null)
            return domain;

        // HOTFIX:
        // Wenn nur eine Cookie-Domain konfiguriert ist,
        // verwende diese als Fallba ck.
        if (options.CookieDomains.Count == 1)
            {
                foreach (var cookieDomain in options.CookieDomains)
                    return cookieDomain;
            }

        return null;
    }

    [NonAction]
    protected async Task CreateSession(Guid accountId, string domain)
    {
        var sessionService = HttpContext.RequestServices.GetRequiredService<ISessionService>();
        
        var session = await sessionService.CreateSessionAsync(accountId, HttpContext.GetUserAgent(), HttpContext.GetRemoteIP().ToString());
        
        HttpContext.Response.Cookies.Append(AuthConstants.UserSessionCookieName, session.Token, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(Duration.LoginSessionLifetime),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });
    }

    [NonAction]
    protected void RemoveSessionKeyCookie()
    {
        var cookieDomains = HttpContext.RequestServices.GetRequiredService<FrontendOptions>().CookieDomains;
        foreach (var domain in cookieDomains)
        {
            HttpContext.Response.Cookies.Delete(AuthConstants.UserSessionCookieName, new CookieOptions { Domain = domain });
        }
    }
}