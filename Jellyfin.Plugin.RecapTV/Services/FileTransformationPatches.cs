namespace Jellyfin.Plugin.RecapTV.Services
{
    // Callback invoked by the File Transformation plugin (if installed) to
    // patch index.html. Registered by FileTransformationRegistrar.
    public static class FileTransformationPatches
    {
        public static string IndexHtml(PatchRequestPayload payload)
        {
            return ScriptInjection.Inject(payload.Contents ?? string.Empty);
        }
    }
}
