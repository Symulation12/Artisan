﻿using Artisan.Autocraft;
using Artisan.CraftingLists;
using Artisan.CraftingLogic;
using Artisan.MacroSystem;
using Artisan.RawInformation;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static Artisan.CraftingLogic.CurrentCraft;

namespace Artisan.UI
{
    internal class CraftingWindow : Window
    {
        public bool repeatTrial = false;

        public CraftingWindow() : base("Artisan Crafting Window###MainCraftWindow", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize)
        {
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
        }

        public override bool DrawConditions()
        {
            return P.PluginUi.CraftingVisible;
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

        public static TimeSpan MacroTime = new();
        public override void Draw()
        {
            if (!Service.Configuration.DisableHighlightedAction)
                Hotbars.MakeButtonsGlow(CurrentRecommendation);

            if (ImGuiEx.AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new ImGuiEx.HeaderIconOptions() { Tooltip = "Open Config" }))
            {
                P.PluginUi.IsOpen = true;
            }

            bool autoMode = Service.Configuration.AutoMode;

            if (ImGui.Checkbox("Auto Action Mode", ref autoMode))
            {
                if (!autoMode)
                    ActionWatching.BlockAction = false;

                Service.Configuration.AutoMode = autoMode;
                Service.Configuration.Save();
            }

            if (autoMode)
            {
                var delay = Service.Configuration.AutoDelay;
                ImGui.PushItemWidth(200);
                if (ImGui.SliderInt("Set delay (ms)", ref delay, 0, 1000))
                {
                    if (delay < 0) delay = 0;
                    if (delay > 1000) delay = 1000;

                    Service.Configuration.AutoDelay = delay;
                    Service.Configuration.Save();
                }
            }


            if (Handler.RecipeID != 0 && !CraftingListUI.Processing && Handler.Enable)
            {
                if (ImGui.Button("Disable Endurance"))
                {
                    Handler.Enable = false;
                }
            }

            if (!Handler.Enable && DoingTrial)
                ImGui.Checkbox("Trial Craft Repeat", ref repeatTrial);

            if (Service.Configuration.IRM.ContainsKey((uint)Handler.RecipeID))
            {
                var macro = Service.Configuration.UserMacros.FirstOrDefault(x => x.ID == Service.Configuration.IRM[(uint)Handler.RecipeID]);
                ImGui.TextWrapped($"Using Macro: {macro.Name} ({(MacroStep >= macro.MacroActions.Count() ? macro.MacroActions.Count() : MacroStep + 1)}/{macro.MacroActions.Count()})");

                if (MacroStep >= macro.MacroActions.Count())
                {
                    ImGui.TextWrapped($"Macro has completed. {(!P.config.DisableMacroArtisanRecommendation ? "Now continuing with solver." : "Please continue to manually craft.")}");
                }
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudYellow, "No macro set");
            }

            if (Service.Configuration.AutoMode)
            {
                if (Service.Configuration.CraftingX)
                ImGui.Text($"Remaining Crafts: {Service.Configuration.CraftX}");

                if (Service.Configuration.IRM.TryGetValue((uint)Handler.RecipeID, out var prevMacro))
                {
                    Macro? macro = Service.Configuration.UserMacros.First(x => x.ID == prevMacro);
                    if (macro != null)
                    {

                        string duration = string.Format("{0:D2}h {1:D2}m {2:D2}s", MacroTime.Hours, MacroTime.Minutes, MacroTime.Seconds);

                        ImGui.Text($"Approximate Remaining Duration: {duration}");
                    }
                }
            }

            if (!Service.Configuration.AutoMode)
            {
                ImGui.Text("Semi-Manual Mode");

                if (ImGui.Button("Execute recommended action"))
                {
                    Hotbars.ExecuteRecommended(CurrentRecommendation);
                }
                if (ImGui.Button("Fetch Recommendation"))
                {
                    FetchRecommendation(CurrentStep);
                }
            }
        }
    }
}
