using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CycleScoresWeb.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Markdown;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Net.Mail;
using System;
using Microsoft.AspNetCore.OutputCaching;

namespace CycleScoresWeb.Services
{
    
    public interface IPDFGeneratorService
    {
        public byte[] GenerateCommunique(Communique c);
    }

    public class PDFGeneratorService : IPDFGeneratorService
    {
        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainerClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConcurrentDictionary<string, byte[]?> _flagCache = new(StringComparer.OrdinalIgnoreCase);

        // Maps three-letter nation codes to ISO 3166-1 alpha-2 (lowercase), as used by flagcdn.com.
        // Keyed by BOTH IOC and ISO 3166-1 alpha-3 codes: where they coincide there is a single
        // entry; where they differ, both are present (e.g. DEN and DNK both map to "dk"). The rare
        // IOC/ISO3 collisions (e.g. BRN = Bahrain in IOC but Brunei in ISO3) resolve to IOC.
        private static readonly Dictionary<string, string> CodeToIso2 = new(StringComparer.OrdinalIgnoreCase)
        {
            { "AFG", "af" },
            { "ALB", "al" },
            { "ALG", "dz" }, { "DZA", "dz" },
            { "AND", "ad" },
            { "ANG", "ao" }, { "AGO", "ao" },
            { "ANT", "ag" }, { "ATG", "ag" },
            { "ARG", "ar" },
            { "ARM", "am" },
            { "AUS", "au" },
            { "AUT", "at" },
            { "AZE", "az" },
            { "BAH", "bs" }, { "BHS", "bs" },
            { "BRN", "bh" }, { "BHR", "bh" },
            { "BAN", "bd" }, { "BGD", "bd" },
            { "BAR", "bb" }, { "BRB", "bb" },
            { "BLR", "by" },
            { "BEL", "be" },
            { "BIZ", "bz" }, { "BLZ", "bz" },
            { "BEN", "bj" },
            { "BER", "bm" }, { "BMU", "bm" },
            { "BOL", "bo" },
            { "BIH", "ba" },
            { "BOT", "bw" }, { "BWA", "bw" },
            { "BRA", "br" },
            { "BRU", "bn" },
            { "BUL", "bg" }, { "BGR", "bg" },
            { "BUR", "bf" }, { "BFA", "bf" },
            { "CAM", "kh" }, { "KHM", "kh" },
            { "CAN", "ca" },
            { "CHI", "cl" }, { "CHL", "cl" },
            { "CHN", "cn" },
            { "COL", "co" },
            { "CRC", "cr" }, { "CRI", "cr" },
            { "CRO", "hr" }, { "HRV", "hr" },
            { "CUB", "cu" },
            { "CYP", "cy" },
            { "CZE", "cz" },
            { "DEN", "dk" }, { "DNK", "dk" },
            { "ECU", "ec" },
            { "EGY", "eg" },
            { "ESA", "sv" }, { "SLV", "sv" },
            { "ERI", "er" },
            { "ESP", "es" },
            { "EST", "ee" },
            { "ETH", "et" },
            { "FIJ", "fj" }, { "FJI", "fj" },
            { "FIN", "fi" },
            { "FRA", "fr" },
            { "GEO", "ge" },
            { "GER", "de" }, { "DEU", "de" },
            { "GBR", "gb" },
            { "GRE", "gr" }, { "GRC", "gr" },
            { "GRN", "gd" }, { "GRD", "gd" },
            { "GUA", "gt" }, { "GTM", "gt" },
            { "GUY", "gy" },
            { "HAI", "ht" }, { "HTI", "ht" },
            { "HON", "hn" }, { "HND", "hn" },
            { "HKG", "hk" },
            { "HUN", "hu" },
            { "ISL", "is" },
            { "IND", "in" },
            { "INA", "id" }, { "IDN", "id" },
            { "IRI", "ir" }, { "IRN", "ir" },
            { "IRQ", "iq" },
            { "IRL", "ie" },
            { "ISR", "il" },
            { "ITA", "it" },
            { "JPN", "jp" },
            { "KAZ", "kz" },
            { "KEN", "ke" },
            { "KOR", "kr" },
            { "KSA", "sa" }, { "SAU", "sa" },
            { "KUW", "kw" }, { "KWT", "kw" },
            { "LAT", "lv" }, { "LVA", "lv" },
            { "LIB", "lb" }, { "LBN", "lb" },
            { "LBA", "ly" }, { "LBY", "ly" },
            { "LIE", "li" },
            { "LTU", "lt" },
            { "LUX", "lu" },
            { "MAD", "mg" }, { "MDG", "mg" },
            { "MAS", "my" }, { "MYS", "my" },
            { "MLT", "mt" },
            { "MRI", "mu" }, { "MUS", "mu" },
            { "MEX", "mx" },
            { "MDA", "md" },
            { "MON", "mc" }, { "MCO", "mc" },
            { "MGL", "mn" }, { "MNG", "mn" },
            { "MNE", "me" },
            { "MAR", "ma" },
            { "MOZ", "mz" },
            { "MYA", "mm" }, { "MMR", "mm" },
            { "NED", "nl" }, { "NLD", "nl" },
            { "NEP", "np" }, { "NPL", "np" },
            { "NZL", "nz" },
            { "NCA", "ni" }, { "NIC", "ni" },
            { "NIG", "ne" }, { "NER", "ne" },
            { "NGR", "ng" }, { "NGA", "ng" },
            { "MKD", "mk" },
            { "NOR", "no" },
            { "OMA", "om" }, { "OMN", "om" },
            { "PAK", "pk" },
            { "PAR", "py" }, { "PRY", "py" },
            { "PER", "pe" },
            { "PHI", "ph" }, { "PHL", "ph" },
            { "POL", "pl" },
            { "POR", "pt" }, { "PRT", "pt" },
            { "PUR", "pr" }, { "PRI", "pr" },
            { "QAT", "qa" },
            { "ROU", "ro" }, { "ROM", "ro" },
            { "RUS", "ru" },
            { "RWA", "rw" },
            { "SMR", "sm" },
            { "SEN", "sn" },
            { "SRB", "rs" },
            { "SEY", "sc" }, { "SYC", "sc" },
            { "SIN", "sg" }, { "SGP", "sg" },
            { "SVK", "sk" },
            { "SLO", "si" }, { "SVN", "si" },
            { "RSA", "za" }, { "ZAF", "za" },
            { "SRI", "lk" }, { "LKA", "lk" },
            { "SUD", "sd" }, { "SDN", "sd" },
            { "SUI", "ch" }, { "CHE", "ch" },
            { "SWE", "se" },
            { "TAN", "tz" }, { "TZA", "tz" },
            { "THA", "th" },
            { "TOG", "tg" }, { "TGO", "tg" },
            { "TGA", "to" }, { "TON", "to" },
            { "TRI", "tt" }, { "TTO", "tt" },
            { "TUN", "tn" },
            { "TUR", "tr" },
            { "UAE", "ae" }, { "ARE", "ae" },
            { "UKR", "ua" },
            { "URU", "uy" }, { "URY", "uy" },
            { "USA", "us" },
            { "UZB", "uz" },
            { "VEN", "ve" },
            { "VIE", "vn" }, { "VNM", "vn" },
            { "ZAM", "zm" }, { "ZMB", "zm" },
            { "ZIM", "zw" }, { "ZWE", "zw" },
        };

