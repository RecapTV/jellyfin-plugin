using System.Linq;
using System.Reflection;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.RecapTV.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public static readonly string DefaultApiBaseUrl = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RecapTVApiBaseUrl")?.Value;

        public string ApiBaseUrl { get; set; } = DefaultApiBaseUrl;
    }
}
