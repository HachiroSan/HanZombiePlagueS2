<div align="center"><h1><img width="600" height="131" alt="Han Zombie Plague S2" src="https://github.com/user-attachments/assets/d0316faa-c2d0-478f-a642-1e3c3651f1d4" /></h1></div>

<div class="section">
<div align="center"><h1>Zombie Plague for Swiftly2</h1></div>

<div align="center"><strong>A CS2 Zombie Plague plugin built on the Swiftly2 framework.</strong></div>
<div align="center"><strong>Supports multiple custom configurations, zombie classes, game modes, item systems, sound systems, and API expansion.</strong></div>
</div>

<div align="center">

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z31PY52N)

## Videos
YouTube: https://www.youtube.com/watch?v=DVeR5u28M_s

Bilibili: https://www.bilibili.com/video/BV1c3cJzrEWn
</div>

---

Example Workshop files
```
sound : 3644652779
zombie models : 3170427476
```

---

<div align="center">
  <a href="./README.md"><img src="https://flagcdn.com/48x36/gb.png" alt="English" width="48" height="36" /> <strong>English</strong></a>
  &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <a href="./README.zh-CN.md"><img src="https://flagcdn.com/48x36/cn.png" alt="简体中文" width="48" height="36" /> <strong>简体中文</strong></a>
</div>

<hr>

# Han Zombie Plague S2

**Zombie Plague Plugin**
A Zombie Plague mode plugin for Counter-Strike 2. Featuring rich game modes, special zombie/human classes, prop systems, and full API support, it brings chaotic, customizable outbreak gameplay to your server.

## Feature Overview

- **10 Diverse Game Modes**: From classic infection to special class confrontations, all fully configurable.
- **Special Class System**: Mother Zombie, Nemesis, Assassin, Survivor, Sniper, Hero, etc. Each class has independent attributes (health, speed, gravity, damage, model, weapon).
- **Props & Abilities**: T-Virus Grenade (infection area), Incendiary Grenade, Flashbang, Freeze Grenade, Teleport Grenade, SCBA Suit (infection immunity), God Mode, Infinite Ammo, Infinite Clip, No Recoil.
- **Custom Configuration**: Per-mode toggle for infinite ammo and zombie respawn; global settings for knockback force, spawn points, sound effects, ambient music, and more.
- **Player Interaction**: Menu-based zombie class preference selection (saved to database), admin menu, and kill damage HUD display.
- **API Support**: Complete event system (infection, class selection, victory, etc.) for other plugins to extend custom logic.
- **Sound & Visuals**: Dedicated sound effects (infection, prop usage), player glow outline, FOV adjustment, and looping ambient atmosphere sounds.
- **Balance Optimization**: Knockback system with separate multipliers (head/body/ground/air), plus independent hero knockback config.

## Game Modes

The plugin provides **10 classic and innovative modes**, each with independent configuration:
- **Zombie Respawn Toggle** (`ZombieCanReborn`)
- **Human Infinite Ammo** (`EnableInfiniteClipMode`)
- **Mode Weight** (`Weight`, used for random mode selection)

1. **Normal Infection Mode**
   Select 1 player (configurable count) as Mother Zombie to start infecting humans. Classic progressive spread gameplay.

2. **Multi Infection Mode**
   Select half the players as Mother Zombies to start infecting simultaneously. Fast entry into high-intensity chaos.

3. **Survivor Mode**
   Select 1 human as Survivor, equipped with M249 machine gun plus special attributes (high health, speed, low gravity, high damage - all configurable). All others become zombies for a lone survival battle.

4. **Sniper Mode**
   Select 1 human as Sniper, equipped with an AWP plus special attributes (high health, speed, low gravity, high damage - configurable). All others become zombies for precision shooting versus the horde.

5. **Swarm Mode (Confrontation Mode)**
   Half the players instantly become zombies with no infection phase for direct human versus zombie firepower battle.

6. **Plague Mode**
   Half the players become zombies plus 1 Survivor plus 1 Nemesis for epic multi-faction chaos.

