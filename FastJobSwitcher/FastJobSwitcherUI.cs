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
            if (ImGui.Checkbox("Classes and Jobs##ClassJobs", ref classJobEnabled))
            {
                configuration.RegisterClassJobs = classJobEnabled;
                configuration.Save();
            }

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
                if (ImGui.BeginTable("PhantomJobsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX))
                {
                    ImGui.TableSetupColumn("Command");
                    ImGui.TableSetupColumn("Job Name");
                    ImGui.TableHeadersRow();
                    foreach (var entry in FastJobSwitcher.PhantomJobNameAcronymMap)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.TextUnformatted($"/{entry.Value}");
                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextUnformatted(entry.Key);
                    }
                    ImGui.EndTable();
                }
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
