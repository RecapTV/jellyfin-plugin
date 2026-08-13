# RecapTV Jellyfin plugin

Jellyfin server plugin for [RecapTV](https://recaptv.app).

## Installing

1. In Jellyfin, go to **Dashboard → Plugins → Repositories → Add Repository**.
2. Set the URL to:
   ```
   https://raw.githubusercontent.com/RecapTV/jellyfin-plugin/main/manifest.json
   ```
3. Go to **Dashboard → Plugins → Catalog**, find **RecapTV**, and install it.
4. Restart Jellyfin.
5. In RecapTV, go to **Settings → Integrations → Connect Jellyfin** and grab the
   code (optionally opt out of backfilling earlier episodes).
6. In Jellyfin, log in as that user, open the user preferences/settings menu, and
   use the **RecapTV** entry to paste the code and connect the account.