7. **Assassin Mode**
   Select 1 zombie as Assassin (invisible at long range, visible when close or attacked - invisibility distance configurable). No infection, focused on stealth assassination gameplay.

8. **Nemesis Mode**
   Select 1 zombie as Nemesis (high-stat boss). No infection, ultimate single boss versus all humans.

9. **Hero Mode**
   The last x surviving humans (count configurable) automatically become Heroes with ultra-strong attributes and continue the fight.

10. **Sniper vs Assassin Mode**
    Half the players become zombies plus 1 Sniper plus 1 Assassin for an intense three-way faction battle.

## Configuration Guide

Main configuration file: `HZPMainCFG.json`.

## Global Configuration

The following table lists the key global settings that apply to all modes unless overridden by mode-specific options.

| Parameter | Description | Example Value |
|----------------------------|--------------------------------------------------|----------------------------------------------------|
| `RoundReadyTime` | Round preparation time (seconds) | 22.0 |
| `RoundTime` | Round duration (minutes) | 4.0 |
| `HumandefaultModel` | Default human model path | "characters/models/ctm_st6/ctm_st6_variante.vmdl" |
| `HumanMaxHealth` | Maximum health for humans | 225 |
| `HumanInitialSpeed` | Initial movement speed for humans (multiplier) | 1.0 |
| `HumanInitialGravity` | Initial gravity scale for humans | 0.8 |
| `EnableDamageHud` | Show kill damage HUD | true |
| `EnableInfiniteReserveAmmo` | Infinite reserve ammo for humans | true |
| `EnableWeaponNoRecoil` | Weapons have no recoil | true |
| `HumanSpawnPoints` | Human spawn points (CT/T/DM) | "CT,T,DM" |
| `ZombieSpawnPoints` | Zombie spawn points (CT/T/DM) | "CT,T,DM" |
| `KnockZombieForce` | Knockback force applied to zombies | 250.0 |
| `StunZombieTime` | Stun duration when knocking back zombies (seconds) | 0.1 |
| `AmbSound` | List of ambient atmosphere sounds (comma-separated) | "han.zombie.amb.zriot,..." |
| `AmbSoundLoopTime` | Ambient sound loop interval (seconds) | 60.0 |
| `AmbSoundVolume` | Ambient sound volume | 0.8 |

### Knockback System

The knockback system allows customizable force when humans shoot zombies, helping balance gameplay and prevent zombies from rushing too easily.

- `HumanKnockBackHeadMultiply`: Headshot knockback multiplier (2.0)
- `HumanKnockBackBodyMultiply`: Body shot knockback multiplier (1.0)
- `HumanKnockBackGroundMultiply`: Ground knockback multiplier (1.0)
- `HumanKnockBackAirMultiply`: Airborne knockback multiplier (0.5)
- `HumanHeroKnockBackMultiply`: Knockback multiplier when the shooter is a Hero (1.0)

### Props Configuration

| Prop | Toggle Parameter | Auto-Give on Spawn Parameter | Range / Duration | Damage / Effect | Sound Effect |
|-------------------|---------------------------|------------------------------|----------------------|----------------------------------|---------------------------------------|
| T-Virus Grenade | - | - | 300.0 | Can infect Heroes | "han.zombieplague.grenadedote" |
| Incendiary Grenade | `FireGrenade` | `SpawnGiveFireGrenade` | 300.0 / 5.0s | 500 initial + 10/s burning | "han.zombieplague.grenadedote" |
| Incendiary Bomb | - | `SpawnGiveIncGrenade` | - | Burning damage | - |
| Flashbang / Light Grenade | `LightGrenade` | `SpawnGiveLightGrenade` | 1000.0 / 30.0s | Blinding / strong light effect | "C4.ExplodeTriggerTrip" |
| Freeze Grenade | `FreezeGrenade` | `SpawnGiveFreezeGrenade` | 300.0 / 10.0s | Freezes target | "han.zombieplague.grenadedote" |
| Teleport Grenade | `TelportGrenade` | `SpawnGiveTelportGrenade` | - | Teleports player | - |
| T-Virus Serum | - | - | - | Turns zombie back to human (special zombies immune) | "HealthShot.Pickup" |
| SCBA Suit (Chemical Suit) | `CanUseScbaSuit` | - | - | Immune to infection | Pickup: "Player.PickupPistol"<br>Broken: "Breakable.Flesh" |

