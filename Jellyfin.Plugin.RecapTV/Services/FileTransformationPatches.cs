using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RecapTV.Services
{
    // Callback invoked by the File Transformation plugin (if installed) to
    // patch index.html. Registered by FileTransformationRegistrar, which also
    // sets Logger since this is a static reflection callback with a fixed
    // signature - no constructor injection available here.
    public static class FileTransformationPatches
    {
        public static ILogger? Logger { get; set; }

        public static string IndexHtml(PatchRequestPayload payload)
        {
            return ScriptInjection.Inject(payload.Contents ?? string.Empty, Logger);
        }
    }
}
