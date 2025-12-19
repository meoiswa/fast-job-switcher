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
            RegisterCommand("/pj", "Phantom Job (fuzzy search)", "Phantom Job");
            RegisterCommand("/PJ", "Phantom Job (fuzzy search)", "Phantom Job");
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

        if (command.Equals("pj", StringComparison.InvariantCultureIgnoreCase) && phantomJobSheet != null)
        {
            HandlePhantomJobCommand(arguments);
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

    private void HandleClassJobCommand(string command)
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

    private static int CalculateCharacterMatchScore(string query, string target)
    {
        // Count how many characters from the query appear in the target (in order)
        int matchCount = 0;
        int targetIndex = 0;
        
        foreach (char queryChar in query)
        {
            while (targetIndex < target.Length)
            {
                if (target[targetIndex] == queryChar)
                {
                    matchCount++;
                    targetIndex++;
                    break;
                }
                targetIndex++;
            }
            
            if (targetIndex >= target.Length)
                break;
        }
        
        return matchCount;
    }

    private MKDSupportJob? FuzzySearchPhantomJob(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || phantomJobSheet == null)
            return null;

        var searchQuery = query.ToLowerInvariant();
        MKDSupportJob? bestSubstringMatch = null;
        int bestSubstringScore = int.MaxValue;
        MKDSupportJob? bestCharMatch = null;
        int bestCharMatchScore = 0;

        foreach (var job in phantomJobSheet)
        {
            var name = job.Name.ExtractText().ToLowerInvariant();
            var nameEnglish = job.NameEnglish.ExtractText().ToLowerInvariant();
            
            // Remove "phantom " prefix from nameEnglish to avoid matching against it
            if (nameEnglish.StartsWith("phantom "))
            {
                nameEnglish = nameEnglish.Substring(8);
            }

            // Check for exact substring match first (best case)
            if (name.Contains(searchQuery) || nameEnglish.Contains(searchQuery))
            {
                var score = Math.Min(
                    name.Contains(searchQuery) ? name.IndexOf(searchQuery) : int.MaxValue,
                    nameEnglish.Contains(searchQuery) ? nameEnglish.IndexOf(searchQuery) : int.MaxValue
                );
                
                if (score < bestSubstringScore)
                {
                    bestSubstringScore = score;
                    bestSubstringMatch = job;
                }
            }
            else
            {
                // Character-based matching: count matching characters in sequence
                var charMatchName = CalculateCharacterMatchScore(searchQuery, name);
                var charMatchEnglish = CalculateCharacterMatchScore(searchQuery, nameEnglish);
                var charMatch = Math.Max(charMatchName, charMatchEnglish);

                if (charMatch > bestCharMatchScore)
                {
                    bestCharMatchScore = charMatch;
                    bestCharMatch = job;
                }
            }
        }

        // Always prefer substring matches over character matches
        return bestSubstringMatch ?? bestCharMatch;
    }

    private unsafe void HandlePhantomJobCommand(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            var msg = "JobSwitch: Please provide a search query for the Phantom Job (e.g., /pj knight)";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }

        var row = FuzzySearchPhantomJob(searchQuery);

        if (!row.HasValue)
        {
            var msg = $"JobSwitch: No Phantom Job found matching: {searchQuery}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }

        var job = row.Value;

        if (GameMain.Instance()->CurrentTerritoryIntendedUseId != FFXIVClientStructs.FFXIV.Client.Enums.TerritoryIntendedUse.OccultCrescent)
        {
            var msg = "You can only use this command while in the Occult Crescent";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }

        var jobId = job.RowId;
        var jobName = job.NameEnglish.ExtractText();
        Service.PluginLog.Information($"Switching to Phantom Job: {jobName} (matched from query: {searchQuery})");

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
