namespace WebApp.Models
{
    public class Vote
    {

        public int Id { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public int IdeaId { get; set; }
        public virtual Idea Idea { get; set; }
    }
}
