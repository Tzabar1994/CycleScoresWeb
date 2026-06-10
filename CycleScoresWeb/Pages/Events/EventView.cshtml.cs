using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CycleScoresWeb.Data;
using CycleScoresWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace CycleScoresWeb.Events
{
    public class EventViewModel : PageModel
    {
        private readonly CycleScoresWeb.Data.CycleScoresWebContext _context;

        public EventViewModel(CycleScoresWeb.Data.CycleScoresWebContext context)
        {
            _context = context;
        }

        public CycleEvent CycleEvent { get; set; } = default!;
        public string DateRange { get; set; }
        public string SortBy { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, string? sortBy)
        {
            if (id == null)
            {
                return NotFound();
            }

            switch(sortBy?.ToUpper())
            {
                case "GROUP":
                    sortBy = "GROUP";
                    break;
                case "EVENT":
                    sortBy = "EVENT";
                    break;
                default:
                    sortBy = "DATE";
                    break;
            }
            sortBy = sortBy ?? "DATE";

            var cycleevent = await _context.Events
                .Include(x => x.EventRaces).ThenInclude(race => race.AdditionalCommuniques)
                .Include(x => x.EventCommuniques)
                //.Include(x => x.EventRaces)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cycleevent is not null)
            {
                CycleEvent = cycleevent;
                DateRange = cycleevent.StartDate.ToString() + " - " + cycleevent.EndDate.ToString();
                SortBy = sortBy;

                return Page();
            }

            return NotFound();
        }
    }
}
