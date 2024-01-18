﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Dalamud.Plugin;
using DeeperDeepDungeonDex.Storage;
using DeeperDeepDungeonDex.System;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using Lumina.Excel.GeneratedSheets2;

namespace DeeperDeepDungeonDex;

public sealed class Plugin : IDalamudPlugin {
    public static Configuration Configuration = null!;
    public static StorageManager StorageManager = null!;
    public static DeeperDeepDungeonDexController Controller = null!;

    public Plugin(DalamudPluginInterface pluginInterface) {
        Strings.Culture = new CultureInfo(pluginInterface.UiLanguage);

        pluginInterface.Create<Services>();
        Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        StorageManager = new StorageManager();
        Task.Run(StorageManager.Load);

        Controller = new DeeperDeepDungeonDexController();
    }

    public void Dispose() {
        Controller.Dispose();
    }

    public static unsafe InstanceContentDeepDungeon* GetDirector() {
        var eventFramework = EventFramework.Instance();
        return eventFramework == null ? null : eventFramework->GetInstanceContentDeepDungeon();
    }

    public static unsafe bool InDeepDungeon() => GetDirector() != null;

    public static unsafe byte? GetFloor() {
        var director = GetDirector();
        if (director is null) return null;
        return director->Floor;
    }
    
    public static uint? GetFloorSetId() {
        if (GetFloor() is { } floor) {
            return (uint) (floor - (floor % 10) + 1);
        }
        
        return null;
    }
    public static int GetFloorSetId(int floor) {
        return (((floor / 10) * 10) + 1);
    }

    public static DeepDungeonType? GetDeepDungeonType() {
        if (Services.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(Services.ClientState.TerritoryType) is { } territoryInfo) {
            return territoryInfo switch {
                { TerritoryIntendedUse: 31, ExVersion.Row: 0 or 1 } => DeepDungeonType.PalaceOfTheDead,
                { TerritoryIntendedUse: 31, ExVersion.Row: 2 } => DeepDungeonType.HeavenOnHigh,
                { TerritoryIntendedUse: 31, ExVersion.Row: 4 } => DeepDungeonType.EurekaOrthos,
                _ => null
            };
        }

        return null;
    }
    
    public static string GetEnemyName(IDrawableMob enemy) {
        if (Services.DataManager.GetExcelSheet<BNpcName>() is { } bnpcNameSheet) {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(bnpcNameSheet.GetRow(enemy.Id)!.Singular);
        }

        throw new Exception($"Exception trying to get mob name from enemy#{enemy.Id}");
    }
}
