namespace NeoPlusPlusApp;

public sealed class Challenge
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StarterCode { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;

    public override string ToString() => Title;
}
