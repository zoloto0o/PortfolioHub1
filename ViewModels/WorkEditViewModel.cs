using System.ComponentModel.DataAnnotations;
using PortfolioHub.Models;

namespace PortfolioHub.ViewModels;

public class WorkEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Title { get; set; } = "";

    [MaxLength(3000)]
    public string Description { get; set; } = "";

    public Visibility Visibility { get; set; } = Visibility.Public;

    public bool IsPinned { get; set; } = false;
}
