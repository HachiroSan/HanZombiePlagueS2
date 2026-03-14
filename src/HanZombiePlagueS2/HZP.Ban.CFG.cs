using System.ComponentModel.DataAnnotations;

namespace HanZombiePlagueS2;

public sealed class HZPBanCFG
{
    public bool Enable { get; set; } = true;

    [Required]
    [RegularExpression("^[A-Za-z0-9_.:-]+$")]
    public string ServerScope { get; set; } = "default";

    [Range(0.0, 5.0)]
    public float ConnectCheckDelaySeconds { get; set; } = 0.25f;
}
