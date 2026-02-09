using System.ComponentModel.DataAnnotations;

namespace PortfolioHub.ViewModels;

public class LoginViewModel
{
    [Required]
    public string Login { get; set; } = ""; // username или email

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}
