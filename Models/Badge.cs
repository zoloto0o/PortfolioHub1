using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.Models;

public class Badge
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Key { get; set; } = default!;

    [Required, MaxLength(80)]
    public string Name { get; set; } = default!;

    [MaxLength(240)]
    public string Description { get; set; } = "";
}
