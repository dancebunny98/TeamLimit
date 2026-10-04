using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeamLimit;

public sealed class TeamLimitConfig : IBasePluginConfig
{
    public int Version { get; set; } = 1;
    public bool EnforceOnRoundStart { get; set; } = true;
    public bool CountBots { get; set; } = true;
    public string OverflowMessage { get; set; } = "Команда уже заполнена (максимум {0} игрока). Вы переведены в наблюдатели.";
}

public sealed class TeamLimitPlugin : BasePlugin, IPluginConfig<TeamLimitConfig>
{
    public override string ModuleName => "Team Limit";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "OpenAI";
    public override string ModuleDescription => "Ограничивает команды половиной слотов сервера.";

    public TeamLimitConfig Config { get; set; } = new();

    public void OnConfigParsed(TeamLimitConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        AddCommandListener("jointeam", OnJoinTeam);

        if (hotReload)
            Server.NextFrame(EnforceTeamLimits);
    }

    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsValidPlayer(player))
            return HookResult.Continue;

        var requestedTeam = ParseRequestedTeam(command.GetArg(1));
        if (requestedTeam is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            return HookResult.Continue;

        if (CountTeam(requestedTeam.Value, player) >= GetMaxPlayersPerTeam())
        {
            RejectJoin(player!);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsValidPlayer(player))
            return HookResult.Continue;

        Server.NextFrame(() => EnforcePlayerTeam(player!));
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (Config.EnforceOnRoundStart)
            Server.NextFrame(EnforceTeamLimits);
        return HookResult.Continue;
    }

    private void EnforceTeamLimits()
    {
        foreach (var team in new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist })
        {
            var players = Utilities.GetPlayers()
                .Where(IsValidPlayer)
                .Where(p => p.Team == team)
                .Where(p => Config.CountBots || !p.IsBot)
                .OrderBy(p => p.Index)
                .ToList();

            foreach (var player in players.Skip(GetMaxPlayersPerTeam()))
                RejectJoin(player);
        }
    }

    private void EnforcePlayerTeam(CCSPlayerController player)
    {
        if (!IsValidPlayer(player))
            return;

        if (player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist &&
            CountTeam(player.Team, null) > GetMaxPlayersPerTeam())
        {
            RejectJoin(player);
        }
    }

    private void RejectJoin(CCSPlayerController player)
    {
        var maxPlayersPerTeam = GetMaxPlayersPerTeam();
        player.ChangeTeam(CsTeam.Spectator);
        player.PrintToChat($"\u0001{string.Format(Config.OverflowMessage, maxPlayersPerTeam)}");
    }

    private int CountTeam(CsTeam team, CCSPlayerController? excluded)
    {
        return Utilities.GetPlayers()
            .Where(IsValidPlayer)
            .Where(p => p.Team == team && p != excluded)
            .Count(p => Config.CountBots || !p.IsBot);
    }

    private static int GetMaxPlayersPerTeam()
    {
        // Keep spectator/reserved connections outside the playable team limit.
        // Configure sv_maxplayers above sv_visiblemaxplayers when spectators
        // should be able to stay connected while all game slots are occupied.
        var maxPlayers = ReadServerPlayerLimit("sv_visiblemaxplayers");
        if (maxPlayers <= 0)
            maxPlayers = ReadServerPlayerLimit("sv_maxplayers");

        // Keep the teams equal if a server exposes an odd number of slots.
        return Math.Max(1, maxPlayers / 2);
    }

    private static int ReadServerPlayerLimit(string name)
    {
        try
        {
            return ConVar.Find(name)?.GetPrimitiveValue<int>() ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static CsTeam? ParseRequestedTeam(string value)
    {
        return value switch
        {
            "2" or "t" or "terrorist" => CsTeam.Terrorist,
            "3" or "ct" or "counter-terrorist" or "counterterrorist" => CsTeam.CounterTerrorist,
            "1" or "spec" or "spectator" => CsTeam.Spectator,
            _ => null
        };
    }

    private static bool IsValidPlayer(CCSPlayerController? player) =>
        player is { IsValid: true, IsHLTV: false };
}
