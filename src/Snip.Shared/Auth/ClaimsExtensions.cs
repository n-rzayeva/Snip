using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Snip.Shared.Auth;

public static class ClaimsExtensions
{
    public static string? GetUserId(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;
    }
}