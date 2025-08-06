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

            var phantomJobsEnabled = configuration.RegisterPhantomJobs;
            if (ImGui.Checkbox("Phantom Jobs##PhantomJobs", ref phantomJobsEnabled))
            {
                configuration.RegisterPhantomJobs = phantomJobsEnabled;
                configuration.Save();
            }
        }
        ImGui.Unindent();
    }
}
