using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RecapTV.Services
{
    public record TokenRecord(string Token, bool Connected, string? LastError, DateTime UpdatedAt);

    /// <summary>
    /// Per-Jellyfin-user RecapTV token storage. Kept in its own JSON file (not the plugin's
    /// XML configuration) so tokens never show up on the standard admin plugin-config page.
    /// </summary>
    public class TokenStore
    {
        private readonly string _filePath;
        private readonly ILogger<TokenStore> _logger;
        private readonly object _lock = new();
        private Dictionary<string, TokenRecord> _tokens;

        public TokenStore(IApplicationPaths applicationPaths, ILogger<TokenStore> logger)
        {
            _logger = logger;
            _filePath = Path.Combine(applicationPaths.DataPath, "recaptv-tokens.json");
            _tokens = Load();
        }

        public TokenRecord? Get(Guid userId)
        {
            lock (_lock)
            {
                return _tokens.TryGetValue(userId.ToString("N"), out var record) ? record : null;
            }
        }

        public void Save(Guid userId, string token)
        {
            lock (_lock)
            {
                _tokens[userId.ToString("N")] = new TokenRecord(token, true, null, DateTime.UtcNow);
                Persist();
            }
        }

        public void Remove(Guid userId)
        {
            lock (_lock)
            {
                if (_tokens.Remove(userId.ToString("N")))
                {
                    Persist();
                }
            }
        }

        public void MarkInvalid(Guid userId, string error)
        {
            lock (_lock)
            {
                var key = userId.ToString("N");
                if (!_tokens.TryGetValue(key, out var existing))
                {
                    return;
                }

                _tokens[key] = existing with { Connected = false, LastError = error, UpdatedAt = DateTime.UtcNow };
                Persist();
            }
        }

        private Dictionary<string, TokenRecord> Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new Dictionary<string, TokenRecord>();
                }

                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, TokenRecord>>(json)
                       ?? new Dictionary<string, TokenRecord>();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "[RecapTV] Failed to read token store at {Path}, starting empty", _filePath);
                return new Dictionary<string, TokenRecord>();
            }
        }

        private void Persist()
        {
            try
            {
                File.WriteAllText(_filePath, JsonSerializer.Serialize(_tokens));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "[RecapTV] Failed to write token store at {Path}", _filePath);
            }
        }
    }
}
