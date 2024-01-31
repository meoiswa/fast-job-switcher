using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace FastJobSwitcher
{
  public class Service
  {
#pragma warning disable CS8618
    [PluginService] public static IDataManager Data { get; private set; }
    [PluginService] public static IChatGui ChatGui { get; private set; }
    [PluginService] public static ICommandManager Commands { get; private set; }
    [PluginService] public static IPluginLog PluginLog { get; private set; }
#pragma warning restore CS8618
  }
}
