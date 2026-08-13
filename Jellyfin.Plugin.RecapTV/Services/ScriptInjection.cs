using System;

namespace Jellyfin.Plugin.RecapTV.Services
{
    // Shared by both injection paths: the built-in IStartupFilter middleware
    // (WebClientInjectorStartupFilter) and the callback registered with the
    // community File Transformation plugin, if installed
    // (FileTransformationPatches). Both call Inject so neither double-injects
    // the tag when the other has already run.
    internal static class ScriptInjection
    {
        private const string Marker = "plugin=\"RecapTV\"";
        private static readonly long CacheBustTicks = DateTime.UtcNow.Ticks;

        public static string Inject(string html)
        {
            if (html.IndexOf(Marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return html;
            }

            var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyClose < 0)
            {
                return html;
            }

            // Relative to index.html's own URL (".../web/index.html" or ".../web/"), so
            // it resolves to ".../RecapTV/ClientScript.js" under whatever prefix the
            // request actually arrived under - no need to know Jellyfin's configured
            // Base URL or track reverse-proxy/tunnel path rewriting ourselves.
            var tag = $"<script {Marker} src=\"../RecapTV/ClientScript.js?v={CacheBustTicks}\" defer></script>";
            return html.Substring(0, bodyClose) + tag + "\n" + html.Substring(bodyClose);
        }
    }
}
