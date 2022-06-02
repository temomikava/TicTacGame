namespace WebAPI.Models
{
    public class Login
    {
        public int Id { get; set; }
        public DateTime LoginDate { get; set; }
        public int UserID { get; set; }
        public int LogoutDate { get; set; }
    }
}
