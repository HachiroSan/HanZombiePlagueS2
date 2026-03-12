using System.ComponentModel.DataAnnotations;

namespace HanZombiePlayerData.Provider;

public sealed class HanZombiePlayerDataConfig
{
    [Required]
    [RegularExpression("^[A-Za-z0-9_.-]+$")]
    public string ConnectionKey { get; set; } = "zombies";

    public bool BootstrapSchema { get; set; } = true;
}