        public PDFGeneratorService(IHttpClientFactory httpClientFactory)
        {
            _blobServiceClient = new BlobServiceClient(
                new Uri("https://cyclescoresweb.blob.core.windows.net"),
                new DefaultAzureCredential());

            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("header-images");
            _httpClientFactory = httpClientFactory;
        }

        private byte[]? GetFlag(string? nation)
        {
            if (string.IsNullOrWhiteSpace(nation))
            {
                return null;
            }

            return _flagCache.GetOrAdd(nation, code =>
            {
                if (!CodeToIso2.TryGetValue(code, out var iso2))
                {
                    return null;
                }

                try
                {
                    var client = _httpClientFactory.CreateClient();
                    return client.GetByteArrayAsync($"https://flagcdn.com/w80/{iso2}.png").GetAwaiter().GetResult();
                }
                catch
                {
                    return null;
                }
            });
        }

        private async Task<byte[]> TryGetHeaderImage(string filename)
        {
            try 
            {
                var blobClient = _blobContainerClient.GetBlobClient(filename);
                var download = await blobClient.DownloadContentAsync();
                var bytes = download.Value.Content.ToArray();
                //var mime = filename.EndsWith(".png") ? "image/png" : "image/jpeg";
                return bytes;
            }
            catch (Azure.RequestFailedException ex)
            {
                return null;
            }
            
        }

        public byte[] GenerateCommunique(Communique c)
        {
            byte[]? image = null;

            if (c.HeaderImage != null)
            {
                image = TryGetHeaderImage(c.HeaderImage).Result;
            }

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Centimetre);
                    //page.MarginTop(0.5f, Unit.Centimetre);
                    //page.MarginBottom(0.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(13));

