using System.Text.Json.Serialization;

namespace WebAPI.Models
{
    public class RegistrationModel
    {       
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        [JsonIgnore]
        public DateTime RegisterDate { get; set; }
        [JsonIgnore]
        public DateTime LastLoginDate { get; set; }
    }
}
