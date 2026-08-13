using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.RecapTV.Services
{
    // Registers RecapTV's index.html injection with the File Transformation
    // plugin (https://github.com/IAmParadox27/jellyfin-plugin-file-transformation),
    // if it's installed. That plugin hooks the static file provider directly and
    // is more broadly compatible across reverse-proxy/CDN setups than our own
    // IStartupFilter middleware (see WebClientInjectorStartupFilter), which stays
    // registered as a fallback for servers that don't have it.
    //
    // Runs as a scheduled task with a startup trigger, rather than an
    // IServerEntryPoint, so it executes after other plugin assemblies are loaded
    // and discoverable via reflection - matches the pattern used by File
    // Transformation's own consumers (e.g. jellyfin-plugin-pages).
    public class FileTransformationRegistrar : IScheduledTask
    {
        public string Name => "RecapTV File Transformation Registration";

        public string Key => "Jellyfin.Plugin.RecapTV.FileTransformationRegistrar";

        public string Description => "Registers RecapTV's client script injection with the File Transformation plugin, if installed.";

        public string Category => "Startup Services";

        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var fileTransformationAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".FileTransformation") ?? false);

            var registerMethod = fileTransformationAssembly?
                .GetType("Jellyfin.Plugin.FileTransformation.PluginInterface")?
                .GetMethod("RegisterTransformation");

            if (registerMethod is not null)
            {
                var payload = new JObject
                {
                    ["id"] = "b3f1b6b0-6f0a-4c8b-9a3d-7c2e4f5a9d11",
                    ["fileNamePattern"] = "index.html",
                    ["callbackAssembly"] = GetType().Assembly.FullName,
                    ["callbackClass"] = typeof(FileTransformationPatches).FullName,
                    ["callbackMethod"] = nameof(FileTransformationPatches.IndexHtml)
                };

                registerMethod.Invoke(null, new object?[] { payload });
            }

            return Task.CompletedTask;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
        }
    }
}
