namespace WebApp.Models
{
    public class RegisterModel
    {
        public string Password { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public bool IsAdmin { get; set; }  // Admin olup olmayacağı bilgisini içerir
    }
}
