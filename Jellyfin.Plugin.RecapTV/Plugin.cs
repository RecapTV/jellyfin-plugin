using System;
using System.Collections.Generic;
using Jellyfin.Plugin.RecapTV.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.RecapTV
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IServerConfigurationManager serverConfigurationManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            ServerConfigurationManager = serverConfigurationManager;
        }

        public static Plugin? Instance { get; private set; }

        internal IServerConfigurationManager ServerConfigurationManager { get; }

        public override string Name => "RecapTV";

        public override Guid Id => Guid.Parse("b3f1b6b0-6f0a-4c8b-9a3d-7c2e4f5a9d10");

        public IEnumerable<PluginPageInfo> GetPages()
        {
#if DEBUG
            yield return new PluginPageInfo
            {
                Name = "RecapTV",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            };
#else
            yield break;
#endif
        }
    }
}
