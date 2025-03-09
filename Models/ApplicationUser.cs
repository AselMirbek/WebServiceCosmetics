using Microsoft.AspNetCore.Identity;

namespace WebServiceCosmetics.Models
{
    public class CustomUser : IdentityUser
    {
        // Дополнительные свойства пользователя (например, полное имя)
        public string FullName { get; set; }
        public string Role { get; set; }  // Роль пользователя (Админ, Менеджер, Руководитель)
    }
}
