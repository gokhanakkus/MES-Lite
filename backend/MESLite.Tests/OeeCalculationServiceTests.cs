using MESLite.Application.Services;
using MESLite.Domain.Entities;
using MESLite.Domain.Enums;
using MESLite.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESLite.Tests;

/// <summary>
/// Exercises <see cref="OeeCalculationService"/> end-to-end against a real (in-memory SQLite) database,
/// verifying the EF queries and downtime-overlap clamping produce correct OEE.
/// </summary>
public class OeeCalculationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public OeeCalculationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task CalculateAsync_ComputesExpectedOee()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddHours(1); // 60 min window

        var machine = new Machine
        {
            Name = "Dokuma-Test",
            MachineType = MachineType.Weaving,
            ProductionLine = "Hat-A",
            IdealRunRatePerHour = 500,
            Status = MachineStatus.Running,
        };
        _db.Machines.Add(machine);
        await _db.SaveChangesAsync();

        // 6 minutes of downtime within the window -> availability 0.9, run time 54 min.
        _db.Downtimes.Add(new Downtime
        {
            MachineId = machine.Id,
            Reason = DowntimeReason.YarnBreak,
            StartTime = from.AddMinutes(10),
            EndTime = from.AddMinutes(16),
        });

        // Produced 360. Ideal over 54 min = 500 * (54/60) = 450 -> performance 0.8.
        _db.ProductionRecords.Add(new ProductionRecord { MachineId = machine.Id, Quantity = 360, ProducedAt = from.AddMinutes(30) });

        // Quality: 1000 produced, 50 defects -> quality 0.95.
        _db.QualityRecords.Add(new QualityRecord { MachineId = machine.Id, ProducedQuantity = 1000, DefectQuantity = 50, CreatedAt = from.AddMinutes(45) });

        await _db.SaveChangesAsync();

        var service = new OeeCalculationService(_db);
        var result = await service.CalculateAsync(machine.Id, from, to);

        Assert.Equal(0.90, result.Availability, 2);
        Assert.Equal(0.80, result.Performance, 2);
        Assert.Equal(0.95, result.Quality, 2);
        Assert.Equal(0.684, result.Oee, 2); // 0.9 * 0.8 * 0.95
    }

    [Fact]
    public async Task CalculateForAllAsync_ReturnsEntryPerMachine()
    {
        _db.Machines.AddRange(
            new Machine { Name = "M1", MachineType = MachineType.Dyeing, ProductionLine = "B", IdealRunRatePerHour = 1000 },
            new Machine { Name = "M2", MachineType = MachineType.Cutting, ProductionLine = "C", IdealRunRatePerHour = 700 });
        await _db.SaveChangesAsync();

        var from = DateTime.UtcNow.AddHours(-1);
        var service = new OeeCalculationService(_db);

        var all = await service.CalculateForAllAsync(from, DateTime.UtcNow);

        Assert.Equal(2, all.Count);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
