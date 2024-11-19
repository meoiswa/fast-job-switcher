using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastJobSwitcher;

public class FastJobSwitcher : IDisposable
{
    private readonly ConfigurationMKI configuration;
    private ExcelSheet<ClassJob>? classJobSheet;
    private HashSet<string> registeredCommands = [];

    public FastJobSwitcher(ConfigurationMKI configuration)
    {
        this.configuration = configuration;
        classJobSheet = Service.Data.Excel.GetSheet<ClassJob>();
        if (classJobSheet == null)
        {
            Service.PluginLog.Warning("Failed to load ClassJob sheet.");
            return;
        }

        Register();
    }

    public void Dispose()
    {
        UnRegister();
    }

    public void Register()
    {
        if (classJobSheet == null)
        {
            return;
        }

        classJobSheet.ToList().ForEach(row =>
        {
            var acronym = row.Abbreviation.ToString();
            var name = row.Name.ToString();
            var rId = row.RowId;
            if (!string.IsNullOrWhiteSpace(acronym) && !string.IsNullOrWhiteSpace(name) && rId != 0)
            {
                var lower = ("/" + configuration.Prefix + acronym + configuration.Suffix).ToLowerInvariant();
                var upper = ("/" + configuration.Prefix + acronym + configuration.Suffix).ToUpperInvariant();
                if (configuration.RegisterUppercaseCommands)
                {
                    if (Service.Commands.Commands.ContainsKey(upper))
                    {
                        Service.PluginLog.Warning($"Command already exists: {upper}");
                    }
                    else
                    {
                        registeredCommands.Add(upper);
                        Service.Commands.AddHandler(upper, new CommandInfo(OnCommand)
                        {
                            HelpMessage = $"Switches to {name} class/job.",
                            ShowInHelp = false,
                        });
                    }
                }
                if (configuration.RegisterLowercaseCommands)
                {
                    if (Service.Commands.Commands.ContainsKey(lower))
                    {
                        Service.PluginLog.Warning($"Command already exists: {lower}");
                    }
                    else
                    {
                        registeredCommands.Add(lower);
                        Service.Commands.AddHandler(lower, new CommandInfo(OnCommand)
                        {
                            HelpMessage = $"Switches to {name} class/job.",
                            ShowInHelp = false,
                        });
                    }
                }
            }
        });
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

        if (command.StartsWith(configuration.Prefix))
        {
            command = command.Substring(configuration.Prefix.Length);
        }
        if (command.EndsWith(configuration.Suffix))
        {
            command = command.Substring(0, command.Length - configuration.Suffix.Length);
        }

        var cj = classJobSheet!.ToList().FirstOrDefault(row => row.Abbreviation.ToString().Equals(command, StringComparison.InvariantCultureIgnoreCase));

        if (cj.Equals(default(ClassJob)))
        {
            var msg = $"JobSwitch: No class job found for command: {command}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }

        var success = TryEquipBestGearsetForClassJob(cj);

        if (!success)
        {
            var msg = $"JobSwitch: No gearset found for class job: {cj.Name}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
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