                    page.Header()
                    .Column(col =>
                    {
                        if (c.CommuniqueNumber != null)
                        {
                            col.Item().Text($"Communiqué {c.CommuniqueNumber}").Italic().AlignEnd();
                        }

                        if (image != null)
                        {
                            col.Item().Image(image).FitWidth();
                        }

                        //col.Item().Image("C:\\Users\\danie\\source\\repos\\CommuniqueGenerator\\CommuniqueGenerator\\Images\\header.png").FitWidth();

                        col.Item().PaddingTop(0.25f, Unit.Centimetre)
                            .Text(c.Title)
                            .Bold()
                            .FontSize(22)
                            .FontColor(Colors.Black)
                            .AlignCenter();

                        if (!c.SubTitle.IsWhiteSpace())
                        {
                            col.Item()
                            .PaddingVertical(5)
                            .Text(c.SubTitle)
                            .Bold()
                            .Italic()
                            .FontSize(16)
                            .FontColor(Colors.Black)
                            .AlignCenter();
                        }

                        col.Item()
                            .PaddingVertical(5)
                            .LineHorizontal(2)
                            .LineColor(Colors.Blue.Darken4);

                        if (c.HeaderText != null)
                        {
                            col.Item()
                            .PaddingTop(5)
                            .PaddingBottom(10)
                            .Border(2, Colors.Blue.Darken4)
                            .Padding(0.2f, Unit.Centimetre)
                            .Text(c.HeaderText).Bold()
                            .AlignCenter();
                        }
                    });




