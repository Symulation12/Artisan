﻿using Artisan.Autocraft;
using Artisan.FCWorkshops;
using Artisan.RawInformation;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace Artisan
{
    internal class RecipeWindowUI : Window
    {
        public RecipeWindowUI() : base($"###RecipeWindow", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNavInputs)
        {
            this.Size = new Vector2(0, 0);
            this.Position = new Vector2(0, 0);
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            this.SizeConstraints = new WindowSizeConstraints()
            {
                MaximumSize = new Vector2(0,0),
            };
        }

        public override void Draw()
        {
           

            if (!Service.Configuration.DisableMiniMenu)
            {
                if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Crafting] || Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.PreparingToCraft])
                    DrawOptions();

                DrawEnduranceCounter();

                DrawWorkshopOverlay();

            }
            DrawMacroOptions();
        }

        private unsafe void DrawWorkshopOverlay()
        {
            try
            {
                var subWindow = Service.GameGui.GetAddonByName("SubmarinePartsMenu", 1);
                if (subWindow == IntPtr.Zero)
                    return;

                var addonPtr = (AtkUnitBase*)subWindow;
                if (addonPtr == null)
                    return;

                if (addonPtr->UldManager.NodeListCount < 38)
                    return;

                var node = addonPtr->UldManager.NodeList[2];

                if (!node->IsVisible)
                    return;

                var position = AtkResNodeFunctions.GetNodePosition(node);
                var scale = AtkResNodeFunctions.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;
                var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                var textSize = ImGui.CalcTextSize("Create crafting list for this phase");

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + (4f * scale.X), position.Y + size.Y - textSize.Y - (34f * scale.Y)));

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                float oldSize = ImGui.GetFont().Scale;
                ImGui.GetFont().Scale *= scale.X;
                ImGui.PushFont(ImGui.GetFont());
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10f, 5f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(3f, 3f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
                ImGui.Begin($"###WorkshopButton{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);


                if (ImGui.Button("Create crafting list for this phase"))
                {
                    var itemNameNode = addonPtr->UldManager.NodeList[37]->GetAsAtkTextNode();
                    var phaseProgress = addonPtr->UldManager.NodeList[26]->GetAsAtkTextNode();

                    if (LuminaSheets.WorkshopSequenceSheet.Values.Any(x => x.ResultItem.Value.Name.ExtractText() == itemNameNode->NodeText.ExtractText()))
                    {
                        var project = LuminaSheets.WorkshopSequenceSheet.Values.First(x => x.ResultItem.Value.Name.ExtractText() == itemNameNode->NodeText.ExtractText());
                        var phaseNum = Convert.ToInt32(phaseProgress->NodeText.ToString().First().ToString());

                        if (project.CompanyCraftPart.Count(x => x.Row > 0) == 1)
                        {
                            var part = project.CompanyCraftPart.First(x => x.Row > 0).Value;
                            var phase = part.CompanyCraftProcess[phaseNum - 1];

                            FCWorkshopUI.CreatePhaseList(phase.Value!, part.CompanyCraftType.Value.Name.ExtractText(), phaseNum, false, null, project);
                            Notify.Success("FC Workshop List Created");
                        }
                        else
                        {
                            var currentPartNode = addonPtr->UldManager.NodeList[28]->GetAsAtkTextNode();
                            string partStep = currentPartNode->NodeText.ExtractText().Split(":").Last();

                            if (project.CompanyCraftPart.Any(x => x.Value.CompanyCraftType.Value.Name.ExtractText() == partStep))
                            {
                                var part = project.CompanyCraftPart.First(x => x.Value.CompanyCraftType.Value.Name.ExtractText() == partStep).Value;
                                var phase = part.CompanyCraftProcess[phaseNum - 1];

                                FCWorkshopUI.CreatePhaseList(phase.Value!, part.CompanyCraftType.Value.Name.ExtractText(), phaseNum, false, null, project);
                                Notify.Success("FC Workshop List Created");
                            }
                        }
                    }
                }

                if (ImGui.Button("Create crafting list for this phase (including precrafts)"))
                {
                    var itemNameNode = addonPtr->UldManager.NodeList[37]->GetAsAtkTextNode();
                    var phaseProgress = addonPtr->UldManager.NodeList[26]->GetAsAtkTextNode();

                    if (LuminaSheets.WorkshopSequenceSheet.Values.Any(x => x.ResultItem.Value.Name.ExtractText() == itemNameNode->NodeText.ExtractText()))
                    {
                        var project = LuminaSheets.WorkshopSequenceSheet.Values.First(x => x.ResultItem.Value.Name.ExtractText() == itemNameNode->NodeText.ExtractText());
                        var phaseNum = Convert.ToInt32(phaseProgress->NodeText.ToString().First().ToString());

                        if (project.CompanyCraftPart.Count(x => x.Row > 0) == 1)
                        {
                            var part = project.CompanyCraftPart.First(x => x.Row > 0).Value;
                            var phase = part.CompanyCraftProcess[phaseNum - 1];

                            FCWorkshopUI.CreatePhaseList(phase.Value!, part.CompanyCraftType.Value.Name.ExtractText(), phaseNum, true, null, project);
                            Notify.Success("FC Workshop List Created");
                        }
                        else
                        {
                            var currentPartNode = addonPtr->UldManager.NodeList[28]->GetAsAtkTextNode();
                            string partStep = currentPartNode->NodeText.ExtractText().Split(":").Last();

                            if (project.CompanyCraftPart.Any(x => x.Value.CompanyCraftType.Value.Name.ExtractText() == partStep))
                            {
                                var part = project.CompanyCraftPart.First(x => x.Value.CompanyCraftType.Value.Name.ExtractText() == partStep).Value;
                                var phase = part.CompanyCraftProcess[phaseNum - 1];

                                FCWorkshopUI.CreatePhaseList(phase.Value!, part.CompanyCraftType.Value.Name.ExtractText(), phaseNum, true, null, project);
                                Notify.Success("FC Workshop List Created");
                            }
                        }
                    }
                }

                ImGui.End();
                ImGui.PopStyleVar(5);
                ImGui.GetFont().Scale = oldSize;
                ImGui.PopFont();
                ImGui.PopStyleColor();

            }
            catch { }
        }

        public override void PreDraw()
        {
            if (!P.config.DisableTheme)
            {
                P.Style.Push();
                ImGui.PushFont(P.CustomFont);
                P.StylePushed = true;
            }
        }

        public override void PostDraw()
        {
            if (P.StylePushed)
            {
                P.Style.Pop();
                ImGui.PopFont();
                P.StylePushed = false;
            }
        }


        public unsafe static void DrawOptions()
        {
            var recipeWindow = Service.GameGui.GetAddonByName("RecipeNote", 1);
            if (recipeWindow == IntPtr.Zero)
                return;

            var addonPtr = (AtkUnitBase*)recipeWindow;
            if (addonPtr == null)
                return;

            var baseX = addonPtr->X;
            var baseY = addonPtr->Y;

            if (addonPtr->UldManager.NodeListCount > 1)
            {
                if (addonPtr->UldManager.NodeList[1]->IsVisible)
                {
                    var node = addonPtr->UldManager.NodeList[1];

                    if (!node->IsVisible)
                        return;

                    if (Service.Configuration.LockMiniMenu)
                    {
                        var position = AtkResNodeFunctions.GetNodePosition(node);
                        var scale = AtkResNodeFunctions.GetNodeScale(node);
                        var size = new Vector2(node->Width, node->Height) * scale;
                        var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                        //position += ImGuiHelpers.MainViewport.Pos;

                        ImGuiHelpers.ForceNextWindowMainViewport();

                        if ((AtkResNodeFunctions.ResetPosition && position.X != 0) || Service.Configuration.LockMiniMenu)
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.Always);
                            AtkResNodeFunctions.ResetPosition = false;
                        }
                        else
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                        }
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                    ImGui.Begin($"###Options{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                        | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                    DrawCopyOfCraftMenu();

                    ImGui.End();
                    ImGui.PopStyleVar(2);
                }
            }

        }

        private static void DrawCopyOfCraftMenu()
        {
            if (ImGuiEx.AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new ImGuiEx.HeaderIconOptions() { Tooltip = "Open Config" }))
            {
                P.PluginUi.IsOpen = true;
            }

            bool autoMode = Service.Configuration.AutoMode;

            if (ImGui.Checkbox("Automatic Action Execution Mode", ref autoMode))
            {
                Service.Configuration.AutoMode = autoMode;
                Service.Configuration.Save();
            }

            bool enable = Handler.Enable;
            if (ImGui.Checkbox("Endurance Mode Toggle", ref enable))
            {
                Handler.ToggleEndurance(enable);
            }

        }

        public unsafe static void DrawMacroOptions()
        {
            var recipeWindow = Service.GameGui.GetAddonByName("RecipeNote", 1);
            if (recipeWindow == IntPtr.Zero)
                return;

            var addonPtr = (AtkUnitBase*)recipeWindow;
            if (addonPtr == null)
                return;

            var baseX = addonPtr->X;
            var baseY = addonPtr->Y;

            if (addonPtr->UldManager.NodeListCount >= 2 && addonPtr->UldManager.NodeList[1]->IsVisible)
            {
                var node = addonPtr->UldManager.NodeList[1];

                if (Service.Configuration.UserMacros.Count == 0)
                    return;

                if (!node->IsVisible)
                    return;

                var position = AtkResNodeFunctions.GetNodePosition(node);
                var scale = AtkResNodeFunctions.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;
                var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);

                ImGuiHelpers.ForceNextWindowMainViewport();
                if ((AtkResNodeFunctions.ResetPosition && position.X != 0) || Service.Configuration.LockMiniMenu)
                {
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                    AtkResNodeFunctions.ResetPosition = false;
                }
                else
                {
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                }

                //Dalamud.Logging.PluginLog.Debug($"{position.X + node->Width + 7}");
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.Begin($"###Options{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                ImGui.Spacing();
                if (Handler.RecipeID != 0)
                {
                    ImGui.Text($"Use a macro for this recipe ({Handler.RecipeName})");
                    string? preview = Service.Configuration.IRM.TryGetValue((uint)Handler.RecipeID, out var prevMacro) ? Service.Configuration.UserMacros.First(x => x.ID == prevMacro).Name : "";
                    if (ImGui.BeginCombo("", preview))
                    {
                        if (ImGui.Selectable(""))
                        {
                            Service.Configuration.IRM.Remove((uint)Handler.RecipeID);
                            Service.Configuration.Save();
                        }
                        foreach (var macro in Service.Configuration.UserMacros)
                        {
                            bool selected = Service.Configuration.IRM.TryGetValue((uint)Handler.RecipeID, out var selectedMacro);
                            if (ImGui.Selectable(macro.Name, selected))
                            {
                                Service.Configuration.IRM[(uint)Handler.RecipeID] = macro.ID;
                                Service.Configuration.Save();
                            }
                        }

                        ImGui.EndCombo();
                    }
                }
                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }

        internal static unsafe void DrawEnduranceCounter()
        {
            if (Handler.RecipeID == 0)
                return;

            var recipeWindow = Service.GameGui.GetAddonByName("RecipeNote", 1);
            if (recipeWindow == IntPtr.Zero)
                return;

            var addonPtr = (AtkUnitBase*)recipeWindow;
            if (addonPtr == null)
                return;

            var baseX = addonPtr->X;
            var baseY = addonPtr->Y;

            if (addonPtr->UldManager.NodeListCount >= 5)
            {
                //var node = addonPtr->UldManager.NodeList[1]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4];
                var node = addonPtr->UldManager.NodeList[8];

                var position = AtkResNodeFunctions.GetNodePosition(node);
                var scale = AtkResNodeFunctions.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;
                var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                //position += ImGuiHelpers.MainViewport.Pos;
                var textHeight = ImGui.CalcTextSize("Craft X Times:");
                var craftableCount = addonPtr->UldManager.NodeList[35]->GetAsAtkTextNode()->NodeText.ToString() == "" ? 0 : Convert.ToInt32(addonPtr->UldManager.NodeList[35]->GetAsAtkTextNode()->NodeText.ToString().GetNumbers());

                if (craftableCount == 0) return;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + (4f * scale.X) - 40f, position.Y - 16f - (17f * scale.Y)));

                //Dalamud.Logging.PluginLog.Debug($"Length: {size.Length()}, Width: {node->Width}, Scale: {scale.Y}");

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5f, 2.5f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(3f, 3f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
                ImGui.GetFont().Scale = scale.X;
                var oldScale = ImGui.GetIO().FontGlobalScale;
                ImGui.GetIO().FontGlobalScale = 1f;
                ImGui.PushFont(ImGui.GetFont());

                ImGui.Begin($"###Repeat{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

                ImGui.AlignTextToFramePadding();
                ImGui.Text("Craft X Times:");
                ImGui.SameLine();
                ImGui.PushItemWidth(110f * scale.X);
                if (ImGui.InputInt($"###TimesRepeat{node->NodeID}", ref Service.Configuration.CraftX))
                {
                    if (Service.Configuration.CraftX < 0)
                        Service.Configuration.CraftX = 0;

                    if (Service.Configuration.CraftX > craftableCount)
                        Service.Configuration.CraftX = craftableCount;

                }
                ImGui.SameLine();
                if (Service.Configuration.CraftX > 0)
                {
                    if (ImGui.Button($"Craft {Service.Configuration.CraftX}"))
                    {
                        Service.Configuration.CraftingX = true;
                        Handler.Enable = true;
                    }
                }
                else
                {
                    if (ImGui.Button($"Craft All ({craftableCount})"))
                    {
                        Service.Configuration.CraftX = craftableCount;
                        Service.Configuration.CraftingX = true;
                        Handler.Enable = true;
                    }
                }

                ImGui.End();

                ImGui.GetFont().Scale = 1;
                ImGui.GetIO().FontGlobalScale = oldScale;
                ImGui.PopFont();
                ImGui.PopStyleVar(5);
                ImGui.PopStyleColor();
            }
        }
    }
}
