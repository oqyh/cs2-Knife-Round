using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Core.Attributes;

namespace Knife_Round;

[MinimumApiVersion(164)]
public class KnifeRoundConfig : BasePluginConfig
{
    [JsonPropertyName("GiveArmorOnKnifeRound")] public int GiveArmorOnKnifeRound { get; set; } = 2;
    [JsonPropertyName("FreezeOnVote")] public bool FreezeOnVote { get; set; } = true;
    [JsonPropertyName("BlockTeamChangeOnVoteAndKnife")] public bool BlockTeamChangeOnVoteAndKnife { get; set; } = true;
    [JsonPropertyName("AllowAllTalkOnKnifeRound")] public bool AllowAllTalkOnKnifeRound { get; set; } = true;
    [JsonPropertyName("KnifeRoundTimer")] public float KnifeRoundTimer { get; set; } = 1;
    [JsonPropertyName("VoteTimer")] public float VoteTimer { get; set; } = 50;
    [JsonPropertyName("MessageKnifeStartTimer")] public float MessageKnifeStartTimer { get; set; } = 15;
    [JsonPropertyName("AfterWinningRestartXTimes")] public int AfterWinningRestartXTimes { get; set; } = 3;
}

public class KnifeRound : BasePlugin, IPluginConfig<KnifeRoundConfig> 
{
    public override string ModuleName => "Knife Round";
    public override string ModuleVersion => "1.0.9";
    public override string ModuleAuthor => "Gold KingZ";
    public override string ModuleDescription => "Creates An Additional Round With Knifes After Warmup";
    public KnifeRoundConfig Config { get; set; } = new KnifeRoundConfig();

    internal static IStringLocalizer? Stringlocalizer;
    private Stopwatch stopwatch = new Stopwatch();
    private Dictionary<ulong, bool> OnSpawn = new Dictionary<ulong, bool>();
    public float mp_roundtime;
    public string mp_roundtimeFixed = "";
    public float mp_roundtime_defuse;
    public float mp_team_intro_time;
    public bool sv_alltalk;
    public bool sv_deadtalk;
    public bool sv_full_alltalk;
    public bool sv_talk_enemy_dead;
    public bool sv_talk_enemy_living;
    public int currentVotesT;
    public int currentVotesCT;
    public bool knifemode = false;
    public bool CTWINNER = false;
    public bool TWINNER = false;
    public bool BlockTeam = false;
    public bool onroundstart = false;
    public bool knifestarted = false;
    public float timer;
    public string targetPlayerName = "";
    private List<ulong> _rtvCountCT = new();
    private List<ulong> _rtvCountT = new();
    
    public void OnConfigParsed(KnifeRoundConfig config)
    {
        Config = config;
        Stringlocalizer = Localizer;
        if(Config.GiveArmorOnKnifeRound < 0 || Config.GiveArmorOnKnifeRound > 2)
        {
            config.GiveArmorOnKnifeRound = 2;
            Console.WriteLine("|||||||||||||||||||||||||||||||||||| I N V A L I D ||||||||||||||||||||||||||||||||||||");
            Console.WriteLine("GiveArmorOnKnifeRound: is invalid, setting to default value (2) Please Choose 0 or 1 or 2");
            Console.WriteLine("GiveArmorOnKnifeRound (0) = No");
            Console.WriteLine("GiveArmorOnKnifeRound (1) = Give Armor");
            Console.WriteLine("GiveArmorOnKnifeRound (2) = Give Armor + Helmet");
            Console.WriteLine("|||||||||||||||||||||||||||||||||||| I N V A L I D ||||||||||||||||||||||||||||||||||||");
        }
    }

    public override void Load(bool hotReload)
    {
        AddCommandListener("jointeam", OnCommandJoinTeam, HookMode.Pre);
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
    }