### Mode Configuration Examples

Each mode has its own independent settings:
- `Enable`: Toggle the mode on or off
- `Name`: Display name in-game
- `Weight`: Random selection weight (higher = more likely to be chosen)

Specific per-mode parameters:
- **Normal Infection**: `MotherZombieCount` (number of Mother Zombies, default 1)
- **Survivor**: `SurvivorHealth` (1000), `SurvivorSpeed` (3.0), custom model/weapon paths
- **Sniper**: `SniperHealth` (500), `SniperWeapon` ("weapon_awp")
- **Assassin**: `InvisibilityDist` (invisibility distance, 200.0)
- **Hero**: `HeroCount` (number of Heroes, e.g. 3)

For the full JSON configuration, see the `configs/` folder in the repository.

## Installation Guide

1. Download the plugin package and extract it to `addons/swiftlys2/plugins/`.
2. Start or restart the server.
3. Edit and configure `HZPMainCFG.json` and other config files as needed.
4. Ensure dependencies: the plugin requires the SwiftlyS2 framework to be installed and running.

After installation, load the map or reload the plugin if necessary. Check server console and logs for any loading errors.

## Commands List

- `!zclass` or `sw_zclass`: Opens the zombie class selection menu (player preference, command can be freely customized in config).
- `!admin` or `sw_admin` by default: Opens the admin item menu (command comes from `AdminMenuItemCommand`, permission comes from `AdminMenuPermission`; if the permission is left empty, accessible to everyone).
- `!hzp_respawn` / `sw_hzp_respawn`: Respawn target players.
- `!hzp_bring` / `sw_hzp_bring`: Teleport target players in front of you.
- `!hzp_goto` / `sw_hzp_goto`: Teleport to another player.
- `!hzp_clean` / `sw_hzp_clean`: Remove dropped world weapons.
- `!hzp_csay` / `sw_hzp_csay`: Send a center-screen admin message.
- `!hzp_human` / `sw_hzp_human`: Force living zombies back to human form.
- `!hzp_zombie` / `sw_hzp_zombie`: Force living humans into direct zombie form.
- `!hzp_infect` / `sw_hzp_infect`: Infect living humans using HZP infection flow.
- `!hzp_mother` / `sw_hzp_mother`: Turn living humans into Mother Zombies.
- `!hzp_nemesis` / `sw_hzp_nemesis`: Turn living humans into Nemesis.
- `!hzp_assassin` / `sw_hzp_assassin`: Turn living humans into Assassin.
- `!hzp_hero` / `sw_hzp_hero`: Turn living humans into Hero.
- `!hzp_survivor` / `sw_hzp_survivor`: Turn living humans into Survivor.
- `!hzp_sniper` / `sw_hzp_sniper`: Turn living humans into Sniper.
- `!hzp_cash_add` / `sw_hzp_cash_add`: Add HZP cash to target players.
- `!hzp_cash_set` / `sw_hzp_cash_set`: Set HZP cash balance for target players.
- `!hzp_tvaccine` / `sw_hzp_tvaccine`: Give a T-Virus Antidote to eligible zombies.
- `!hzp_scba` / `sw_hzp_scba`: Equip an eligible human with a Hazmat Suit.
- `!hzp_god` / `sw_hzp_god`: Grant God Mode to a living player, optionally with custom seconds.
- `!hzp_infiniteammo` / `sw_hzp_infiniteammo`: Grant Infinite Ammo to an eligible human, optionally with custom seconds.
- `!hzp_tvirusgrenade` / `sw_hzp_tvirusgrenade`: Give a Virus Grenade to an eligible zombie.
- `!hzp_addhealth` / `sw_hzp_addhealth`: Give bonus HP to an eligible human, optionally with a custom amount.
- `!hzp_humanwin` / `sw_hzp_humanwin`: Force an immediate human victory during an active round.
- `!hzp_zombiewin` / `sw_hzp_zombiewin`: Force an immediate zombie victory during an active round.
- `!hzp_checkround` / `sw_hzp_checkround`: Re-check HZP round win conditions during an active round.
- `!hzp_restartround` / `sw_hzp_restartround`: Trigger the HZP round restart flow.

