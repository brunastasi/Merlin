namespace Merlin.Models.ApiData
{
    public class Article
    {
        public Source Source { get; set; }            // Nom de la source
        public string Title { get; set; }            // Titre de l'article
        public string Description { get; set; }      // Description de l'article
        public DateTime PublishedAt { get; set; }    // Date de publication
    }

    public class Source
    {
        public string Id { get; set; }              // Id de la source (peut être null)
        public string Name { get; set; }            // Nom de la source (ex. : Bloomberg)
    }
}
