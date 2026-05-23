using System.ComponentModel.DataAnnotations;

namespace CycleScoresWeb.Models
{
    public class CycleEvent
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        [DataType(DataType.Date)]
        public required DateOnly StartDate { get; set; }
        [DataType(DataType.Date)]
        public required DateOnly EndDate { get; set; }
        public required string Location { get; set; }

        public List<Race>? EventRaces { get; set; }
        public List<CommuniqueSet>? EventCommuniques { get; set; }
    }

    public record CommuniqueSet
    {
        public required int Id { get; set; }
        public required string CommuniqueTitle { get; set;  }
        public required Guid CommuniqueID { get; set; }
    }
}