    private HookResult OnCommandJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (Config.BlockTeamChangeOnVoteAndKnife && BlockTeam)
        {
            return HookResult.Handled;
        }
        return HookResult.Continue;
    }

    public void OnTick()
    {
        if(knifemode && BlockTeam)
        {
            var playerEntitiess = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var player in playerEntitiess)
            {
                if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid) continue;
                var playerid = player.SteamID;

                if (OnSpawn.ContainsKey(playerid))
                {
                    foreach (var weapon in player.PlayerPawn.Value.WeaponServices!.MyWeapons)
                    {
                        if (weapon is { IsValid: true, Value.IsValid: true } && !weapon.Value.DesignerName.Contains("weapon_knife"))
                        {

                            player.ExecuteClientCommand("slot3");
                            player.DropActiveWeapon();
                            weapon.Value.Remove();
                            
                        }
                    }
                }
            }
        }
        if(!knifemode)
        {
            if(TWINNER == true || CTWINNER == true)
            {
                var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
                var countct = Utilities.GetPlayers().Count(p => p.TeamNum == (int)CsTeam.CounterTerrorist && !p.IsHLTV);
                var countt = Utilities.GetPlayers().Count(p => p.TeamNum == (int)CsTeam.Terrorist && !p.IsHLTV);
                string Close = "</font>";
                string Red = "<font color='Red'>";
                string Cyan = "<font color='cyan'>";
                string Blue = "<font color='blue'>";
                string DarkBlue = "<font color='darkblue'>";
                string LightBlue = "<font color='lightblue'>";
                string Purple = "<font color='purple'>";
                string Yellow = "<font color='yellow'>";
                string Lime = "<font color='lime'>";
                string Magenta = "<font color='magenta'>";
                string Pink = "<font color='pink'>";
                string Grey = "<font color='grey'>";
                string Green = "<font color='green'>";
                string Orange = "<font color='orange'>";
                string NextLine = "<br>";
                string ImageOpen = "<img src='";
                string ImageClose = "' class=''>";

                if (timer > 0)
                {
                    if (stopwatch.ElapsedMilliseconds >= 1000)
                    {
                        timer--;
                        stopwatch.Restart();
                    }
                }
                foreach (var player in playerEntities)
                {
                    if (!player.IsValid) continue;
                    
                    if(TWINNER == true)
                    {
                        if(player.TeamNum == 3)
                        {
                            StringBuilder builder = new StringBuilder();
                            var required = (int)Math.Ceiling(countt * 0.6);

                            builder.AppendFormat(Localizer["When_CT_Lose"], Close,Red,Cyan,Blue,DarkBlue,LightBlue,Purple,Yellow,Lime,Magenta,Pink,Grey,Green,Orange,NextLine, currentVotesCT, currentVotesT, required,timer, ImageOpen, ImageClose);
                            var centerhtml = builder.ToString();
                            player?.PrintToCenterHtml(centerhtml);
                        }else if(player.TeamNum == 2)
                        {
                            StringBuilder builder = new StringBuilder();
                            var required = (int)Math.Ceiling(countt * 0.6);

                            builder.AppendFormat(Localizer["Winner_Message"], Close,Red,Cyan,Blue,DarkBlue,LightBlue,Purple,Yellow,Lime,Magenta,Pink,Grey,Green,Orange,NextLine, currentVotesCT, currentVotesT, required,timer, ImageOpen, ImageClose);
                            var centerhtml = builder.ToString();
                            player?.PrintToCenterHtml(centerhtml);
                        }
                    }else if(CTWINNER == true)
                    {
                        if(player.TeamNum == 3)
                        {
                            StringBuilder builder = new StringBuilder();
                            var required = (int)Math.Ceiling(countct * 0.6);

                            builder.AppendFormat(Localizer["Winner_Message"], Close,Red,Cyan,Blue,DarkBlue,LightBlue,Purple,Yellow,Lime,Magenta,Pink,Grey,Green,Orange,NextLine, currentVotesCT, currentVotesT, required,timer, ImageOpen, ImageClose);
                            var centerhtml = builder.ToString();
                            player?.PrintToCenterHtml(centerhtml);
                            
                        }else if(player.TeamNum == 2)
                        {
                            StringBuilder builder = new StringBuilder();
                            var required = (int)Math.Ceiling(countct * 0.6);

                            builder.AppendFormat(Localizer["When_T_Lose"], Close,Red,Cyan,Blue,DarkBlue,LightBlue,Purple,Yellow,Lime,Magenta,Pink,Grey,Green,Orange,NextLine, currentVotesCT, currentVotesT, required,timer, ImageOpen, ImageClose);
                            var centerhtml = builder.ToString();
                            player?.PrintToCenterHtml(centerhtml);
                        }
                    }
                }
                if (timer < 1)
                {
                    if (TWINNER && currentVotesCT > currentVotesT)
                    {
                        foreach(var pl in Utilities.GetPlayers().FindAll(x => x.IsValid))
                        { 
                            if(pl.TeamNum == 3)
                            {
                                pl.SwitchTeam(CsTeam.Terrorist);
                            }else if(pl.TeamNum == 2)
                            {
                                pl.SwitchTeam(CsTeam.CounterTerrorist);
                            }
                        }

                        Server.NextFrame(() =>
                        {
                            _rtvCountT.Clear();
                            _rtvCountCT.Clear();
                            TWINNER = false;
                            CTWINNER = false;
                            BlockTeam = false;
                            int x = Config.AfterWinningRestartXTimes;
                            for (int i = 1; i <= x; i++)
                            {
                                float interval = i * 2.0f;

                                AddTimer(interval, () =>
                                {
                                    string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                                    if (Config.AllowAllTalkOnKnifeRound)
                                    {
                                        Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                                    }
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            }
                        });
                    }
                    if (CTWINNER && currentVotesCT > currentVotesT)
                    {
                        Server.NextFrame(() =>
                        {
                            _rtvCountT.Clear();
                            _rtvCountCT.Clear();
                            TWINNER = false;
                            CTWINNER = false;
                            BlockTeam = false;
                            int x = Config.AfterWinningRestartXTimes;
                            for (int i = 1; i <= x; i++)
                            {
                                float interval = i * 2.0f;

                                AddTimer(interval, () =>
                                {
                                    string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                                    if (Config.AllowAllTalkOnKnifeRound)
                                    {
                                        Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                                    }
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            }
                        });
                    }
                    if (CTWINNER && currentVotesT > currentVotesCT)
                    {
                        foreach(var pl in Utilities.GetPlayers().FindAll(x => x.IsValid))
                        { 
                            if(pl.TeamNum == 3)
                            {
                                pl.SwitchTeam(CsTeam.Terrorist);
                            }else if(pl.TeamNum == 2)
                            {
                                pl.SwitchTeam(CsTeam.CounterTerrorist);
                            }
                        }

                        Server.NextFrame(() =>
                        {
                            _rtvCountT.Clear();
                            _rtvCountCT.Clear();
                            TWINNER = false;
                            CTWINNER = false;
                            BlockTeam = false;
                            int x = Config.AfterWinningRestartXTimes;
                            for (int i = 1; i <= x; i++)
                            {
                                float interval = i * 2.0f;

                                AddTimer(interval, () =>
                                {
                                    string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                                    if (Config.AllowAllTalkOnKnifeRound)
                                    {
                                        Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                                    }
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            }
                        });
                    }
                    if (TWINNER && currentVotesT > currentVotesCT)
                    {
                        Server.NextFrame(() =>
                        {
                            _rtvCountT.Clear();
                            _rtvCountCT.Clear();
                            TWINNER = false;
                            CTWINNER = false;
                            BlockTeam = false;
                            int x = Config.AfterWinningRestartXTimes;
                            for (int i = 1; i <= x; i++)
                            {
                                float interval = i * 2.0f;

                                AddTimer(interval, () =>
                                {
                                    string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                                    if (Config.AllowAllTalkOnKnifeRound)
                                    {
                                        Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                                    }
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            }
                        });
                    }
                    if (TWINNER && currentVotesT == currentVotesCT || CTWINNER && currentVotesT == currentVotesCT)
                    {
                        Server.NextFrame(() =>
                        {
                            _rtvCountT.Clear();
                            _rtvCountCT.Clear();
                            TWINNER = false;
                            CTWINNER = false;
                            BlockTeam = false;
                            int x = Config.AfterWinningRestartXTimes;
                            for (int i = 1; i <= x; i++)
                            {
                                float interval = i * 2.0f;

                                AddTimer(interval, () =>
                                {
                                    string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                                    
                                    if (Config.AllowAllTalkOnKnifeRound)
                                    {
                                        Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                                    }
                                }, TimerFlags.STOP_ON_MAPCHANGE);
                            }
                        });
                    }
                }
            }
        }else if(knifestarted == true)
            {
                var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
                
                string Close = "</font>";
                string Red = "<font color='Red'>";
                string Cyan = "<font color='cyan'>";
                string Blue = "<font color='blue'>";
                string DarkBlue = "<font color='darkblue'>";
                string LightBlue = "<font color='lightblue'>";
                string Purple = "<font color='purple'>";
                string Yellow = "<font color='yellow'>";
                string Lime = "<font color='lime'>";
                string Magenta = "<font color='magenta'>";
                string Pink = "<font color='pink'>";
                string Grey = "<font color='grey'>";
                string Green = "<font color='green'>";
                string Orange = "<font color='orange'>";
                string NextLine = "<br>";
                string ImageOpen = "<img src='";
                string ImageClose = "' class=''>";

                foreach (var player in playerEntities)
                {
                    StringBuilder builder = new StringBuilder();
                    
                    builder.AppendFormat(Localizer["Knife_Start_Message"], Close,Red,Cyan,Blue,DarkBlue,LightBlue,Purple,Yellow,Lime,Magenta,Pink,Grey,Green,Orange,NextLine, string.Empty, string.Empty, string.Empty,string.Empty, ImageOpen, ImageClose);
                    var centerhtml = builder.ToString();
                    player?.PrintToCenterHtml(centerhtml);
                }
            }
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;
        if(onroundstart)
        {
            if(knifemode)
            {
                BlockTeam = true;
                knifestarted = true;
                AddTimer(Config.MessageKnifeStartTimer, () =>
                {
                    knifestarted = false;
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }else if(!onroundstart)
        {
            mp_roundtime = ConVar.Find("mp_roundtime")!.GetPrimitiveValue<float>();
            mp_roundtime_defuse = ConVar.Find("mp_roundtime_defuse")!.GetPrimitiveValue<float>();
            mp_team_intro_time = ConVar.Find("mp_team_intro_time")!.GetPrimitiveValue<float>();
            sv_alltalk = ConVar.Find("sv_alltalk")!.GetPrimitiveValue<bool>();
            sv_full_alltalk = ConVar.Find("sv_full_alltalk")!.GetPrimitiveValue<bool>();
            sv_talk_enemy_dead = ConVar.Find("sv_talk_enemy_dead")!.GetPrimitiveValue<bool>();
            sv_talk_enemy_living = ConVar.Find("sv_talk_enemy_living")!.GetPrimitiveValue<bool>();
            sv_deadtalk = ConVar.Find("sv_deadtalk")!.GetPrimitiveValue<bool>();
            knifemode = true;
            onroundstart = true;
        }
        if(knifemode)
        {
            Server.NextFrame(() =>
            {
                Server.ExecuteCommand($"mp_team_intro_time 0.0; sv_buy_status_override 3; mp_roundtime {Config.KnifeRoundTimer}; mp_roundtime_defuse {Config.KnifeRoundTimer}; mp_give_player_c4 0");
                if(Config.AllowAllTalkOnKnifeRound)
                {
                    Server.ExecuteCommand($"sv_alltalk true; sv_deadtalk true; sv_full_alltalk true; sv_talk_enemy_dead true; sv_talk_enemy_living true;");
                }
            });
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult EventRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
    {
        if(onroundstart && knifemode)
        {
            BlockTeam = true; 
        }
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)return HookResult.Continue;
        var playerid = player.SteamID;
        if(knifemode && BlockTeam)
        {
            if (!OnSpawn.ContainsKey(playerid))
            {
                OnSpawn.Add(playerid, true);
            }

            if (OnSpawn.ContainsKey(playerid))
            {
                if(Config.GiveArmorOnKnifeRound == 1)
                {
                    player.GiveNamedItem("item_kevlar");
                }else if(Config.GiveArmorOnKnifeRound == 2)
                {
                    player.GiveNamedItem("item_assaultsuit");
                }
                Server.NextFrame(() =>
                {
                    AddTimer(2.0f, () =>
                    {
                        OnSpawn.Remove(playerid); 
                    }, TimerFlags.STOP_ON_MAPCHANGE);
                });
            }
        }else if(!knifemode)
        {
            if(TWINNER == true || CTWINNER == true)
            {
                Server.NextFrame(() =>
                {
                    
                    if(Config.FreezeOnVote)
                    {
                        
                        if(player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid){player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NONE;}
                        
                    }
                    
                });
            }
        }   
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Post)]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (@event == null || !knifemode) return HookResult.Continue;
        
        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        
        stopwatch.Start();
        timer = Config.VoteTimer;
        int countt = 0;
        int countct = 0;

        foreach (var player in playerEntities)
        {
            if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || !player.PlayerPawn.Value.IsValid)
                continue;

            if (player.TeamNum == (int)CsTeam.Terrorist && player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
            {
                countt++;
            }
        }

        foreach (var players in playerEntities)
        {
            if (players == null || !players.IsValid || players.PlayerPawn == null || !players.PlayerPawn.IsValid || players.PlayerPawn.Value == null || !players.PlayerPawn.Value.IsValid)
                continue;

            if (players.TeamNum == (int)CsTeam.CounterTerrorist && players.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
            {
                countct++;
            }
        }

        if (countt > countct)
        {
            BlockTeam = true;
            TWINNER = true;
            knifemode = false;
        }
        else if (countct > countt)
        {
            BlockTeam = true;
            CTWINNER = true;
            knifemode = false;
        }
        else
        {
            BlockTeam = true;
            CTWINNER = true;
            knifemode = false;
        }

        return HookResult.Continue;
    }

    [ConsoleCommand("css_ct", "change to ct")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY)]
    public void ChangeToCTTeamCommand(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player == null || !player.IsValid)return;
        
        if(TWINNER && player.TeamNum == 2)
        {
            targetPlayerName = player.PlayerName;
            if(!player.UserId.HasValue || string.IsNullOrEmpty(targetPlayerName))return;
            
            
            if (_rtvCountT.Contains(player!.SteamID))
            {
                _rtvCountT.Remove(player.SteamID);
                currentVotesT =  currentVotesT - 1;
            }

            if (_rtvCountCT.Contains(player!.SteamID))return;
            _rtvCountCT.Add(player.SteamID);
            
            
            var councT = Utilities.GetPlayers().Count(p => p.TeamNum == (int)CsTeam.Terrorist && !p.IsHLTV);
            var required = (int)Math.Ceiling(councT * 0.6);
            currentVotesCT = _rtvCountCT.Count;

            if (currentVotesCT >= required)
            {
                foreach(var pl in Utilities.GetPlayers().FindAll(x => x.IsValid))
                { 
                    if(pl.TeamNum == 3)
                    {
                        pl.SwitchTeam(CsTeam.Terrorist);
                    }else if(pl.TeamNum == 2)
                    {
                        pl.SwitchTeam(CsTeam.CounterTerrorist);
                    }
                }

                Server.NextFrame(() =>
                {
                    _rtvCountT.Clear();
                    _rtvCountCT.Clear();
                    TWINNER = false;
                    CTWINNER = false;
                    BlockTeam = false;
                    int x = Config.AfterWinningRestartXTimes;
                    for (int i = 1; i <= x; i++)
                    {
                        float interval = i * 2.0f;

                        AddTimer(interval, () =>
                        {
                            string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                            
                            if (Config.AllowAllTalkOnKnifeRound)
                            {
                                Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                            }
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                });
            }
        }else if(CTWINNER && player.TeamNum == 3)
        {
            targetPlayerName = player.PlayerName;
            if(!player.UserId.HasValue || string.IsNullOrEmpty(targetPlayerName))return;

            if (_rtvCountT.Contains(player!.SteamID))
            {
                _rtvCountT.Remove(player.SteamID);
                currentVotesT =  currentVotesT - 1;
            }
            if (_rtvCountCT.Contains(player!.SteamID))return;
            _rtvCountCT.Add(player.SteamID);

            var councCT = Utilities.GetPlayers().Count(p => p.TeamNum == (int)CsTeam.CounterTerrorist && !p.IsHLTV);
            var required = (int)Math.Ceiling(councCT * 0.6);
            currentVotesCT = _rtvCountCT.Count;

            if (currentVotesCT >= required)
            {
                Server.NextFrame(() =>
                {
                    _rtvCountT.Clear();
                    _rtvCountCT.Clear();
                    TWINNER = false;
                    CTWINNER = false;
                    BlockTeam = false;
                    int x = Config.AfterWinningRestartXTimes;
                    for (int i = 1; i <= x; i++)
                    {
                        float interval = i * 2.0f;

                        AddTimer(interval, () =>
                        {
                            string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                            
                            if (Config.AllowAllTalkOnKnifeRound)
                            {
                                Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                            }
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                });
            }
        }
        
    }

    [ConsoleCommand("css_t", "change to t")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY)]
    public void ChangeToTTeamCommand(CCSPlayerController? player, CommandInfo cmd)
    {
        if (player == null || !player.IsValid)return;
        
        if(CTWINNER && player.TeamNum == 3)
        {
            targetPlayerName = player.PlayerName;
            if(!player.UserId.HasValue || string.IsNullOrEmpty(targetPlayerName))return;
            
            
            if (_rtvCountCT.Contains(player!.SteamID))
            {
                _rtvCountCT.Remove(player.SteamID);
                currentVotesCT =  currentVotesCT - 1;
            }

            if (_rtvCountT.Contains(player!.SteamID))return;
            _rtvCountT.Add(player.SteamID);
            
            var councCT = Utilities.GetPlayers().Count(p => p.TeamNum == (int)CsTeam.CounterTerrorist && !p.IsHLTV);
            var required = (int)Math.Ceiling(councCT * 0.6);
            currentVotesT = _rtvCountT.Count;

            if (currentVotesT >= required)
            {
                foreach(var pl in Utilities.GetPlayers().FindAll(x => x.IsValid))
                { 
                    if(pl.TeamNum == 3)
                    {
                        pl.SwitchTeam(CsTeam.Terrorist);
                    }else if(pl.TeamNum == 2)
                    {
                        pl.SwitchTeam(CsTeam.CounterTerrorist);
                    }
                }

                Server.NextFrame(() =>
                {
                    _rtvCountT.Clear();
                    _rtvCountCT.Clear();
                    TWINNER = false;
                    CTWINNER = false;
                    BlockTeam = false;
                    int x = Config.AfterWinningRestartXTimes;
                    for (int i = 1; i <= x; i++)
                    {
                        float interval = i * 2.0f;

                        AddTimer(interval, () =>
                        {
                            string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                            if (Config.AllowAllTalkOnKnifeRound)
                            {
                                Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                            }
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                });
            }
            
        }else if(TWINNER && player.TeamNum == 2)
        {
            targetPlayerName = player.PlayerName;
            if(!player.UserId.HasValue || string.IsNullOrEmpty(targetPlayerName))return;
           
            if (_rtvCountCT.Contains(player!.SteamID))
            {
                _rtvCountCT.Remove(player.SteamID);
                currentVotesCT =  currentVotesCT - 1;
            }

            if (_rtvCountT.Contains(player!.SteamID))return;
            _rtvCountT.Add(player.SteamID);
            
            var councT = Utilities.GetPlayers().Count(p => p.TeamNum == (int)CsTeam.Terrorist && !p.IsHLTV);
            var required = (int)Math.Ceiling(councT * 0.6);
            currentVotesT = _rtvCountT.Count;

            if (currentVotesT >= required)
            {
                Server.NextFrame(() =>
                {
                    _rtvCountT.Clear();
                    _rtvCountCT.Clear();
                    TWINNER = false;
                    CTWINNER = false;
                    BlockTeam = false;
                    int x = Config.AfterWinningRestartXTimes;
                    for (int i = 1; i <= x; i++)
                    {
                        float interval = i * 2.0f;

                        AddTimer(interval, () =>
                        {
                            string test = mp_roundtime.ToString();
                                    string test2 = mp_roundtime_defuse.ToString();
                                    string test3 = mp_team_intro_time.ToString();
                                    if (test.Contains(',') || test2.Contains(',') || test3.Contains(','))
                                    {
                                        string replacedValue = test.Replace(',', '.');
                                        string replacedValue2 = test2.Replace(',', '.');
                                        string replacedValue3 = test3.Replace(',', '.');
                                        Server.ExecuteCommand($"mp_team_intro_time {replacedValue3}; sv_buy_status_override -1; mp_roundtime {replacedValue}; mp_roundtime_defuse {replacedValue2}; mp_give_player_c4 1; mp_restartgame 1");
                                    }else
                                    {
                                        Server.ExecuteCommand($"mp_team_intro_time {mp_team_intro_time}; sv_buy_status_override -1; mp_roundtime {mp_roundtime}; mp_roundtime_defuse {mp_roundtime_defuse}; mp_give_player_c4 1; mp_restartgame 1");
                                    }
                            if (Config.AllowAllTalkOnKnifeRound)
                            {
                                Server.ExecuteCommand($"sv_alltalk {sv_alltalk}; sv_deadtalk {sv_deadtalk}; sv_full_alltalk {sv_full_alltalk}; sv_talk_enemy_dead {sv_talk_enemy_dead}; sv_talk_enemy_living {sv_talk_enemy_living};");
                            }
                        }, TimerFlags.STOP_ON_MAPCHANGE);
                    }
                });
            }
        }
    }

    public override void Unload(bool hotReload)
    {
        OnSpawn.Clear();
        _rtvCountT.Clear();
        _rtvCountCT.Clear();
        knifemode = false;
        CTWINNER = false;
        TWINNER = false;
        BlockTeam = false;
        onroundstart = false;
        knifestarted = false;
        targetPlayerName = "";
        currentVotesT = 0;
        currentVotesCT = 0;
    }
    private void OnMapEnd()
    {
        OnSpawn.Clear();
        _rtvCountT.Clear();
        _rtvCountCT.Clear();
        knifemode = false;
        CTWINNER = false;
        TWINNER = false;
        BlockTeam = false;
        onroundstart = false;
        knifestarted = false;
        targetPlayerName = "";
        currentVotesT = 0;
        currentVotesCT = 0;
    }
}