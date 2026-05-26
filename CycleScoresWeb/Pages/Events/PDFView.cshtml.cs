using CycleScoresWeb.Models;
using CycleScoresWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CycleScoresWeb.Pages.Events
{
    public class PDFViewModel : PageModel
    {
        private ICommuniqueService _service;
        private IPDFGeneratorService _pdfService;

        public PDFViewModel(ICommuniqueService service, IPDFGeneratorService pdfService)
        {
            _service = service;
            _pdfService = pdfService;
        }

        public async Task<FileResult> OnGetAsync(Guid communiqueId)
        {
            var communique = await _service.FetchCommunique(communiqueId);
            return File(_pdfService.GenerateCommunique(communique), "application/pdf", $"{communiqueId}.pdf");
        }
    }
}
