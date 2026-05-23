using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CycleScoresWeb.Models;
namespace CycleScoresWeb.Pages
{
    public class IndexModel : PageModel
    {

        private readonly CycleScoresWeb.Data.CycleScoresWebContext _context;

        public IndexModel(CycleScoresWeb.Data.CycleScoresWebContext context)
        {
            _context = context;
        }

        public IList<CycleEvent> CycleEvent { get; set; } = default!;

        public async Task OnGetAsync()
        {
            CycleEvent = await _context.Events.ToListAsync();
        }
        }
}
