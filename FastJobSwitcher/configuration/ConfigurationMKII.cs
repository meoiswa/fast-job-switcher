using System;

namespace FastJobSwitcher;

[Serializable]
public class ConfigurationMKII : ConfigurationBase
{
    public override int Version { get; set; } = 1;

    public bool IsVisible { get; set; } = true;

    public bool RegisterClassJobs { get; set; } = true;

    public bool RegisterPhantomJobs { get; set; } = true;

    public static ConfigurationMKII MigrateFrom(ConfigurationMKI oldConfig)
    {
        if (oldConfig == null)
        {
            return new ConfigurationMKII();
        }

        return new ConfigurationMKII
        {
            IsVisible = oldConfig.IsVisible,
            RegisterClassJobs = oldConfig.RegisterLowercaseCommands || oldConfig.RegisterUppercaseCommands,
        };
    }
}
