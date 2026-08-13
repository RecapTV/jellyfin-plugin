using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.RecapTV.Services
{
    // Injects the RecapTV <script> tag into jellyfin-web's index.html at request
    // time via IStartupFilter, instead of rewriting the file on disk. On-disk
    // rewrites need a writable web root (fails on read-only containers) and get
    // wiped on every jellyfin-web update; rewriting the response in-flight avoids
    // both problems entirely. Fragile against some reverse-proxy/CDN setups
    // (buffered rewrite, header stripping), so this is a fallback: when the
    // File Transformation plugin is installed, FileTransformationRegistrar
    // registers the same injection with it instead, which is more broadly
    // compatible. ScriptInjection.Inject is idempotent, so having both active
    // is safe.
    public class WebClientInjectorStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(InvokeAsync);
                next(app);
            };
        }

        private static async Task InvokeAsync(HttpContext context, Func<Task> next)
        {
            if (!HttpMethods.IsGet(context.Request.Method) || !IsIndexRequest(context.Request.Path.Value))
            {
                await next().ConfigureAwait(false);
                return;
            }

            // Drop compression/range negotiation so the static handler returns a
            // full, plain-text 200 body we can rewrite.
            context.Request.Headers.Remove("Accept-Encoding");
            context.Request.Headers.Remove("Range");
            context.Request.Headers.Remove("If-Range");

            var originalBody = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                context.Response.Body = originalBody;
            }

            buffer.Seek(0, SeekOrigin.Begin);

            var isHtml = context.Response.StatusCode == 200
                && (context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) ?? false);
            if (!isHtml)
            {
                await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
                return;
            }

            string html;
            using (var reader = new StreamReader(buffer, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                html = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            html = ScriptInjection.Inject(html);

            var bytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html;charset=utf-8";
            context.Response.ContentLength = bytes.Length;
            context.Response.Headers.Remove("ETag");
            context.Response.Headers.Remove("Last-Modified");
            context.Response.Headers.Remove("Accept-Ranges");
            await originalBody.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        private static bool IsIndexRequest(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return path.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("/web/", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/web", StringComparison.OrdinalIgnoreCase);
        }
    }
}
