# RecapTV

[Jellyfin](https://jellyfin.org) server plugin for [RecapTV](https://recaptv.app). Syncs watched movies and
episodes to your RecapTV watchlist as you watch them.

<br>

## 🚀 Quick Start

1. In Jellyfin, go to **Dashboard → Plugins → Repositories → Add Repository**.
2. Set the URL to:
   ```
   https://raw.githubusercontent.com/RecapTV/jellyfin-plugin/main/manifest.json
   ```
3. Go to **Dashboard → Plugins → Catalog**, find **RecapTV**, and install it.
4. **Restart** Jellyfin.
5. In RecapTV, go to **Settings → Integrations → Connect Jellyfin** and grab the code.
6. In Jellyfin, open your user preferences and use the **RecapTV** entry to paste the code and connect.

> [!NOTE]
> No server-admin setup beyond installing the plugin is required. Each user connects their own account.

<br>

## ✨ What it does

- **Auto-sync on watch.** Completed playback of a movie or episode is reported to RecapTV automatically.
- **Per-user connection.** Every Jellyfin user links their own RecapTV account independently.
- **Two metadata sources.** Matches by TMDB and/or TVDB id, whichever your library metadata has.

<br>

## 💜 Recommended, not required

RecapTV works without either of these. They just make it more reliable.

- **[File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) Plugin** makes script injection more reliable. Install it like any other plugin, nothing else to configure.

- **[TheTVDB](https://github.com/jellyfin/jellyfin-plugin-tvdb) Plugin** improves episode matching. Install it, then enable it as a metadata provider on your TV libraries
  (**Dashboard → Libraries → your library → Metadata downloaders**).

<br>

## 💡 Support

- [Report an issue](https://github.com/RecapTV/jellyfin-plugin/issues)