## Permissions

- HanZombiePlagueS2 only checks Swiftly permissions and does not require the external `Admins` plugin as a source or runtime dependency.
- Default admin-menu permission: `hzp.adminmenu` from `configs/plugins/HanZombiePlagueS2/HZPMainCFG.jsonc`.
- The built-in `hzp_*` admin utility commands reuse the same `AdminMenuPermission` gate as the admin item menu.
- Reserved broad VIP permission for future use: `hzp.vip`.
- You can grant these permissions from either `addons/swiftlys2/configs/permissions.jsonc` or any external admin/group plugin that writes Swiftly permissions.

Example `permissions.jsonc` setup:

```json
{
  "Permissions": {
    "Players": {
      "76561198111700953": [
        "hzp_admin"
      ]
    },
    "PermissionGroups": {
      "hzp_admin": [
        "hzp.adminmenu"
      ],
      "hzp_vip": [
        "hzp.vip"
      ]
    }
  }
}
```

---

## Zombie Class Configuration

The plugin supports a rich zombie class system, divided into two separate configuration files:

- **HZPZombieClassCFG.json**: List of normal zombie classes (`ZombieClassList`).
  These are the standard zombie types players may become after normal infection (for example Red Skull, White Skull, Xenomorph Queen, and more).

- **HZPSpecialClassCFG.json**: List of special zombie classes (`SpecialClassList`).
  These are the special roles used in specific modes (for example Mother Zombie, Nemesis, Assassin, and more).

**Both files use exactly the same format**. They are separated only to distinguish between normal zombies and special mode-specific zombies.

Each zombie class follows this structure:

```json
{
  "Name": "Class Name",
  "Enable": true,
  "PrecacheSoundEvent": "...",
  "Stats": { ... },
  "Models": { ... },
  "Sounds": { ... }
}
```

---

### Main Config Matching Mechanism for Zombie Classes

In `HZPMainCFG.json`, special modes match zombie classes by name fields. Example:

```json
"Nemesis": {
  "Enable": true,
  "Name": "Nemesis Mode",
  "NemesisNames": "Nemesis",
  "Weight": 50
}
```

Fields like `NemesisNames`, `AssassinNames`, and `SurvivorNames` must exactly match the `Name` value in the corresponding config file. If the name does not match, the class is disabled, or the class does not exist, the plugin will fail to load that role and may cause mode errors.

---

### Stats Parameters

| Parameter | Description | Example Value | Notes |
|---------------------|--------------------------------------------------|---------------|--------------------------------------------|
| Health | Maximum health in normal state | 8000 | - |
| MotherZombieHealth | Maximum health when acting as Mother Zombie | 18000 | Only applies in Mother Zombie modes |
| Speed | Movement speed multiplier (1.0 = default human speed) | 1.0 ~ 2.5 | Higher value = faster movement |
| Damage | Base melee attack damage | 50.0 | Claw/knife damage |
| Gravity | Gravity scale (lower value = higher jumps, slower fall) | 0.7 | Typically 0.2 ~ 1.0 |
| Fov | Field of View (FOV) | 110 | Wider view for zombies |
| EnableRegen | Enable automatic health regeneration | true | - |
| HpRegenSec | Health regeneration interval (seconds) | 5.0 | - |
| HpRegenHp | Health restored per regeneration tick | 30 | - |
| ZombieSoundVolume | Volume of zombie-related sounds | 1.0 | Range: 0.0 ~ 1.0 |
| IdleInterval | Interval between idle sounds (seconds) | 70.0 | - |

| Parameter | Description | Example Path |
|-----------------------|------------------------------------------|-----------------------------------------------------------------------|
| ModelPath | Main zombie model path | characters/models/voikanaa/feral_ghoul_fonv/feral_ghoul_fonv.vmdl |
| CustomKinfeModelPath | Custom claw/knife model path (optional) | "" (uses default) |

