using DoTrack.Application.Abstractions;

namespace DoTrack.Api.Middleware;

public sealed class AuditContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditContextAccessor accessor)
    {
        var metadata = new Dictionary<string, string>
        {
            ["request_id"] = context.TraceIdentifier,
            ["http_method"] = context.Request.Method,
            ["http_path"] = context.Request.Path.ToString()
        };
        accessor.SetContext(new AuditContext("api", null, metadata));
        await next(context);
    }
}
