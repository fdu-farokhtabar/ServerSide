namespace Domain.Entity
{
    public class Tutorial
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Description { get; set; }
        public string[] VideoUrls { get; set; }
        public string[] ImageUrls { get; set; }
        public string[] Roles { get; set; }
    }
}
