# Team Limit (CounterStrikeSharp)

A plugin for CS2 that automatically limits each team to half of the available server slots.

The limit is read from 'sv_maxplayers' (with a margin of 'sv_visiblemaxplayers') and is counted as a whole half of the value: on a server with 4 slots, a maximum of 2 players per team, on a server with 24 slots, a maximum of 12. No additional settings are needed for team sizes.

## Install

1. Build the project via 'dotnet build -c Release'. 
2. Copy 'bin/Release/net10.0/TeamLimit.dll' to 'game/csgo/addons/counterstrikesharp/plugins/TeamLimit/'. 
3. On the first launch, a 'TeamLimit.json' config will be created for the rest of the plugin parameters.
