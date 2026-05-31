using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Models;
using MESLite.Application.Features.Reports;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class ReportsController : ApiControllerBase
{
    /// <summary>Rapor verisi (JSON). period = Daily | Weekly | Monthly.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] PeriodType period = PeriodType.Daily, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetReportQuery(period), ct));

    /// <summary>Raporu CSV olarak indir.</summary>
    [HttpGet("csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] PeriodType period = PeriodType.Daily, CancellationToken ct = default)
    {
        var report = await Mediator.Send(new GetReportQuery(period), ct);
        var sb = new StringBuilder();
        sb.AppendLine("Makine;Tip;Üretim;Hata;Availability%;Performance%;Quality%;OEE%;Duruş(saat)");
        foreach (var r in report.Rows)
        {
            sb.AppendLine(string.Join(';', new[]
            {
                r.MachineName, r.MachineType,
                r.TotalProduction.ToString(CultureInfo.InvariantCulture),
                r.TotalDefects.ToString(CultureInfo.InvariantCulture),
                r.Availability.ToString(CultureInfo.InvariantCulture),
                r.Performance.ToString(CultureInfo.InvariantCulture),
                r.Quality.ToString(CultureInfo.InvariantCulture),
                r.Oee.ToString(CultureInfo.InvariantCulture),
                r.DowntimeHours.ToString(CultureInfo.InvariantCulture)
            }));
        }

        // UTF-8 BOM so Excel renders Turkish characters correctly.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"MESLite-Rapor-{period}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>Raporu Excel (.xlsx) olarak indir.</summary>
    [HttpGet("excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] PeriodType period = PeriodType.Daily, CancellationToken ct = default)
    {
        var report = await Mediator.Send(new GetReportQuery(period), ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"Rapor-{period}");

        string[] headers = { "Makine", "Tip", "Üretim", "Hata", "Availability %", "Performance %", "Quality %", "OEE %", "Duruş (saat)" };
        for (var c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            ws.Cell(1, c + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.MachineName;
            ws.Cell(row, 2).Value = r.MachineType;
            ws.Cell(row, 3).Value = r.TotalProduction;
            ws.Cell(row, 4).Value = r.TotalDefects;
            ws.Cell(row, 5).Value = r.Availability;
            ws.Cell(row, 6).Value = r.Performance;
            ws.Cell(row, 7).Value = r.Quality;
            ws.Cell(row, 8).Value = r.Oee;
            ws.Cell(row, 9).Value = r.DowntimeHours;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"MESLite-Rapor-{period}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }
}
