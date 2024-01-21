﻿using System.Numerics;
using ImGuiNET;

namespace DeeperDeepDungeonDex.System;

public class MobDataWindow : DeepDungeonWindow {
    private readonly IDrawableMob enemy;

    public MobDataWindow(string name, IDrawableMob enemyData) : base(name) {
        enemy = enemyData;
        
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(350.0f, 125.0f),
            MaximumSize = new Vector2(float.PositiveInfinity),
        };

        Flags |= ImGuiWindowFlags.NoTitleBar;
    }

    public override void PreDraw() {
        if (Plugin.Configuration.LockedMobWindows.Contains(enemy.Id)) {
            Flags |= ImGuiWindowFlags.NoMove;
            Flags |= ImGuiWindowFlags.NoResize;
        } else {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoResize;
        }
    }

    public override void Draw() {
        base.Draw();

        enemy.Draw(WindowExtraButton.CloseWithLock);
    }

    public override void OnClose() {
        Plugin.Controller.WindowController.RemoveWindow(this);
    }
}
