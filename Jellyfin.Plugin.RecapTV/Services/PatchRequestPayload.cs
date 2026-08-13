namespace Jellyfin.Plugin.RecapTV.Services
{
    // Shape the File Transformation plugin reflects a JObject into before
    // calling FileTransformationPatches.IndexHtml. Property name must match
    // the "contents" key it sends (Newtonsoft matches property names
    // case-insensitively, so no [JsonProperty] needed).
    public class PatchRequestPayload
    {
        public string? Contents { get; set; }
    }
}
