using System.ComponentModel.DataAnnotations;

namespace HanZombiePlagueS2;

public sealed class HZPDatabaseConfig
{
    [Required]
    [RegularExpression("^[A-Za-z0-9_.-]+$")]
    public string ConnectionKey { get; set; } = "zombies";

    public bool BootstrapSchema { get; set; } = true;
}
