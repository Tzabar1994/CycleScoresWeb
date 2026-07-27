using System.ComponentModel.DataAnnotations.Schema;

namespace CycleScoresWeb.Models
{
    [NotMapped]
    public record Communique
    {
        public required string Event { get; set; }
        public string? HeaderImage { get; set; }
        public required string Title { get; set; }
        public string? SubTitle { get; set; }
        public string? CommuniqueNumber { get; set; }
        public Guid? CommuniqueId { get; set; }
        public required CommuniqueType CommuniqueType { get; set; }
        public List<Heat>? Start { get; set; }
        public List<RaceResult>? Result { get; set; }
        public List<Schedule>? Schedule { get; set; }
        public string? Decision { get; set; }
        public string? HeaderText { get; set; }
        public string[]? BodyText { get; set; }
        public bool? LandScape { get; set; }
        public bool? Minimal { get; set; }
    }
    public enum CommuniqueType
    {
        START = 0,
        RESULT = 1,
        SCHEDULE = 2,
        TEXT = 3
    }
    [NotMapped]
    public record Heat
    {
        public string? HeatTitle { get; set; }
        public required List<Rider> Riders { get; set; }
    }
    [NotMapped]
    public record Rider
    {
        public int? Bib { get; set; }
        public required string Name { get; set; }
        public string? Nation { get; set; }
    }
    [NotMapped]

    public record RaceResult
    {
        public string? HeatTitle { get; set; }
        public string? ResultTitle { get; set; }
        public required List<RiderResult> RiderResults { get; set; }
    }

    [NotMapped]
    public record RiderResult : Rider
    {
        public required string Rank { get; set; }
        public string? ResultDetails { get; set; }
    }

    [NotMapped]
    public record Schedule
    {
        public string? StartTime { get; set; }
        public string? Duration { get; set; }
        public string? Group { get; set; }
        public string? Event { get; set; }
        public string? Phase { get; set; }
    }
}
