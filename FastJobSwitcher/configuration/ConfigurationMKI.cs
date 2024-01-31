using System;

namespace FastJobSwitcher
{
  [Serializable]
  public class ConfigurationMKI : ConfigurationBase
  {
    public override int Version { get; set; } = 0;

    public bool IsVisible { get; set; } = true;

    public string Prefix { get; set; } = "";

    public string Suffix { get; set; } = "";

    public bool RegisterLowercaseCommands { get; set; } = true;

    public bool RegisterUppercaseCommands { get; set; } = true;
  }
}
