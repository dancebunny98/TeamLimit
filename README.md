# Team Limit (CounterStrikeSharp)

Limits each CS2 team to half of the available server slots. The plugin reads `sv_maxplayers`, accounts for a smaller `sv_visiblemaxplayers` when appropriate, and moves excess players to spectators. For example, a 24-slot server allows up to 12 players per team.

## Requirements

- CounterStrikeSharp API 1.0.376
- .NET 10

## Build and installation

1. Run `dotnet build -c Release` in this directory.
2. Copy `bin/Release/net10.0/TeamLimit.dll` and the `lang` directory to `game/csgo/addons/counterstrikesharp/plugins/TeamLimit/`.
3. Start the server once to generate `configs/plugins/TeamLimit/TeamLimit.json`.

## Configuration

| Setting | Default | Description |
| --- | --- | --- |
| `Language` | `ru` | Message language: `ru` or `en`. Invalid values use Russian. |
| `EnforceOnRoundStart` | `true` | Recheck team limits at the start of each round. |
| `CountBots` | `true` | Include bots in the team count. |
| `OverflowMessage` | `null` | Optional custom message. When set, it overrides `lang/<Language>.json`. `{0}` is the team limit. |

Translate or customize the `team_full` entry in `lang/ru.json` and `lang/en.json`. The plugin falls back to English if the selected file is missing.

## Behavior

The `jointeam` listener rejects a request for a full team. Team-change and round-start checks also move any excess players to spectators. The limit is calculated from the server's visible capacity, with a minimum of one player per team.

When `sv_maxplayers` is unavailable, the plugin uses `Server.MaxPlayers`. It uses a smaller `sv_visiblemaxplayers` only when that value is above half of the server capacity, so a deliberately reserved spectator slot reduces the playable capacity without treating an invalid low value as the limit. Team checks exclude the joining player from the current count and can include or ignore bots according to `CountBots`.
