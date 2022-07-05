using System.Text.Json.Serialization;

namespace WebAPI.Models
{
    public class AuthorizationModel
    {      
        public string UserName { get; set; }
        public string Password { get; set; }
        [JsonIgnore]
        public Guid SessionID { get; set; }

    }
}
