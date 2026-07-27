using CycleScoresWeb.Models;
using CycleScoresWeb.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace CycleScoresWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommuniqueController : ControllerBase
    {
        private readonly CycleScoresWeb.Data.CycleScoresWebContext _context;
        private ICommuniqueService _service;
        private IPDFGeneratorService _pdfService;

        public CommuniqueController(CycleScoresWeb.Data.CycleScoresWebContext context, ICommuniqueService service, IPDFGeneratorService pdfService)
        {
            _context = context;
            _service = service;
            _pdfService = pdfService;
        }

        [HttpGet("/generateBook")]
        public async Task<ActionResult> Test(int eventId, int key)
        {
            if (key != 1824)
            {
                return Forbid();
            }

            var cycleevent = await _context.Events
                .Include(x => x.EventRaces).ThenInclude(race => race.AdditionalCommuniques)
                .Include(x => x.EventCommuniques)
                //.Include(x => x.EventRaces)
                .FirstOrDefaultAsync(m => m.Id == eventId);

            var communiqueIds = new List<Guid>();

            foreach (var ac in cycleevent.EventCommuniques)
            {
                communiqueIds.Add(ac.CommuniqueID);
            }

            foreach (var race in cycleevent.EventRaces)
            {
                if (race.StartCommuniqueID != null)
                {
                    communiqueIds.Add((Guid)race.StartCommuniqueID); 
                }

                if (race.ResultCommuniqueID != null)
                {
                    communiqueIds.Add((Guid)race.ResultCommuniqueID);
                }

                if (race.AdditionalCommuniques != null)
                {
                    foreach (var ac in race.AdditionalCommuniques)
                    {
                        communiqueIds.Add(ac.CommuniqueID);
                    }
                }    
            }

            var communiques = new List<Communique>();

            foreach (var cID in communiqueIds)
            {
                try
                {
                    var c = await _service.FetchCommunique(cID);
                    communiques.Add(c);
                }
                catch
                {
                   //pass
                }
            }

            communiques = communiques.OrderBy(z => z.CommuniqueNumber ?? "").ToList();

            //var temp = new List<TempObject>();

            //foreach (var c in communiques)
            //{
            //    temp.Add(new TempObject
            //    {
            //        ID = c.CommuniqueId,
            //        Title = c.Title,
            //        SubTitle = c.SubTitle,
            //        CommuniqueNumber = c.CommuniqueNumber
            //    });

            //}

            //return new ObjectResult(temp);
            
            var doc = _pdfService.GenerateResultsBook(communiques);

            return File(doc, "application/pdf");
            //return Ok("Test");
        }

        private record TempObject
        {
            public Guid? ID { get; set; }
            public string? Title { get; set; }
            public string? SubTitle { get; set; }
            public string? CommuniqueNumber { get; set; }
        }

    }
}
