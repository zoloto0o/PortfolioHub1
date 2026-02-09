namespace PortfolioHub.Models;

public class PortfolioMedia
{
    public int Id { get; set; }

    public int PortfolioItemId { get; set; }
    public PortfolioItem PortfolioItem { get; set; } = default!;

    public int MediaFileId { get; set; }
    public MediaFile MediaFile { get; set; } = default!;

    public int SortOrder { get; set; } = 0;
}
