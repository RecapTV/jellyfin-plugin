# RecapTV Jellyfin plugin

Jellyfin server plugin for [RecapTV](https://recaptv.app).

## Installing

1. **Required:** install the [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)
   plugin first (add `https://www.iamparadox.dev/jellyfin/plugins/manifest.json` as a
   repository, then install "File Transformation" from the catalog). RecapTV uses it
   to inject its client script into the web UI.
2. In Jellyfin, go to **Dashboard → Plugins → Repositories → Add Repository**.
3. Set the URL to:
   ```
   https://raw.githubusercontent.com/RecapTV/jellyfin-plugin/main/manifest.json
   ```
4. Go to **Dashboard → Plugins → Catalog**, find **RecapTV**, and install it.
5. Restart Jellyfin.
6. In RecapTV, go to **Settings → Integrations → Connect Jellyfin** and grab the
   code (optionally opt out of backfilling earlier episodes).
7. In Jellyfin, log in as that user, open the user preferences/settings menu, and
   use the **RecapTV** entry to paste the code and connect the account.
