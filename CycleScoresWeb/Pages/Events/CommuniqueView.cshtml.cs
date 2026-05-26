using CycleScoresWeb.Services;
using CycleScoresWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Markdig;

namespace CycleScoresWeb.Pages.Events
{
    public class CommuniqueViewModel : PageModel
    {
        private ICommuniqueService _service;
        private IPDFGeneratorService _pdfService;

        public CommuniqueViewModel(ICommuniqueService service)
        { 
            _service = service;
        }

        public Communique Communique { get; set; }
        public int EventId { get; set;  }
        public Guid CommuniqueId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid communiqueId, int eventId)
        {
            try
            {
                EventId = eventId;
                CommuniqueId = communiqueId;
                Communique = await _service.FetchCommunique(communiqueId);
                //for (var text in Communique?.BodyText)
                //{
                //    text = Markdown.ToHtml(text);
                //}
                return Page();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
