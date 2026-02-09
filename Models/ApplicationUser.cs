using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PortfolioHub.Models;

public class ApplicationUser : IdentityUser
{
    public Profile? Profile { get; set; }
}
