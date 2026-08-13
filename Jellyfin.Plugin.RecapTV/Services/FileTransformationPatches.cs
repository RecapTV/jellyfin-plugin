using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;

namespace Jellyfin.Plugin.RecapTV.Services
{
    // Callback invoked by the File Transformation plugin (if installed) to
    // patch index.html. Registered by FileTransformationRegistrar.
    public static class FileTransformationPatches
    {
        public static string IndexHtml(PatchRequestPayload payload)
        {
            var networkConfiguration = Plugin.Instance!.ServerConfigurationManager.GetNetworkConfiguration();
            var basePath = string.IsNullOrWhiteSpace(networkConfiguration.BaseUrl)
                ? string.Empty
                : $"/{networkConfiguration.BaseUrl.Trim('/')}";

            return ScriptInjection.Inject(payload.Contents ?? string.Empty, basePath);
        }
    }
}
