using Microsoft.AspNetCore.Identity;

namespace WebServiceCosmetics.Models
{
    public class User: IdentityUser
    {
        public string FullName { get; set; }
    }
}
