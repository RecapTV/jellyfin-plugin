using Jellyfin.Plugin.RecapTV.Notifiers;
using Jellyfin.Plugin.RecapTV.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.RecapTV
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<TokenStore>();
            serviceCollection.AddHttpClient<RecapTVApiClient>();
            serviceCollection.AddScoped<IEventConsumer<PlaybackStopEventArgs>, PlaybackStopNotifier>();
            serviceCollection.AddSingleton<IStartupFilter, WebClientInjectorStartupFilter>();
        }
    }
}
