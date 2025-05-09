namespace MarketplaceAPI.Models
{
    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TagWithCountDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductCount { get; set; }
    }
}
