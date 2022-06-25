using Microsoft.AspNetCore.Identity;

namespace WebAPI.Models
{
    public class User:IdentityUser<int>
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Registered_at { get; set; }

        
    }
}