| Parameter | Description | Example Sound Key(s) |
|---------------|--------------------------------------|-----------------------------------------------|
| SoundInfect | Sound played when infected | han.human.mandeath |
| SoundPain | Pain/injury sound | han.hl.zombie.pain |
| SoundHurt | Hurt sound | han.zombie.manclassic_hurt |
| SoundDeath | Death sound | han.zombie.manclassic_death |
| IdleSound | Idle or breathing sound (multiple allowed) | han.hl.nihilanth.idle,han.hl.nihilanth.idleb |
| RegenSound | Health regeneration sound | han.zombie.state.manheal |
| BurnSound | Sound when burning | han.zombieplague.zburn |
| ExplodeSound | Explosion or special death sound | han.hl.zombie.idle |
| HitSound | Sound when hitting an enemy | han.zombie.classic_hit |
| HitWallSound | Sound when hitting a wall | han.zombie.classic_hitwall |
| SwingSound | Sound when swinging or missing | han.zombie.classic_swing |

| Class Name | Normal Health | Mother Health | Speed | Gravity | FOV | Regen Interval / Amount | Special Notes |
|---------------------|---------------|---------------|-------|---------|-----|-------------------------|----------------------------------------|
| Red Skull | 8000 | 18000 | 1.0 | 0.7 | 110 | 5.0s / 30 | High durability, auto-regen |
| White Skull | 3000 | 13000 | 1.1 | 0.8 | 110 | 5.0s / 30 | Medium health, slightly faster speed |
| frozen | 5000 | 15000 | 1.7 | 0.7 | 110 | 1.0s / 150 | High regen rate (disabled) |
| Fat Guy | 5000 | 15000 | 1.7 | 0.8 | 110 | 1.0s / 150 | High regen rate (disabled) |
| Xenomorph Queen | 2500 | 12500 | 2.0 | 0.2 | 110 | 10.0s / 5 | Extremely low gravity, high speed, female sounds |
| Female Scientist Zombie | 1800 | 12000 | 1.8 | 0.5 | 110 | 10.0s / 5 | High speed, female sounds |

| Class Name | Normal Health | Mother Health | Speed | Gravity | FOV | Regen Interval / Amount | Special Notes |
|----------------|---------------|---------------|-------|---------|-----|-------------------------|--------------------------------------------|
| Mother Zombie | 15000 | 20000 | 1.5 | 0.5 | 110 | 1.0s / 50 | Initial infection source, high damage (150) |
| Nemesis | 30000 | 50000 | 2.0 | 0.3 | 120 | 1.0s / 50 | Ultimate boss, ultra-high health, low gravity |
| Assassin | 15000 | 35000 | 2.5 | 0.4 | 120 | 2.0s / 60 | Ultra-high speed, pairs with invisibility mechanic |

---

## Sound Broadcast System (Vox System)

The plugin includes a powerful sound broadcast system (Vox) that automatically plays voice announcements at key game moments such as round start, countdown, mode announcement, and victory declaration, greatly enhancing immersion and atmosphere.

Configuration file: **HZPVoxCFG.json**

### Vox System Structure

`VoxList` is an array where each element represents a complete voice broadcast package, such as CSOL Male, CSOL Female, or HL1 Male.

Each voice package follows this structure:

```json
{
  "Name": "Voice Package Name",
  "Enable": true,
  "PrecacheSoundEvent": "...",
  "RoundMusicVox": "...",
  "SecRemainVox": "...",
  "CoundDownVox": "...",
  "ZombieSpawnVox": "...",
  "NormalInfectionVox": "...",
  "MultiInfectionVox": "...",
  "NemesisVox": "...",
  "SurvivorVox": "...",
  "SwarmVox": "...",
  "PlagueVox": "...",
  "AssassinVox": "...",
  "SniperVox": "...",
  "AVSVox": "...",
  "HeroVox": "...",
  "HumanWinVox": "...",
  "ZombieWinVox": "..."
}
```