                    page.Content()
                        .Column(x =>
                        {
                            if (c.Start != null)
                            {
                                foreach (var s in c.Start)
                                {
                                    x.Item()
                                    .PreventPageBreak()
                                    .Column(y =>
                                    {
                                        if(s.HeatTitle != null && !s.HeatTitle.IsWhiteSpace())
                                        {
                                            y.Item()
                                            .Background(Colors.Blue.Lighten5)
                                            .PaddingVertical(0.1f, Unit.Centimetre)
                                            .PaddingLeft(0.2f, Unit.Centimetre)
                                            .Text(s.HeatTitle)
                                            .Bold();
                                        }
                                        
                                        y.Item().PaddingLeft(20)
                                            .Row(row =>
                                            {
                                                row.Spacing(5);
                                                row.ConstantItem(40).Text("#").Bold();
                                                row.ConstantItem(375).Text("Rider").Bold();
                                                row.ConstantItem(35).Text("NAT").Bold();
                                                row.ConstantItem(28).Text("").Bold();
                                            });

                                        foreach (var r in s.Riders)
                                        {
                                            y.Item().PaddingLeft(20)
                                            .Row(row =>
                                            {
                                                row.Spacing(5);
                                                row.ConstantItem(40).Text($"{r?.Bib.ToString() ?? ""}");
                                                row.ConstantItem(375).Text(r.Name);
                                                row.ConstantItem(35).Text(r.Nation ?? "");
                                                var flag = GetFlag(r.Nation);
                                                var flagCell = row.ConstantItem(28);
                                                if (flag != null) flagCell.Height(13).Image(flag).FitUnproportionally();
                                            });
                                        }
                                        y.Item().PaddingBottom(0.5f, Unit.Centimetre);
                                    }

                                    );


                                }
                            }

                            if (c.Result != null)
                            {
                                foreach (var r in c.Result)
                                {
                                    x.Item()
                                    .PreventPageBreak()
                                    .Column(y =>
                                    {
                                        if (r.HeatTitle != null)
                                        {
                                            y.Item()
                                                .Background(Colors.Blue.Lighten5)
                                                .PaddingVertical(0.1f, Unit.Centimetre)
                                                .PaddingLeft(0.2f, Unit.Centimetre)
                                                .Text(r.HeatTitle)
                                                .Bold();
                                        }

                                        y.Item().PaddingLeft(20)
                                                .Row(row =>
                                                {
                                                    row.ConstantItem(50).Text("Rank").Bold();
                                                    row.ConstantItem(50).Text("#").Bold();
                                                    row.ConstantItem(276).Text("Rider").Bold();
                                                    row.ConstantItem(35).Text("NAT").Bold();
                                                    row.ConstantItem(28).Text("").Bold();
                                                    row.ConstantItem(100).Text(r.ResultTitle == null ? "" : r.ResultTitle).Bold().AlignEnd();
                                                });

                                        y.Item()
                                            .PaddingLeft(0.2f, Unit.Centimetre)
                                            .LineHorizontal(0.5f)
                                            .LineColor(Colors.Blue.Darken4);

                                        foreach (var result in r.RiderResults)
                                        {
                                            y.Item().PaddingLeft(20)
                                                .Row(row =>
                                                {
                                                    row.ConstantItem(50).Text($"{result.Rank}");
                                                    row.ConstantItem(50).Text($"{result.Bib}");
                                                    row.ConstantItem(276).Text(result.Name);
                                                    row.ConstantItem(35).Text(result.Nation == null ? "" : result.Nation);
                                                    var flag = GetFlag(result.Nation);
                                                    var flagCell = row.ConstantItem(28);
                                                    if (flag != null) flagCell.Height(13).Image(flag).FitUnproportionally();
                                                    row.ConstantItem(100).Text(result.ResultDetails == null ? "" : result.ResultDetails).Bold().AlignEnd();
                                                });
                                        }
                                    });
                                }
                            }

                            if (c.Schedule != null)
                            {
                                x.Item().Column(y =>
                                {
                                    y.Item().PaddingBottom(10).Row(row =>
                                    {
                                        row.RelativeItem(1).Text("Start").Bold();
                                        row.RelativeItem(2).Text("Duration").Bold();
                                        row.RelativeItem(3).Text("Group").Bold();
                                        row.RelativeItem(3).Text("Event").Bold();
                                        row.RelativeItem(2).Text("").Bold();
                                    });

                                    foreach (var ev in c.Schedule)
                                    {
                                        y.Item().Row(row =>
                                        {
                                            //row.Spacing(5);
                                            row.RelativeItem(1).Text(ev.StartTime).Bold();
                                            row.RelativeItem(2).Text(ev.Duration);
                                            row.RelativeItem(3).Markdown(ev.Group ?? "");
                                            row.RelativeItem(3).Text(ev.Event);
                                            row.RelativeItem(2).Text(ev.Phase);
                                        });
                                    }
                                });

                            }

                            if (c.BodyText != null)
                            {
                                page.DefaultTextStyle(x => x.FontSize(10));

                                var count = c.BodyText.Length;

                                foreach (var text in c.BodyText)
                                {
                                    count--;

                                    x.Item().PreventPageBreak()
                                    .PaddingTop(0.2f, Unit.Centimetre)
                                    //.Border(2, Colors.Blue.Lighten4)
                                    .Padding(0.2f, Unit.Centimetre)
                                    //.AlignCenter()
                                    .Markdown(text);

                                    if (count > 0)
                                    {
                                        x.Item()
                                        .LineHorizontal(2)
                                        .LineColor(Colors.Blue.Lighten4);
                                    }
                                }
                            }

                            if (c.Decision != null && !c.Decision.IsWhiteSpace())
                            {
                                x.Item()
                                .PaddingTop(0.2f, Unit.Centimetre)
                                .Border(2, Colors.Blue.Darken4)
                                .Padding(0.2f, Unit.Centimetre)
                                //.Text(c.Decision)
                                //.AlignCenter()
                                .Markdown(c.Decision);

                            }

                        });

                    page.Footer()
                        .Column(outerCol =>
                        {
                            outerCol.Item()
                            .PaddingVertical(5)
                            .LineHorizontal(2)
                            .LineColor(Colors.Blue.Darken4);

                            outerCol.Item().Background(Colors.Blue.Lighten5)
                            .Padding(0.25f, Unit.Centimetre).Column(col =>
                            {
                                col.Item().Text("Approved by the Secretary of the Commissaires Panel").AlignCenter().Bold();

                                col.Item().Text(c.Event).AlignCenter().Italic();

                                //col.Item().PaddingTop(0.25f, Unit.Centimetre).Row(row =>
                                //{
                                //    row.RelativeItem(3).Text($"2026 National Masters Track Championships");
                                //    //row.RelativeItem().Text("Approved by the Secretary of the Commissaires Panel");
                                //    row.RelativeItem(1).Text("26/06/2026 14:02").AlignEnd();
                                //});
                            });

                            outerCol.Item().Row(row =>
                            {
                                row.RelativeItem(2).Text($"Document Generated: {DateTime.Now.ToString("HH:mm dd/MM/yyyy")}");
                                row.RelativeItem(2).AlignRight().Text(text =>
                                {
                                    text.Span("Page ");
                                    text.CurrentPageNumber();
                                    text.Span(" of ");
                                    text.TotalPages();
                                });

                                //text.CurrentPageNumber();
                                //text.Span(" / ");
                                //text.TotalPages();
                            });
                        });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
