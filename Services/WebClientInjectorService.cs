using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RecapTV.Services
{
    // Direct on-disk rewrite of index.html (same technique other client-injection plugins use); upgrade to the File Transformation plugin's non-destructive hook if this proves fragile across web client updates or read-only deployments.
    public class WebClientInjectorService : IHostedService
    {
        private static readonly Regex ExistingTag = new("<script plugin=\"RecapTV\"[^>]*></script>\\s*", RegexOptions.Compiled);

        // Cache-busts with a per-startup value so browsers/service workers can't keep serving a stale ClientScript.js across plugin updates.
        private static readonly string ScriptTag =
            $"<script plugin=\"RecapTV\" src=\"/RecapTV/ClientScript.js?v={DateTime.UtcNow.Ticks}\" defer></script>";

        private readonly IApplicationPaths _applicationPaths;
        private readonly ILogger<WebClientInjectorService> _logger;

        public WebClientInjectorService(IApplicationPaths applicationPaths, ILogger<WebClientInjectorService> logger)
        {
            _applicationPaths = applicationPaths;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var indexPath = Path.Combine(_applicationPaths.WebPath, "index.html");
            try
            {
                if (!File.Exists(indexPath))
                {
                    _logger.LogWarning("Web client index.html not found at {Path}; skipping script injection", indexPath);
                    return Task.CompletedTask;
                }

                var content = File.ReadAllText(indexPath);
                content = ExistingTag.Replace(content, string.Empty);
                content = content.Replace("</body>", $"{ScriptTag}\n</body>", StringComparison.Ordinal);
                File.WriteAllText(indexPath, content);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    ex,
                    "Could not inject RecapTV script into {Path}. Add '{Tag}' before </body> manually.",
                    indexPath,
                    ScriptTag);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
