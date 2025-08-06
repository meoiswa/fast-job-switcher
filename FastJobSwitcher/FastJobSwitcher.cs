using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace FastJobSwitcher;

public class FastJobSwitcher : IDisposable
{
    private readonly ConfigurationMKII configuration;
    private List<ClassJob>? classJobSheet;
    private List<MKDSupportJob>? phantomJobSheet;
    private HashSet<string> registeredCommands = new();
    public static readonly Dictionary<string, string> PhantomJobNameAcronymMap = new()
    {
        { "Phantom Freelancer", "PFRE" },
        { "Phantom Knight", "PKNT" },
        { "Phantom Berserker", "PBER" },
        { "Phantom Monk", "PMNK" },
        { "Phantom Ranger", "PRNG" },
        { "Phantom Samurai", "PSAM" },
        { "Phantom Bard", "PBRD" },
        { "Phantom Geomancer", "PGEO" },
        { "Phantom Time Mage", "PTIM" },
        { "Phantom Cannoneer", "PCAN" },
        { "Phantom Chemist", "PCHM" },
        { "Phantom Oracle", "PORC" },
        { "Phantom Thief", "PTHF" },
    };

    public FastJobSwitcher(ConfigurationMKII configuration)
    {
        this.configuration = configuration;
        classJobSheet = Service.Data.Excel.GetSheet<ClassJob>()?.ToList();
        if (classJobSheet == null)
        {
            Service.PluginLog.Warning("Failed to load ClassJob sheet.");
        }

        phantomJobSheet = Service.Data.Excel.GetSheet<MKDSupportJob>()?.ToList();
        if (phantomJobSheet == null)
        {
            Service.PluginLog.Warning("Failed to load MKDSupportJob sheet.");
        }
        else
        {
            // print to log every row in phantomJobSheet
            foreach (var row in phantomJobSheet)
            {
                var strings = row.GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(ReadOnlySeString))
                    .Select(p => (p.GetValue(row) as ReadOnlySeString?)?.ToString() ?? string.Empty)
                    .ToArray();
                Service.PluginLog.Information($"Phantom Job: {string.Join(", ", strings)}");
            }
        }

