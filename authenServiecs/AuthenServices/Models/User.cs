namespace AuthenController.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Pwd { get; set; }
    }
}