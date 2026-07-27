using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CycleScoresWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Markdown;
using System;
using System.Diagnostics.Tracing;
using System.Net.Mail;
using static System.Net.Mime.MediaTypeNames;

namespace CycleScoresWeb.Services
{
    
    public interface IPDFGeneratorService
    {
        public byte[] GenerateCommunique(Communique c);
        public byte[] GenerateResultsBook(List<Communique> EventCommuniques);
    }

    public class PDFGeneratorService : IPDFGeneratorService
    {
        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainerClient;

        public PDFGeneratorService()
        {
            _blobServiceClient = new BlobServiceClient(
                new Uri("https://cyclescoresweb.blob.core.windows.net"),
                new DefaultAzureCredential());

            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("header-images");
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

            var doc = GenerateQuestCommuniqueDocument(c);

            return doc.GeneratePdf();
        }

        public byte[] GenerateResultsBook(List<Communique> EventCommuniques)
        {
            //throw new NotImplementedException();

            List<Document> docList = new List<Document>();

            foreach (var ec in EventCommuniques)
            {
                docList.Add(GenerateQuestCommuniqueDocument(ec));
            }

            var merged = Document.Merge(docList);

            return merged.GeneratePdf();

        }

        private Document GenerateQuestCommuniqueDocument(Communique c)
        {
            byte[]? image = null;

            if (c.HeaderImage != null && !c.HeaderImage.IsWhiteSpace())
            {
                try
                {
                    image = TryGetHeaderImage(c.HeaderImage).Result;
                }
                catch
                {
                    image = null;
                }

            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    
                    if (c.LandScape == true)
                    {
                        page.Size(PageSizes.A4.Landscape());
                    }
                    else
                    {
                        page.Size(PageSizes.A4);
                    }
                    page.Margin(0.5f, Unit.Centimetre);
                    //page.MarginTop(0.5f, Unit.Centimetre);
                    //page.MarginBottom(0.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10.5f));

                    page.Header()
                    .Column(col =>
                    {
                        col.Item().PaddingBottom(0.25f, Unit.Centimetre).Row(r =>
                        {
                            if (c.CommuniqueId != null)
                            {
                                r.RelativeItem(2).Text(c.CommuniqueId.ToString() ?? "").FontSize(10).FontColor(Colors.Grey.Lighten1);
                            }

                            if (c.CommuniqueNumber != null && !c.CommuniqueNumber.IsWhiteSpace())
                            {
                                r.RelativeItem(2).Text($"Communiqué {c.CommuniqueNumber}").Italic().AlignEnd();
                            }
                        });



                        if (image != null)
                        {
                            col.Item().Image(image).FitWidth();
                        }
                        else if (c.LandScape == false && c.Minimal == true)
                        {
                            col.Item().Background(Colors.Blue.Lighten5)
                            .PaddingTop(0.25f, Unit.Centimetre)
                            .PaddingBottom(0.25f, Unit.Centimetre)
                            .Text(c.Event)
                            .Bold()
                            .FontSize(22)
                            .Underline().DecorationSolid()
                            .FontColor(Colors.Black)
                            .AlignCenter();
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

                        if (c.HeaderText != null && !c.HeaderText.IsWhiteSpace())
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
                                        if (s.HeatTitle != null && !s.HeatTitle.IsWhiteSpace())
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
                                                row.ConstantItem(75).Text("NAT").Bold();
                                            });

                                        foreach (var r in s.Riders)
                                        {
                                            y.Item().PaddingLeft(20)
                                            .Row(row =>
                                            {
                                                row.Spacing(5);
                                                row.ConstantItem(40).Text($"{r?.Bib.ToString() ?? ""}");
                                                row.ConstantItem(375).Text(r.Name);
                                                row.ConstantItem(75).Text(r.Nation ?? "");
                                                // ToDo -> Can we have nation flags?
                                                //row.ConstantItem(30).Image(Placeholders.Image(1, 1));

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
                                    //.PreventPageBreak()
                                    .Column(y =>
                                    {
                                        if (r.HeatTitle != null && !r.HeatTitle.IsWhiteSpace())
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
                                                    row.ConstantItem(200).Text("Rider").Bold();
                                                    row.ConstantItem(75).Text("Nation").Bold();
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
                                                    row.ConstantItem(200).Text(result.Name);
                                                    row.ConstantItem(75).Text(result.Nation == null ? "" : result.Nation);
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
                                if (c.Minimal == true)
                                {
                                    page.DefaultTextStyle(x => x.FontSize(6));
                                }
                                else
                                {
                                    page.DefaultTextStyle(x => x.FontSize(10));
                                }

                                var count = c.BodyText.Length;

                                foreach (var text in c.BodyText)
                                {
                                    count--;

                                    x.Item()
                                    //.PreventPageBreak()
                                    .PaddingTop(0.1f, Unit.Centimetre)
                                    //.Border(2, Colors.Blue.Lighten4)
                                    .Padding(0.1f, Unit.Centimetre)
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

                            if (c.Minimal != true)
                            {
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
                            }

                            

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
        }
    }
}