        Register();
    }

    public void Dispose()
    {
        UnRegister();
    }

    public void Register()
    {
        if (configuration.RegisterClassJobs)
        {
            classJobSheet?.ToList().ForEach(row =>
            {
                var acronym = row.Abbreviation.ToString();
                var name = row.Name.ToString();
                var rId = row.RowId;
                if (!string.IsNullOrWhiteSpace(acronym) && !string.IsNullOrWhiteSpace(name) && rId != 0)
                {
                    var command = "/" + acronym;
                    RegisterCommand(command.ToUpperInvariant(), name, "Class/Job");
                    RegisterCommand(command.ToLowerInvariant(), name, "Class/Job");
                }
            });
        }

        if (configuration.RegisterPhantomJobs)
        {
            phantomJobSheet?.ForEach(row =>
            {
                var jobName = row.Unknown0.ExtractText();
                var acronym = PhantomJobNameToAcronym(jobName);
                if (!string.IsNullOrWhiteSpace(acronym))
                {
                    var command = "/" + acronym;
                    RegisterCommand(command.ToUpperInvariant(), jobName, "Phantom Job");
                    RegisterCommand(command.ToLowerInvariant(), jobName, "Phantom Job");
                }
            });
        }
    }

    public void UnRegister()
    {
        registeredCommands.ToList().ForEach(command =>
        {
            if (Service.Commands.Commands.ContainsKey(command))
            {
                Service.Commands.RemoveHandler(command);
            }
        });
    }

    protected void OnCommand(string command, string arguments)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }
        else if (command.StartsWith("/"))
        {
            command = command.Substring(1);
        }

        if (command.Length == 3 && classJobSheet != null)
        {
            HandleClassJobCommand(command);
        }

        if (command.Length == 4 && phantomJobSheet != null)
        {
            HandlePhantomJobCommand(command);
        }
    }

    private void RegisterCommand(string command, string name, string type)
    {
        if (Service.Commands.Commands.ContainsKey(command))
        {
            Service.PluginLog.Warning($"Command already exists: {command}");
        }
        else
        {
            registeredCommands.Add(command);
            Service.Commands.AddHandler(command, new CommandInfo(OnCommand)
            {
                HelpMessage = $"Switches to {name} {type}",
                ShowInHelp = false,
            });
            Service.PluginLog.Information($"Registered command: {command} for {name} {type}");
        }
    }

    private unsafe void HandleClassJobCommand(string command)
    {
        var cj = classJobSheet!.FirstOrDefault(row => row.Abbreviation.ToString().Equals(command, StringComparison.InvariantCultureIgnoreCase));

        if (cj.Equals(default(ClassJob)))
        {
            var msg = $"JobSwitch: No class job found for command: {command}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
        }

        if (TryEquipBestGearsetForClassJob(cj))
        {
            Service.PluginLog.Information($"JobSwitch: Equipped best gearset for class job: {cj.Name}");
        }
        else
        {
            var msg = $"JobSwitch: No gearset found for class job: {cj.Name}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
        }
    }

    private string PhantomJobNameToAcronym(string name)
    {
        return PhantomJobNameAcronymMap.TryGetValue(name, out var acronym) ? acronym : string.Empty;
    }

    private string PhantomJobAcronymToName(string acronym)
    {
        foreach (var kvp in PhantomJobNameAcronymMap)
        {
            if (string.Equals(kvp.Value, acronym, StringComparison.InvariantCultureIgnoreCase))
                return kvp.Key;
        }
        return string.Empty;
    }

    private unsafe void HandlePhantomJobCommand(string command)
    {
        if (command.StartsWith("p", StringComparison.InvariantCultureIgnoreCase))
        {
            var jobName = PhantomJobAcronymToName(command);

            if (string.IsNullOrWhiteSpace(jobName))
            {
                var msg = $"JobSwitch: No Phantom Job found for command: {command}";
                Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            var row = phantomJobSheet!.FirstOrDefault(row => row.Unknown0.ExtractText().Equals(jobName, StringComparison.InvariantCultureIgnoreCase));

            if (row.Equals(default(MKDSupportJob)))
            {
                var msg = $"JobSwitch: No Phantom Job found for command: {command}";
                Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            if (GameMain.Instance()->CurrentTerritoryIntendedUseId != 61)
            {
                var msg = "You can only use this command in the Occult Crescent";
                Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            var jobId = row.RowId;

            var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.MKDSupportJobList);

            if (agent == null)
            {
                var msg = "Failed to get MKDSupportJobList agent.";
                Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            var eventObject = stackalloc AtkValue[1];
            var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
            atkValues[0].Type = ValueType.UInt;
            atkValues[0].UInt = 0;
            atkValues[1].Type = ValueType.UInt;
            atkValues[1].UInt = jobId;

            try
            {
                agent->ReceiveEvent(eventObject, atkValues, 2, 1);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Failed to switch Phantom Job: {ex.Message}");
                Service.ChatGui.PrintError($"Failed to switch Phantom Job: {ex.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
        }
    }

    private unsafe bool TryEquipBestGearsetForClassJob(ClassJob cj)
    {
        var rapture = RaptureGearsetModule.Instance();
        if (rapture != null)
        {
            short bestLevel = 0;
            byte? bestId = null;
            for (var i = 0; i < 100; i++)
            {
                var gearset = rapture->GetGearset(i);
                if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) && gearset->Id == i && gearset->ClassJob == cj.RowId)
                {
                    if (gearset->ItemLevel > bestLevel)
                    {
                        bestLevel = gearset->ItemLevel;
                        bestId = gearset->Id;
                    }
                }
            }
            if (bestId.HasValue)
            {
                rapture->EquipGearset(bestId.Value);
                return true;
            }
        }

        return false;
    }
}
