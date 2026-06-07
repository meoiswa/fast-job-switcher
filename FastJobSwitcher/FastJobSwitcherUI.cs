using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace FastJobSwitcher;

public class FastJobSwitcherUI : Window, IDisposable
{
    private readonly ConfigurationMKII configuration;

    public FastJobSwitcherUI(ConfigurationMKII configuration)
      : base(
        "Fast Job Switcher##ConfigWindow",
        ImGuiWindowFlags.AlwaysAutoResize
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoCollapse
      )
    {
        this.configuration = configuration;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(268, 0),
            MaximumSize = new Vector2(268, 1000)
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        configuration.IsVisible = false;
        configuration.Save();
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Register:");
        ImGui.Indent();
        {
            var classJobEnabled = configuration.RegisterClassJobs;
            ImGui.BeginGroup();
            ImGui.PushID("ClassJobsRow");
            ImGui.Checkbox("Classes and Jobs##ClassJobs", ref classJobEnabled);
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 999f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
            if (ImGui.Button("?", new Vector2(22, 22))) { /* No action on click */ }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted("Use each class or job acronym as a command, for example:");
                ImGui.Spacing();
                if (ImGui.BeginTable("ClassJobExamples", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX))
                {
                    ImGui.TableSetupColumn("Command");
                    ImGui.TableSetupColumn("Matches");
                    ImGui.TableHeadersRow();

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pld");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Paladin");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/whm");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("White Mage");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/drk");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Dark Knight");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/fsh");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Fisher");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pgl");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Pugilist");

                    ImGui.EndTable();
                }
                ImGui.Spacing();
                ImGui.TextUnformatted("The plugin equips your best saved gearset for that class/job.");
                ImGui.EndTooltip();
            }
            ImGui.PopStyleVar(2);
            ImGui.PopID();
            if (classJobEnabled != configuration.RegisterClassJobs)
            {
                configuration.RegisterClassJobs = classJobEnabled;
                configuration.Save();
            }
            ImGui.EndGroup();

            ImGui.BeginGroup();
            var phantomJobsEnabled = configuration.RegisterPhantomJobs;
            ImGui.PushID("PhantomJobsRow");
            ImGui.Checkbox("Phantom Jobs##PhantomJobs", ref phantomJobsEnabled);
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 999f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
            if (ImGui.Button("?", new Vector2(22, 22))) { /* No action on click */ }
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted("Use the /pj command with fuzzy search, for example:");
                ImGui.Spacing();
                if (ImGui.BeginTable("PhantomJobExamples", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX))
                {
                    ImGui.TableSetupColumn("Command");
                    ImGui.TableSetupColumn("Matches");
                    ImGui.TableHeadersRow();
                    
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pj knight");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Phantom Knight");
                    
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pj rng");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Phantom Ranger");
                    
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pj mage");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Phantom Time Mage");
                    
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pj cnr");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Phantom Cannoneer");
                    
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("/pj dnc");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Phantom Dancer");
                    
                    ImGui.EndTable();
                }
                ImGui.Spacing();
                ImGui.TextUnformatted("Supports partial matches and typos!");
                ImGui.EndTooltip();
            }
            ImGui.PopStyleVar(2);
            ImGui.PopID();
            if (phantomJobsEnabled != configuration.RegisterPhantomJobs)
            {
                configuration.RegisterPhantomJobs = phantomJobsEnabled;
                configuration.Save();
            }
            ImGui.EndGroup();
        }
        ImGui.Unindent();
    }
}
