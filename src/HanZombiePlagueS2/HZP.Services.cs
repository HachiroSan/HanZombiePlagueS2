using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Helpers;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.SteamAPI;
using static HanZombiePlagueS2.HZPZombieClassCFG;
using static Npgsql.Replication.PgOutput.Messages.RelationMessage;


namespace HanZombiePlagueS2;

public partial class HZPServices
{
    private readonly ILogger<HZPServices> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HZPGlobals _globals;
    private readonly HZPHelpers _helpers;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly IOptionsMonitor<HZPZombieClassCFG> _zombieClassCFG;
    private readonly IOptionsMonitor<HZPSpecialClassCFG> _specialClassCFG;
    private readonly PlayerZombieState _zombieState;
    private readonly HZPGameMode _gameMode;
    private readonly HZPLoadoutMenu _loadoutMenu;
    private readonly HZPLoadoutState _loadoutState;
    private readonly HZPStoreState _storeState;

    private readonly HanZombiePlagueAPI _api;
    public HZPServices(ISwiftlyCore core, ILogger<HZPServices> logger,
        HZPGlobals globals, HZPHelpers helpers, 
        IOptionsMonitor<HZPMainCFG> mainCFG,
        IOptionsMonitor<HZPZombieClassCFG> zombieClassCFG,
        PlayerZombieState zombieState, HZPGameMode gameMode,
        HZPLoadoutMenu loadoutMenu,
        HZPLoadoutState loadoutState,
        HZPStoreState storeState,
        IOptionsMonitor<HZPSpecialClassCFG> specialClassCFG,
        HanZombiePlagueAPI api)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
        _mainCFG = mainCFG;
        _zombieClassCFG = zombieClassCFG;
        _zombieState = zombieState;
        _gameMode = gameMode;
        _loadoutMenu = loadoutMenu;
        _loadoutState = loadoutState;
        _storeState = storeState;
        _specialClassCFG = specialClassCFG;
        _api = api;
    }

    public void SelectMotherZombie(int count)
    {
        var allplayer = _core.PlayerManager.GetAllPlayers();
        var candidates = new List<IPlayer>();

        foreach (var p in allplayer)
        {
            if (p == null || !p.IsValid)
                continue;

            var id = p.PlayerID;
            bool isZombie = false;
            _globals.IsZombie.TryGetValue(id, out isZombie);
            if (isZombie)
                continue;

            candidates.Add(p);
        }

        if (candidates.Count == 0)
            return;

        // 随机打乱候选列表
        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(candidates));

        // 确保不超过候选人数
        int actualCount = Math.Min(count, candidates.Count);

        var zombieConfig = _zombieClassCFG.CurrentValue;
        var zombieClasses = zombieConfig.ZombieClassList;

        for (int i = 0; i < actualCount; i++)
        {
            var target = candidates[i];
            if (target == null || !target.IsValid)
                continue;

            SetupMotherZombie(target);
            _helpers.SendChatToAllT("GameInfoBecomeMother", target.Name);

        }
    }

    public void Infect(IPlayer attacker, IPlayer victim, bool grenade)
    {
        if (attacker == null || !attacker.IsValid)
            return;

        if (victim == null || !victim.IsValid)
            return;

        _helpers.RemoveGlow(victim);

        var attackerId = attacker.PlayerID;
        var victimId = victim.PlayerID;
        var victimsteamId = victim.SteamID;

        var CFG = _mainCFG.CurrentValue;

        _globals.IsZombie.TryGetValue(attackerId, out bool attackerIsZombie);
        _globals.IsZombie.TryGetValue(victimId, out bool victimIsZombie);
        if (attackerIsZombie && !victimIsZombie)
        {
            _globals.ScbaSuit.TryGetValue(victimId, out bool IsHaveScbaSuit);
            if (IsHaveScbaSuit && CFG.CanUseScbaSuit)
            {
                _helpers.RemoveScbaSuit(victim, CFG.ScbaSuitBrokenSound);
                return;
            }

            var zombieConfig = _zombieClassCFG.CurrentValue;
            var zombieClasses = zombieConfig.ZombieClassList;

            // 根据玩家偏好选择类
            var preference = _zombieState.GetPlayerPreference(victimId, victimsteamId);
            ZombieClass? selectedClass;

            if (preference != null && preference.Preference == ZombiePreference.Fixed)
            {
                selectedClass = zombieClasses.FirstOrDefault(c => c.Name == preference.FixedZombieName);
            }
            else
            {
                selectedClass = _zombieState.PickRandomZombieClass(zombieClasses);
            }

            if (selectedClass != null)
            {
                PlayerSelectSoundtoAll(selectedClass.Sounds.SoundInfect, selectedClass.Stats.ZombieSoundVolume);
                posszombie(victim, selectedClass, false);
                SendZombieClassReveal(victim, selectedClass);
                CreateFakeKill(attacker, victim, grenade);
                CheckRoundWinConditions();

                if (_api != null)
                    _api.NotifyInfect(attacker, victim, grenade, selectedClass.Name);
            }
        }

        SetupHero();
    }

    public void ForceCommandInfect(IPlayer Infecter, bool IgnoreScbaSuit)
    {
        if (Infecter == null || !Infecter.IsValid)
            return;

        _helpers.RemoveGlow(Infecter);

        var InfecterId = Infecter.PlayerID;
        var InfectersteamId = Infecter.SteamID;
        var CFG = _mainCFG.CurrentValue;
        _globals.IsZombie.TryGetValue(InfecterId, out bool InfecterIsZombie);

        if (InfecterIsZombie)
            return;

        
        _globals.ScbaSuit.TryGetValue(InfecterId, out bool IsHaveScbaSuit);
        if (IsHaveScbaSuit && CFG.CanUseScbaSuit && !IgnoreScbaSuit)
        {
            _helpers.RemoveScbaSuit(Infecter, CFG.ScbaSuitBrokenSound);
            return;
        }

        var zombieConfig = _zombieClassCFG.CurrentValue;
        var zombieClasses = zombieConfig.ZombieClassList;

        // 根据玩家偏好选择类
        var preference = _zombieState.GetPlayerPreference(InfecterId, InfectersteamId);
        ZombieClass? selectedClass;

        if (preference != null && preference.Preference == ZombiePreference.Fixed)
        {
            selectedClass = zombieClasses.FirstOrDefault(c => c.Name == preference.FixedZombieName);
        }
        else
        {
            selectedClass = _zombieState.PickRandomZombieClass(zombieClasses);
        }

        if (selectedClass != null)
        {
            PlayerSelectSoundtoAll(selectedClass.Sounds.SoundInfect, selectedClass.Stats.ZombieSoundVolume);
            posszombie(Infecter, selectedClass, false);
            SendZombieClassReveal(Infecter, selectedClass);
            CreateFakeKill(Infecter, Infecter, false);
            CheckRoundWinConditions();

            if (_api != null)
                _api.NotifyInfect(Infecter, Infecter, false, selectedClass.Name);
        }
        

        SetupHero();
    }



    public void SetPlayerZombie(IPlayer player)
    {
        if (player == null || !player.IsValid)
            return;
        var Id = player.PlayerID;
        var steamId = player.SteamID;

        _helpers.RemoveSHumanClass(Id);
        _helpers.RemoveSZombieClass(Id);
        _helpers.RemoveGlow(player);

        _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
        if (!IsZombie)
        {
            var zombieConfig = _zombieClassCFG.CurrentValue;
            var zombieClasses = zombieConfig.ZombieClassList;

            // 根据玩家偏好选择类
            var preference = _zombieState.GetPlayerPreference(Id, steamId);
            ZombieClass? selectedClass;

            if (preference != null && preference.Preference == ZombiePreference.Fixed)
            {
                selectedClass = zombieClasses.FirstOrDefault(c => c.Name == preference.FixedZombieName);
            }
            else
            {
                selectedClass = _zombieState.PickRandomZombieClass(zombieClasses);
            }

            if (selectedClass != null)
            {
                PlayerSelectSoundtoAll(selectedClass.Sounds.SoundInfect, selectedClass.Stats.ZombieSoundVolume);
                posszombie(player, selectedClass, false);
                SendZombieClassReveal(player, selectedClass);
                CreateFakeKill(player, player, false);
                CheckRoundWinConditions();
            }
        }

        SetupHero();
    }

    public void SetPlayerHuman(IPlayer player)
    {
        if (player == null || !player.IsValid)
            return;

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid)
            return;

        var Id = player.PlayerID;

        _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
        if (!IsZombie)
            return;

        var CFG = _mainCFG.CurrentValue;

        _helpers.RemoveSZombieClass(Id);

        _globals.IsZombie[Id] = false;
        player.SwitchTeam(Team.CT);
        _helpers.ChangeKnife(player, false, false);
        _helpers.SetFov(player, 90);
        _helpers.ClearPlayerBurn(Id);
        _helpers.ClearFreezeStaten(player);


        string Default = "characters/models/ctm_st6/ctm_st6_variante.vmdl";
        string Custom = string.IsNullOrEmpty(CFG.HumandefaultModel) ? Default : CFG.HumandefaultModel;

        pawn.SetModel(Custom);

        var maxHealth = CFG.HumanMaxHealth;
        pawn.MaxHealth = maxHealth;
        pawn.MaxHealthUpdated();
        pawn.Health = maxHealth;
        pawn.HealthUpdated();

        pawn.VelocityModifier = CFG.HumanInitialSpeed;
        pawn.VelocityModifierUpdated();

        _helpers.EmitSoundFormPlayer(player, CFG.TVaccineSound, 1.0f);
        CheckRoundWinConditions();
    }


    public void FakeHumanWins()
    {
        _globals.GameStart = false;

        if (_globals.RoundVoxGroup != null)
        {
            PlayerSelectSoundtoAll(_globals.RoundVoxGroup.HumanWinVox, _globals.RoundVoxGroup.Volume);
        }

        _helpers.SendCenterToAllT("ServerGameHumanWin");
        _helpers.SetTeamScore(Team.CT);
        _helpers.TerminateRound(RoundEndReason.CTsWin, 5.0f);

        if (_api != null)
            _api.NotifyHumanWin(true);
    }

    
    
    public void FakeZombieWins()
    {
        _globals.GameStart = false;

        if (_globals.RoundVoxGroup != null)
        {
            PlayerSelectSoundtoAll(_globals.RoundVoxGroup.ZombieWinVox, _globals.RoundVoxGroup.Volume);
        }
        _helpers.SendCenterToAllT("ServerGameZombieWin");
        _helpers.SetTeamScore(Team.T);
        _helpers.TerminateRound(RoundEndReason.TerroristsWin, 5.0f);

        if (_api != null)
            _api.NotifyHumanWin(false);
    }

    public void posszombie(IPlayer zombie, ZombieClass Zclass, bool isMother)
    {
        try
        {
            if(!_globals.GameStart)
                return;

            if (zombie == null || !zombie.IsValid)
                return;

            var controller = zombie.Controller;
            if (controller == null || !controller.IsValid)
                return;

            //_logger.LogInformation($"posszombie 开始 [{controller.PlayerName}]: {Zclass.Name}");

            if (Zclass == null)
                return;

            var pawn = zombie.PlayerPawn;
            if (pawn == null || !pawn.IsValid)
                return;

            //_logger.LogInformation($"生成丧尸 {Zclass.Name}");

            var Id = zombie.PlayerID;
            _loadoutState.ResetLifeState(Id);
            _storeState.ResetLifeState(Id);

            _helpers.RemoveSHumanClass(Id);

            var CFG = _mainCFG.CurrentValue;
            _helpers.RemoveScbaSuit(zombie, CFG.ScbaSuitBrokenSound);
            _helpers.RemoveGodState(zombie);
            _helpers.RemoveInfiniteAmmo(zombie);
            
            _globals.IsZombie[Id] = true;
            zombie.SwitchTeam(Team.T);

            _helpers.ShakeZombie(zombie);

            _zombieState.SetPlayerZombieClass(Id, Zclass.Name);

            string path = Zclass.Models.ModelPath;
            _core.Scheduler.NextWorldUpdate(() =>
            {
                pawn.SetModel(path);
            });
            
            _helpers.DropAllWeapon(zombie);

            bool CustomKinfe = !string.IsNullOrEmpty(Zclass.Models.CustomKinfeModelPath);
            _helpers.ChangeKnife(zombie, true, CustomKinfe);


            int ZHealth;
            if (isMother)
            {
                ZHealth = Zclass.Stats.MotherZombieHealth > 0 ? ZHealth = Zclass.Stats.MotherZombieHealth : ZHealth = 8000;
            }
            else
            {
                ZHealth = Zclass.Stats.Health > 0 ? ZHealth = Zclass.Stats.Health : ZHealth = 3000;
            }
            pawn.MaxHealth = ZHealth;
            pawn.MaxHealthUpdated();
            pawn.Health = ZHealth;
            pawn.HealthUpdated();

            pawn.ActualGravityScale = Zclass.Stats.Gravity;

            int fov = Zclass.Stats.Fov;
            _helpers.SetFov(zombie, fov);

            float zSpeed = Zclass.Stats.Speed > 0 ? zSpeed = Zclass.Stats.Speed : zSpeed = 1.0f;
            pawn.VelocityModifier = zSpeed;
            pawn.VelocityModifierUpdated();

            if (Zclass.Stats.EnableRegen)
            {
                var now = Environment.TickCount / 1000f;
                _globals.g_ZombieRegenStates[Id] = new ZombieRegenState
                {
                    PlayerID = Id,
                    RegenAmount = Zclass.Stats.HpRegenHp,
                    RegenInterval = Zclass.Stats.HpRegenSec,
                    NextRegenTime = now + Zclass.Stats.HpRegenSec 
                };
            }

            var origin = pawn.AbsOrigin;
            if (origin == null)
                return;

            Vector offsetPos = new(origin.Value.X, origin.Value.Y, origin.Value.Z + 50);
            var particle = _helpers.CreateParticleAtPos(pawn, offsetPos, "particles/explosions_fx/explosion_hegrenade_water_intial_trail.vpcf");
            //_logger.LogInformation($"posszombie 完成 [{controller.PlayerName}]");
        }
        catch (Exception ex)
        {
            var controller = zombie.Controller;
            if (controller == null || !controller.IsValid)
                return;

            _logger.LogError($"posszombie 异常 [{controller.PlayerName}]: {ex.Message}");
            _logger.LogError($"异常堆栈: {ex.StackTrace}");
            _logger.LogError($"僵尸类模型: {Zclass?.Models}");
        }
    }


    public void SetRoundEndTime()
    {
        var cvar = _core.ConVar.Find<float>("mp_roundtime");
        float roundSeconds = (cvar?.Value ?? 3) * 60f;
        _globals.g_hRoundEndTimer?.Cancel();
        _globals.g_hRoundEndTimer = null;

        _globals.g_hRoundEndTimer = _core.Scheduler.DelayBySeconds(roundSeconds, () =>
        {
            if (!_globals.GameStart || _globals.RestartRoundPendingForMinPlayers)
                return;

            if (!HasValidRoundPopulation(out _, out _, out _))
            {
                _globals.WaitingForPlayers = true;
                return;
            }

            FakeHumanWins();
        });
    }

    public void CreateFakeKill(IPlayer attacker, IPlayer victim, bool grenade)
    {
        if (attacker == null || !attacker.IsValid)
            return;

        if (victim == null || !victim.IsValid)
            return;

        string weapon = grenade ? "hegrenade" : "knife";
        _core.GameEvent.Fire<EventPlayerDeath>(@event =>
        {
            @event.Attacker = attacker.PlayerID;
            @event.UserId = victim.PlayerID;
            @event.Weapon = weapon;
        });
    }


    public void JoinTeamCheck(IPlayer player)
    {
        if (player is not { IsValid: true } || player.Controller is not { IsValid: true } ctrl)
            return;

        if (!_globals.GameStart)
        {
            if (!ctrl.PawnIsAlive)
            {
                ctrl.Respawn();
            }
            return;
        }
    }

   public void Round_Countdown()
    {
        var CFG = _mainCFG.CurrentValue;
        bool wasWaitingForPlayers = _globals.WaitingForPlayers;
        float tickInterval = CFG.CountdownTickInterval > 0 ? CFG.CountdownTickInterval : 1.0f;

        if (!HasValidRoundPopulation(out int realPlayers, out int totalEntities, out int requiredEntities))
        {
            _globals.WaitingForPlayers = true;

            if (_globals.RoundPrepActive && _globals.OutbreakAtUnixMs > 0)
                _globals.OutbreakAtUnixMs += (long)Math.Round(tickInterval * 1000f);

            _helpers.SendCenterToAllT("ServerWaitForPlayers", totalEntities, requiredEntities);
            return;
        }

        if (wasWaitingForPlayers)
        {
            TriggerMinPlayersRecoveryRestart();
            return;
        }

        _globals.WaitingForPlayers = false;

        if (!_globals.RoundPrepActive)
        {
            float prepSeconds = CFG.RoundReadyTime > 0 ? CFG.RoundReadyTime : 3.0f;

            _globals.RoundPrepActive = true;
            _globals.OutbreakAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                                        (long)Math.Round(prepSeconds * 1000f);
            _globals.LastAnnouncedCountdown = int.MinValue;
        }

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int currentDisplay = Math.Max(0, (int)Math.Ceiling((_globals.OutbreakAtUnixMs - nowMs) / 1000.0));
        _globals.Countdown = currentDisplay;

        int previousDisplay = _globals.LastAnnouncedCountdown;
        if (previousDisplay == int.MinValue)
            previousDisplay = currentDisplay + 1;

        if (currentDisplay == previousDisplay)
        {
            _helpers.SendCenterToAllT("ServerGameCountDown", currentDisplay);
            return;
        }

        _globals.LastAnnouncedCountdown = currentDisplay;

        if (_globals.RoundVoxGroup != null)
        {
            var vox = _globals.RoundVoxGroup;

            if (previousDisplay > 20 && currentDisplay <= 20 && currentDisplay > 0)
            {
                if (!string.IsNullOrWhiteSpace(vox.SecRemainVox))
                    PlayerSelectSoundtoAll(vox.SecRemainVox.Trim(), vox.Volume);
            }

            string[] soundList = string.IsNullOrWhiteSpace(vox.CoundDownVox)
                ? Array.Empty<string>()
                : vox.CoundDownVox.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            int upper = Math.Min(10, previousDisplay - 1);
            int lower = Math.Max(1, currentDisplay);

            for (int sec = upper; sec >= lower; sec--)
            {
                int soundIndex = sec - 1;
                if (soundIndex < 0 || soundIndex >= soundList.Length)
                    continue;

                string soundPath = soundList[soundIndex];
                if (string.IsNullOrWhiteSpace(soundPath))
                    continue;

                _helpers.EmitSoundToAll(soundPath, vox.Volume);
                break;
            }
        }

        if (currentDisplay <= 0)
        {
            _globals.g_hCountdown?.Cancel();
            _globals.g_hCountdown = null;
            _globals.RoundPrepActive = false;
            _globals.OutbreakAtUnixMs = 0;
            _globals.LastAnnouncedCountdown = int.MinValue;
            _globals.GameStart = true;

            _loadoutMenu.EnsureValidLoadoutToAll();

            if (_api != null)
                _api.NotifyGameStart(_globals.GameStart);

            _globals.GameInfiniteClipMode = _gameMode.InfiniteClipMode();

            CheckEndTimer();
            SwitchMode();
            _helpers.SetAmbSounds(CFG, _globals);

            var modeVox = _gameMode.SelectModeVox();
            if (_globals.RoundVoxGroup != null && !string.IsNullOrWhiteSpace(modeVox))
                PlayerSelectSoundtoAll(modeVox, _globals.RoundVoxGroup.Volume);

            _core.Scheduler.DelayBySeconds(1.0f, () =>
            {
                if (_globals.GameStart)
                    CheckRoundWinConditions();
            });

            return;
        }

        _helpers.SendCenterToAllT("ServerGameCountDown", currentDisplay);
    }


    public void ScheduleRoundPreparationStart(float roundReadySeconds, float delayAfterFreezeEndSeconds)
    {
        _globals.g_hCountdown?.Cancel();
        _globals.g_hCountdown = null;
        _globals.RoundPrepActive = false;
        _globals.OutbreakAtUnixMs = 0;
        _globals.LastAnnouncedCountdown = int.MinValue;

        float safeDelay = Math.Max(0f, delayAfterFreezeEndSeconds);
        _core.Scheduler.DelayBySeconds(safeDelay, () =>
        {
            if (_globals.GameStart || _globals.RestartRoundPendingForMinPlayers || _globals.g_hCountdown != null)
                return;

            float prepSeconds = roundReadySeconds > 0 ? roundReadySeconds : 3.0f;
            float tickInterval = _mainCFG.CurrentValue.CountdownTickInterval > 0
                ? _mainCFG.CurrentValue.CountdownTickInterval
                : 1.0f;

            _globals.RoundPrepActive = true;
            _globals.OutbreakAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)Math.Round(prepSeconds * 1000f);
            _globals.LastAnnouncedCountdown = int.MinValue;

            _globals.g_hCountdown = _core.Scheduler.DelayAndRepeatBySeconds(0.1f, tickInterval, () => Round_Countdown());
            _core.Scheduler.StopOnMapChange(_globals.g_hCountdown);
        });
    }

    public void CheckEndTimer()
    {
        if (_globals.g_hRoundEndTimer != null)
            return;

        if (!_globals.GameStart || _globals.RestartRoundPendingForMinPlayers)
            return;

        SetRoundEndTime();
    }

    public void EnterWaitingForPlayersState(bool serverEmpty)
    {
        _globals.WaitingForPlayers = true;
        _globals.GameStart = false;
        _globals.RestartRoundPendingForMinPlayers = false;
        _globals.RoundPrepActive = false;
        _globals.OutbreakAtUnixMs = 0;
        _globals.LastAnnouncedCountdown = int.MinValue;

        if (serverEmpty)
            _globals.ServerIsEmpty = true;

        _globals.g_hCountdown?.Cancel();
        _globals.g_hCountdown = null;
        _globals.g_hRoundEndTimer?.Cancel();
        _globals.g_hRoundEndTimer = null;
    }

    public void HandleServerEmptyTransition()
    {
        EnterWaitingForPlayersState(true);
        _globals.FreshRestartAfterEmptyPending = false;
    }

    public void RestartFreshAfterEmptyJoin()
    {
        if (_globals.FreshRestartAfterEmptyPending)
            return;

        _globals.FreshRestartAfterEmptyPending = true;
        EnterWaitingForPlayersState(false);
        _globals.ServerIsEmpty = false;

        _core.Scheduler.DelayBySeconds(3f, () =>
        {
            try
            {
                if (_helpers.GetEligibleParticipantCount() > 0)
                {
                    _core.Engine.ExecuteCommand("mp_restartgame 1");
                }
            }
            finally
            {
                _globals.FreshRestartAfterEmptyPending = false;
            }
        });
    }


    public void PlayerSelectSoundtoAll(string soundevent, float Volume)
    {
        if (_globals.RoundVoxGroup != null && !string.IsNullOrWhiteSpace(soundevent))
        {
            var sound = _helpers.RandomSelectSound(soundevent);
            if (sound != null)
            {
                _helpers.EmitSoundToAll(sound, Volume);
            }
        }
    }

    public void PlayerSelectSoundtoEntity(IPlayer player, string soundevent, float Volume)
    {
        if (_globals.RoundVoxGroup != null && !string.IsNullOrWhiteSpace(soundevent))
        {
            var sound = _helpers.RandomSelectSound(soundevent);
            if (sound != null)
            {
                _helpers.EmitSoundFormPlayer(player, sound, Volume);
            }
        }
    }

    public void CheckRoundWinConditions()
    {
        if (_globals.RestartRoundPendingForMinPlayers)
            return;

        if (!HasValidRoundPopulation(out _, out _, out _))
        {
            _globals.WaitingForPlayers = true;
            return;
        }

        if (_globals.WaitingForPlayers)
        {
            TriggerMinPlayersRecoveryRestart();
            return;
        }

        _globals.WaitingForPlayers = false;

        var allPlayers = _core.PlayerManager.GetAlive();
        int zombieCount = 0;
        int humanCount = 0;

        foreach (var p in allPlayers)
        {
            if (p == null || !p.IsValid)
                continue;

            if (p.PlayerPawn == null || !p.PlayerPawn.IsValid)
                continue;

            if (!p.Controller.PawnIsAlive)
                continue;

            var Id = p.PlayerID;
            _globals.IsZombie.TryGetValue(Id, out bool IsZombie);

            if (IsZombie)
                zombieCount++;
            else
                humanCount++;
        }

        if (zombieCount == 0)
            FakeHumanWins();
        else if (humanCount == 0)
            FakeZombieWins();
    }

    private int GetEffectiveMinPlayersToStart()
    {
        int configured = _mainCFG.CurrentValue.MinPlayersToStart;
        return Math.Max(1, _globals.RuntimeMinPlayersToStart ?? configured);
    }

    private bool HasValidRoundPopulation(out int realPlayers, out int totalEntities, out int requiredEntities)
    {
        realPlayers = _helpers.GetEligibleParticipantCount();
        totalEntities = _helpers.GetTotalEntityCount();
        requiredEntities = GetEffectiveMinPlayersToStart();
        return realPlayers >= 1 && totalEntities >= requiredEntities;
    }

    private void TriggerMinPlayersRecoveryRestart()
    {
        if (_globals.RestartRoundPendingForMinPlayers)
            return;

        _globals.RestartRoundPendingForMinPlayers = true;
        _globals.WaitingForPlayers = false;
        _globals.GameStart = false;
        _globals.RoundPrepActive = false;
        _globals.OutbreakAtUnixMs = 0;
        _globals.LastAnnouncedCountdown = int.MinValue;
        _globals.g_hCountdown?.Cancel();
        _globals.g_hCountdown = null;

        _logger.LogInformation("Using standard recovery restart on waiting->min transition");
        _core.Engine.ExecuteCommand("mp_restartgame 1");
    }

    public void ZombieRegenTimer()
    {
        _globals.g_ZombieRegenTimer?.Cancel();
        _globals.g_ZombieRegenTimer = null;
        _globals.g_ZombieRegenTimer = _core.Scheduler.RepeatBySeconds(0.2f, () =>
        {

            int now = Environment.TickCount / 1000;
            var allalive = _core.PlayerManager.GetAlive();
            foreach (var player in allalive)
            {
                try
                {
                    var Id = player.PlayerID;
                    _globals.IsZombie.TryGetValue(Id, out bool IsZombie);

                    if (!IsZombie)
                        continue;


                    if (!_globals.g_ZombieRegenStates.TryGetValue(Id, out var state))
                        continue;

                    var pawn = player.PlayerPawn;
                    if (pawn == null || !pawn.IsValid)
                        continue;

                    int maxHealth = pawn.MaxHealth;
                    if (pawn.Health >= maxHealth)
                        continue;

                    if (now < state.NextRegenTime)
                        continue;

                    if (pawn.AbsVelocity.Length() > 0)
                        continue;


                    var zombieConfig = _zombieClassCFG.CurrentValue;
                    var specialConfig = _specialClassCFG.CurrentValue;
                    var zombie = _zombieState.GetZombieClass(Id, zombieConfig.ZombieClassList, specialConfig.SpecialClassList);
                    if (zombie == null)
                        continue;

                    pawn.Health = Math.Min(pawn.Health + state.RegenAmount, maxHealth);
                    pawn.HealthUpdated();

                    //_logger.LogInformation($"{player.Controller.PlayerName}回血成功 恢复 {state.RegenAmount} 最大血量 {maxHealth} 当前 {pawn.Health}");

                    PlayerSelectSoundtoEntity(player, zombie.Sounds.RegenSound, zombie.Stats.ZombieSoundVolume);
                    state.NextRegenTime = now + state.RegenInterval;
                }
                catch (Exception ex)
                {
                    _core.Logger.LogError($"Regen Error: {ex.Message}");
                }
            }
        });

        _core.Scheduler.StopOnMapChange(_globals.g_ZombieRegenTimer);
    }

    public void GlobalIdleTimer()
    {
        _globals.g_IdleTimer?.Cancel();
        _globals.g_IdleTimer = null;

        _globals.g_IdleTimer = _core.Scheduler.RepeatBySeconds(0.1f, () =>
        {
            int now = Environment.TickCount / 1000;
            var allalive = _core.PlayerManager.GetAlive();
            foreach (var player in allalive)
            {
                try
                {
                    var Id = player.PlayerID;
                    _globals.IsZombie.TryGetValue(Id, out bool IsZombie);

                    if (!IsZombie)
                        continue;

                    if (!_globals.g_ZombieIdleStates.TryGetValue(Id, out var state))
                        continue;

                    var controller = player.Controller;
                    if (controller == null || !controller.IsValid)
                        continue;


                    if (!controller.PawnIsAlive)
                        continue;

                    if (now < state.NextIdleTime)
                        continue;

                    var zombieConfig = _zombieClassCFG.CurrentValue;
                    var specialConfig = _specialClassCFG.CurrentValue;
                    var zombie = _zombieState.GetZombieClass(Id, zombieConfig.ZombieClassList, specialConfig.SpecialClassList);
                    if (zombie == null)
                        continue;

                    PlayerSelectSoundtoEntity(player, zombie.Sounds.IdleSound, zombie.Stats.ZombieSoundVolume);
                    state.NextIdleTime = now + state.IdleInterval;
                }
                catch (Exception ex)
                {
                    _core.Logger.LogError($"Idle Error: {ex.Message}");
                }

            }
        });

        _core.Scheduler.StopOnMapChange(_globals.g_IdleTimer);
    }

    public void ShowDmgHud(IPlayer attacker, IPlayer victim, int damage)
    {
        if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid)
            return;

        var victimpawn = victim.PlayerPawn;
        if (victimpawn == null || !victimpawn.IsValid)
            return;

        int Maxhealth = victimpawn.MaxHealth;
        int health = victimpawn.Health;

        var ZombieClassName = _zombieState.GetPlayerZombieClass(victim.PlayerID);

        string message = $"<font size='-1'><font color='red'>{ZombieClassName}</font> | <font color='red'>{victim.Name}</font><br><font color='red'>{damage} DMG</font> → <font color='green'>{health}/{Maxhealth}</font></font>";

        attacker.SendCenterHTMLAsync(message);

    }

    public void ShowCurrentZombieClassInfo(IPlayer player)
    {
        if (player == null || !player.IsValid)
            return;

        var id = player.PlayerID;
        _globals.IsZombie.TryGetValue(id, out bool isZombie);
        if (!isZombie)
        {
            _helpers.SendChatT(player, "ZombieInfoNotZombie", FormatCommandLabel(_mainCFG.CurrentValue.ZombieClassCommand));
            return;
        }

        var zombie = _zombieState.GetZombieClass(
            id,
            _zombieClassCFG.CurrentValue.ZombieClassList,
            _specialClassCFG.CurrentValue.SpecialClassList);

        if (zombie == null)
        {
            _helpers.SendChatT(player, "ZombieInfoUnavailable");
            return;
        }

        SendZombieClassInfo(player, zombie, false);
    }

    private void SendZombieClassReveal(IPlayer player, ZombieClass zombieClass)
    {
        SendZombieClassInfo(player, zombieClass, true);
    }

    private void SendZombieClassInfo(IPlayer player, ZombieClass zombieClass, bool includeCenter)
    {
        if (player == null || !player.IsValid)
            return;

        string className = zombieClass.Name;
        if (includeCenter)
        {
            string centerMessage = $"<font size='-1'>{_helpers.T(player, "ZombieInfoBecome", className)}</font>";
            player.SendMessage(MessageType.Center, centerMessage);
        }

        _helpers.SendChatT(player, "ZombieInfoCurrent", className);
        _helpers.SendChatT(player, "ZombieInfoAbility", BuildZombieAbilitySummary(player, zombieClass));
        _helpers.SendChatT(player, "ZombieInfoHint", FormatCommandLabel(_mainCFG.CurrentValue.ZombieInfoCommand));
    }

    private string BuildZombieAbilitySummary(IPlayer player, ZombieClass zombieClass)
    {
        string configuredSummary = ResolveConfiguredAbilitySummary(player, zombieClass.AbilitySummary);
        if (!string.IsNullOrWhiteSpace(configuredSummary))
            return configuredSummary;

        var traits = new List<string>();
        var stats = zombieClass.Stats;

        if (stats.Health >= 12000)
        {
            traits.Add(_helpers.T(player, "ZombieTraitMassiveHealth"));
        }
        else if (stats.Health >= 6000)
        {
            traits.Add(_helpers.T(player, "ZombieTraitDurable"));
        }
        else if (stats.Health <= 2200)
        {
            traits.Add(_helpers.T(player, "ZombieTraitFragile"));
        }

        if (stats.Speed >= 2.2f)
        {
            traits.Add(_helpers.T(player, "ZombieTraitExtremeSpeed"));
        }
        else if (stats.Speed >= 1.5f)
        {
            traits.Add(_helpers.T(player, "ZombieTraitFast"));
        }

        if (stats.Gravity <= 0.35f)
        {
            traits.Add(_helpers.T(player, "ZombieTraitExtremeJump"));
        }
        else if (stats.Gravity <= 0.55f)
        {
            traits.Add(_helpers.T(player, "ZombieTraitHighJump"));
        }

        if (stats.EnableRegen)
        {
            if (stats.HpRegenSec <= 2.0f || stats.HpRegenHp >= 50)
            {
                traits.Add(_helpers.T(player, "ZombieTraitStrongRegen", stats.HpRegenHp, stats.HpRegenSec));
            }
            else
            {
                traits.Add(_helpers.T(player, "ZombieTraitRegen", stats.HpRegenHp, stats.HpRegenSec));
            }
        }

        if (stats.Damage >= 120.0f)
        {
            traits.Add(_helpers.T(player, "ZombieTraitHeavyDamage"));
        }
        else if (stats.Damage >= 75.0f)
        {
            traits.Add(_helpers.T(player, "ZombieTraitHardHitting"));
        }

        if (traits.Count == 0)
            return _helpers.T(player, "ZombieTraitBalanced");

        return string.Join(", ", traits.Distinct().Take(4));
    }

    private string ResolveConfiguredAbilitySummary(IPlayer player, string? configuredSummary)
    {
        if (string.IsNullOrWhiteSpace(configuredSummary))
            return string.Empty;

        string trimmed = configuredSummary.Trim();
        const string localizePrefix = "loc:";
        if (trimmed.StartsWith(localizePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string key = trimmed[localizePrefix.Length..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
                return _helpers.T(player, key);
        }

        return trimmed;
    }

    private static string FormatCommandLabel(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return "zinfo";

        string trimmed = command.Trim();
        if (trimmed.StartsWith('!'))
            return trimmed;

        if (trimmed.StartsWith("sw_", StringComparison.OrdinalIgnoreCase) && trimmed.Length > 3)
            return $"!{trimmed[3..]}";

        return trimmed;
    }

    public void RandomSpawnPoint(IPlayer player, bool isZombie)
    {
        if (player == null || !player.IsValid)
            return;

        var CFG = _mainCFG.CurrentValue;

        var spawnConfig = isZombie ? CFG.ZombieSpawnPoints : CFG.HumanSpawnPoints;
        var pool = _helpers.GetSpawnPool(spawnConfig);
        if (pool.Count == 0)
        {
            _logger.LogWarning($"[Spawn] Empty pool for {(isZombie ? "zombie" : "human")} spawn (config: '{spawnConfig}')");
            return;
        }

        var sp = pool[Random.Shared.Next(pool.Count)];

        if (float.IsNaN(sp.Position.X) || float.IsNaN(sp.Position.Y) || float.IsNaN(sp.Position.Z))
        {
            _logger.LogWarning("[Spawn] Selected spawn contains NaN position values, skipping teleport");
            return;
        }

        if (Math.Abs(sp.Position.Z) > 32768f)
        {
            _logger.LogWarning($"[Spawn] Selected spawn has suspicious Z value ({sp.Position.Z}), skipping teleport");
            return;
        }

        player.Teleport(sp.Position, sp.Angle);
    }

    public void GiveSpawnGrenade(IPlayer player, HZPMainCFG CFG)
    {
        if (CFG.SpawnGiveFireGrenade) 
            _helpers.GiveFireGrenade(player);

        if (CFG.SpawnGiveLightGrenade) 
            _helpers.GiveLightGrenade(player);

        if (CFG.SpawnGiveFreezeGrenade) 
            _helpers.GiveFreezeGrenade(player);

        if (CFG.SpawnGiveTelportGrenade) 
            _helpers.GiveTeleprotGrenade(player);

        if (CFG.SpawnGiveIncGrenade) 
            _helpers.GiveIncGrenade(player);

    }

}
