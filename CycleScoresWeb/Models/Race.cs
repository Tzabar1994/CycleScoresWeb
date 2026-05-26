namespace CycleScoresWeb.Models
{
    public class Race
    {
        public required int Id { get; set; }
        public required string Group { get; set; }
        public required string Name { get; set; }
        public string? Phase { get; set; }
        public required DateOnly RaceDate { get; set; }
        public required int SortOrder { get; set; }
        public Guid? StartCommuniqueID { get; set;  }

        public Guid? ResultCommuniqueID { get; set; }
    }
}
