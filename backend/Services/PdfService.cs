using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RaffleIt.API.Models;

namespace RaffleIt.API.Services;

public class PdfService
{
    private readonly QrCodeService _qrCodeService;
    private readonly IConfiguration _config;

    public PdfService(QrCodeService qrCodeService, IConfiguration config)
    {
        _qrCodeService = qrCodeService;
        _config = config;
    }

    public byte[] GenerateRafflePdf(Raffle raffle, List<Ticket> tickets)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var baseUrl = _config["AppSettings:BaseUrl"];

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Column(col =>
                {
                    col.Item().Text($"Raffle Tickets: {raffle.Name}")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken2).AlignCenter();
                    col.Item().Text($"{raffle.Organisation ?? ""}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1).AlignCenter();
                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Lighten2);
                });

                page.Content().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Spacing(8);

                    foreach (var ticket in tickets)
                    {
                        var ticketUrl = $"{baseUrl}/claim/{ticket.Id}";
                        var qrBytes = _qrCodeService.GenerateQrCode(ticketUrl, 8);

                        grid.Item().Border(1).BorderColor(Colors.Blue.Lighten2)
                            .Background(Colors.Blue.Lighten5)
                            .Padding(8)
                            .Column(col =>
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(innerCol =>
                                    {
                                        innerCol.Item().Text(raffle.Name)
                                            .FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                                        if (!string.IsNullOrWhiteSpace(raffle.Organisation))
                                            innerCol.Item().Text(raffle.Organisation)
                                                .FontSize(8).FontColor(Colors.Grey.Darken2);
                                        innerCol.Item().PaddingTop(4)
                                            .Text($"Ticket #{ticket.TicketNumber:D4}")
                                            .FontSize(14).Bold().FontColor(Colors.Red.Darken2);
                                        innerCol.Item().PaddingTop(2)
                                            .Text($"Price: {raffle.Currency} {raffle.TicketPrice:F2}")
                                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                                        if (!string.IsNullOrWhiteSpace(raffle.Prizes))
                                        {
                                            var shortPrize = raffle.Prizes.Length > 60
                                                ? raffle.Prizes[..60] + "..."
                                                : raffle.Prizes;
                                            innerCol.Item().PaddingTop(2)
                                                .Text($"Prize: {shortPrize}")
                                                .FontSize(8).FontColor(Colors.Green.Darken2);
                                        }
                                        innerCol.Item().PaddingTop(4)
                                            .Text($"ID: {ticket.Id.ToString()[..8].ToUpper()}")
                                            .FontSize(7).FontColor(Colors.Grey.Medium);
                                    });

                                    row.ConstantItem(70).Column(qrCol =>
                                    {
                                        qrCol.Item().Width(65).Height(65).Image(qrBytes);
                                        qrCol.Item().Text("Scan to\nclaim").FontSize(7)
                                            .AlignCenter().FontColor(Colors.Grey.Darken1);
                                    });
                                });

                                col.Item().PaddingTop(4).LineHorizontal(0.5f)
                                    .LineColor(Colors.Blue.Lighten3);
                                col.Item().PaddingTop(3).Text("VALID TICKET - NOT TRANSFERABLE")
                                    .FontSize(6).AlignCenter().FontColor(Colors.Grey.Medium).Italic();
                            });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }
}
