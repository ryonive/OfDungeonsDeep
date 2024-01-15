﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using DeeperDeepDungeonDex.Storage;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;
using AttackType = DeeperDeepDungeonDex.Storage.AttackType;
using Status = DeeperDeepDungeonDex.Storage.Status;

namespace DeeperDeepDungeonDex.System;

public interface IDrawableMob {
    string Name { get; }
    uint Id { get; set; }
    int? Hp { get; set; }
    AttackType? AttackType { get; set; }
    uint? AttackDamage { get; set; }
    string? AttackName { get; set; }
    int StartFloor { get; set; }
    int EndFloor { get; set; }
    Aggro Aggro { get; set; }
    Dictionary<Status, bool>? Vulnerabilities { get; set; }
    string? Image { get; set; }
    DeepDungeonType DungeonType { get; set; }
    List<Ability>? Abilities { get; set; }
    
    IDalamudTextureWrap? ImageSmall { get; set; }
    IDalamudTextureWrap? ImageLarge { get; set; }
    
    public void Draw(bool extendedInfo, WindowExtraButton buttonType) {
        var topSegmentSize = new Vector2(ImGui.GetContentRegionAvail().X, 110.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginChild("TopSegment",  topSegmentSize, false)) {
            var portraitHeight = 110.0f * ImGuiHelpers.GlobalScale;
            var portraitWidth = 110.0f * ImGuiHelpers.GlobalScale;
            var portraitSize = new Vector2(portraitWidth, portraitHeight);
            if (ImGui.BeginChild("##Portrait", portraitSize, false)) {
                DrawPortrait();
            }
            ImGui.EndChild();
            
            ImGui.SameLine();
            var portraitSideInfoSize = new Vector2(ImGui.GetContentRegionAvail().X, portraitHeight);
            if (ImGui.BeginChild("##PortraitSideInfo", portraitSideInfoSize, false)) {
                DrawPortraitSideInfo(buttonType);
            }
            ImGui.EndChild();
        }
        ImGui.EndChild();

        DrawAbilityList();

        if (extendedInfo) {
            DrawExtendedInfo();
        }
    }

    protected void DrawPortrait() {
        ImageSmall ??= Services.TextureProvider.GetTextureFromFile(new FileInfo( GetImagePath("Thumbnails")));
        ImageLarge ??= Services.TextureProvider.GetTextureFromFile(new FileInfo(GetImagePath("Images")));

        if (ImageSmall is not null && ImageLarge is not null) {
            var rectPosition = ImGui.GetCursorScreenPos();
            ImGui.Image(ImageSmall.ImGuiHandle, ImGui.GetContentRegionAvail());
            ImGui.GetWindowDrawList().AddRect(rectPosition, rectPosition + ImGui.GetContentRegionMax(), ImGui.GetColorU32(KnownColor.White.Vector() with { W = 0.75f }));
            
            if (ImGui.IsItemClicked()) {
                ImGui.OpenPopup("ImagePopup");
            }
        
            if (ImGui.BeginPopup("ImagePopup")) {
                ImGui.Image(ImageLarge.ImGuiHandle, ImageLarge.Size);
                ImGui.EndPopup();
            } else if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Click to see Full Resolution");
            }
        }
    }
    
    protected void DrawAbilityList() {
        Ability.DrawAbilityList(Abilities, this);
    }

    private void DrawExtendedInfo() {
    }
    
    protected void DrawPortraitSideInfo(WindowExtraButton buttonType) {
        if (ImGui.BeginTable("PortraitSideInfoTable", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoClip, ImGui.GetContentRegionAvail())) {
            ImGui.TableNextColumn();
            DrawMobName();

            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            DrawUtilityButton(buttonType);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawMobHealth();

            ImGui.TableNextColumn();
            DrawAutoAttackDamage();
                    
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawFloorRange();

            ImGui.TableNextColumn();
            DrawAggroType();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawVulnerabilities();
                    
            ImGui.EndTable();
        }
    }
    
    private void DrawMobName() {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(Name);
    }
    
    private void DrawMobHealth() {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextUnformatted(FontAwesomeIcon.Heart.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextUnformatted(Hp.ToString());
    }
    
    private void DrawAutoAttackDamage() {
        switch (this) {
            case FloorSet when AttackDamage is not null:
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextUnformatted(FontAwesomeIcon.Explosion.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.Text(AttackDamage.ToString());
                break;
            
            default:
                AttackType?.DrawAttack(AttackDamage, AttackName);
                break;
        }
    }
    
    private void DrawFloorRange() {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextUnformatted(FontAwesomeIcon.Stairs.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        switch (this) {
            case FloorSet:
                ImGui.TextUnformatted($"{StartFloor + 9}");
                break;
            
            case Enemy:
                ImGui.TextUnformatted($"{StartFloor} - {EndFloor}");
                break;
        }
    }
    
    private void DrawAggroType() {
        Aggro.Draw();
    }
    
    private void DrawVulnerabilities() {
        if (Vulnerabilities is null) return;
        
        foreach (var (status, isVulnerable) in Vulnerabilities) {
            if (Services.TextureProvider.GetIcon((uint) status) is { } image) {
                if (status is not Status.Resolution) {
                    ImGui.Image(image.ImGuiHandle, image.Size * 0.5f, Vector2.Zero, Vector2.One, isVulnerable ? Vector4.One : Vector4.One / 2.5f );
                    if (ImGui.IsItemHovered() && Services.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets2.Status>()?.FirstOrDefault(statusEffect => statusEffect.Icon == (uint)status) is {} statusInfo ) {
                        ImGui.SetTooltip(statusInfo.Name);
                    }
                } else {
                    ImGui.Image(image.ImGuiHandle, ImGuiHelpers.ScaledVector2(32.0f, 32.0f), Vector2.Zero, Vector2.One, isVulnerable ? Vector4.One : Vector4.One / 2.5f );
                    if (ImGui.IsItemHovered() && Services.DataManager.GetExcelSheet<DeepDungeonItem>()?.GetRow(16) is {} resolution) {
                        ImGui.SetTooltip(resolution.Name);
                    }
                }
                ImGui.SameLine();
            }
        }
    }
    
    private void DrawUtilityButton(WindowExtraButton buttonType) {
        switch (buttonType) {
            case WindowExtraButton.PopOut:
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 23.0f * ImGuiHelpers.GlobalScale);
                if (ImGuiComponents.IconButton("Button", FontAwesomeIcon.ArrowUpRightFromSquare)) {
                    Plugin.Controller.WindowController.TryAddMobDataWindow(this);
                } 
                break;
                        
            case WindowExtraButton.Close:
                ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - 18.0f * ImGuiHelpers.GlobalScale);
                if (ImGuiComponents.IconButton("Button", FontAwesomeIcon.Times)) {
                    Plugin.Controller.WindowController.RemoveWindowForEnemy(this);
                }
                break;
        }
    }
    
    private string GetImagePath(string folder) {
        if (Image is null) return string.Empty;
        
        return Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Data",
            folder,
            DungeonType switch {
                DeepDungeonType.PalaceOfTheDead => "potd",
                DeepDungeonType.HeavenOnHigh => "hoh",
                DeepDungeonType.EurekaOrthos => "eo",
                _ => throw new ArgumentOutOfRangeException(nameof(folder))
            },
            (((StartFloor / 10) * 10) + 1).ToString("000"),
            Image
        );
    }
}