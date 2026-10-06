namespace Cicclo.Api.Auth;

public static class CurrentUser
{
    public static Guid? Id(HttpContext http)
    {
        var sub = http.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