### Vox Parameters / Voice Trigger Events

| Parameter | Trigger Timing | Example Sound Key(s) | Notes |
|--------------------|-----------------------------------------|-----------------------------------------------------------|--------------------------------------------|
| RoundMusicVox | Round officially starts (prep time ends) | han.zombie.round.class_start | Often used for background music or opening voice |
| SecRemainVox | 20 seconds remaining in round | han.zombie.round.20secremain | Reminds players time is running out |
| CoundDownVox | Countdown 10~1 seconds (one per second) | han.zombie.round.mancdone,... | Supports 10 separate voices, played in sequence |
| ZombieSpawnVox | Zombie spawn or Mother Zombie appears | han.zombie.round.manzbcome | Builds tension |
| NormalInfectionVox | Normal Infection mode announcement | han.zombieplague.end.horror | Mode-specific voice |
| MultiInfectionVox | Multi Infection mode announcement | han.zombieplague.end.horror | - |
| NemesisVox | Nemesis mode announcement | han.zombieplague.type.nemesis | - |
| SurvivorVox | Survivor mode announcement | han.zombieplague.type.survivor | - |
| SwarmVox | Swarm / Legion mode announcement | han.zombieplague.end.horror | - |
| PlagueVox | Plague mode announcement | han.zombieplague.end.plague | - |
| AssassinVox | Assassin mode announcement | han.zombieplague.type.nemesis | - |
| SniperVox | Sniper mode announcement | han.zombieplague.type.survivor | - |
| AVSVox | Sniper vs Assassin mode announcement | han.zombieplague.type.nemesis | - |
| HeroVox | Hero mode announcement | han.zombieplague.type.survivor | - |
| HumanWinVox | Humans win | han.zombie.round.manhmwin | Multiple allowed - random playback |
| ZombieWinVox | Zombies win | han.zombie.round.manzbwin | Multiple allowed - random playback |

### Voice Package Examples

| Voice Package Name | Style Source | Enabled | Special Features |
|----------------------|-----------------------|---------|----------------------------------------------------------------------------------|
| CSOL Male Broadcast | CSOL Male Style | true | Classic male announcer, clear and powerful countdown, exciting victory voice |
| CSOL Female Broadcast | CSOL Female Style | true | Female voice, gentle yet tense countdown, suitable for varied atmospheres |
| HL1 Male Broadcast | Half-Life 1 Male | true | Retro HL1 style, classic infection sounds and victory announcements |
| HL1 Female Broadcast | Half-Life 1 Female | true | HL1 female voice, unique retro atmosphere, special remaining time reminders |
| Zombie Plague Broadcast | Zombie Plague Classic | true | Mix of various classic voices, rich victory announcements, strong random playback effect |

**Customization Tips**:
- Each voice package can be independently enabled or disabled (`"Enable": true/false`).
- For the same event, you can list multiple sound keys (comma-separated); the system will randomly play one of them for variety.
- All voices must be pre-cached in the specified `PrecacheSoundEvent` file.
- You can add your own custom voice packages, such as Japanese, Korean, or localized voices, as long as the sound file paths are correct.

**Recommended Usage**:
- Servers can switch voice packages based on event themes, such as horror style for Halloween or cheerful for holidays.
- Combining different voice packages with modes creates stronger thematic immersion, such as HL1 package with retro maps or CSOL package with high-intensity matches.

---

## API Support

Full API interface (`IHanZombiePlagueAPI`) is provided, supporting event listening, player status queries, forced class setting, and more.
See details in [HanZombiePlagueAPI.xml](API/net10.0/HanZombiePlagueAPI.xml)

This API allows other plugins to:
- Listen to key events such as `HZP_OnPlayerInfect`, `HZP_OnNemesisSelected`, `HZP_OnGameStart`, and `HZP_OnHumanWin`
- Query player states such as whether a player is a zombie or Nemesis, and what the current mode is
- Forcefully set player roles and classes such as Survivor, Nemesis, or Hero
- Interact with game flow such as checking win conditions, giving props, and setting glow, FOV, or god mode
