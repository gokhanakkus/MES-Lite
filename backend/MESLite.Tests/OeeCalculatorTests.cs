using MESLite.Application.Common.Oee;
using Xunit;

namespace MESLite.Tests;

/// <summary>Unit tests for the pure OEE math (no DB involved).</summary>
public class OeeCalculatorTests
{
    [Fact]
    public void Compute_PerfectRun_YieldsHundredPercent()
    {
        // 60 min planned, no downtime, produced exactly the ideal amount, zero defects.
        var result = OeeCalculator.Compute(
            plannedMinutes: 60,
            downtimeMinutes: 0,
            totalProduced: 500,
            idealRatePerHour: 500,
            qualityProduced: 500,
            qualityDefects: 0);

        Assert.Equal(1.0, result.Availability, 3);
        Assert.Equal(1.0, result.Performance, 3);
        Assert.Equal(1.0, result.Quality, 3);
        Assert.Equal(1.0, result.Oee, 3);
    }

    [Fact]
    public void Compute_Availability_IsRunTimeOverPlanned()
    {
        // 25% of the window was downtime -> 75% availability.
        var result = OeeCalculator.Compute(60, 15, 0, 500, 0, 0);
        Assert.Equal(0.75, result.Availability, 3);
    }

    [Fact]
    public void Compute_Performance_IsActualOverIdeal()
    {
        // Ran 60 min with no downtime; ideal would be 500, produced 400 -> 80%.
        var result = OeeCalculator.Compute(60, 0, 400, 500, 400, 0);
        Assert.Equal(0.80, result.Performance, 3);
    }

    [Fact]
    public void Compute_Quality_IsGoodOverProduced()
    {
        // 100 produced, 10 defects -> 90% quality.
        var result = OeeCalculator.Compute(60, 0, 100, 500, 100, 10);
        Assert.Equal(0.90, result.Quality, 3);
    }

    [Fact]
    public void Compute_Oee_IsProductOfThreeFactors()
    {
        // A=0.9 (6 min down), P=0.8 (produced 360 of ideal 450 over 54 min), Q=0.95
        var result = OeeCalculator.Compute(
            plannedMinutes: 60,
            downtimeMinutes: 6,
            totalProduced: 360,
            idealRatePerHour: 500,
            qualityProduced: 1000,
            qualityDefects: 50);

        Assert.Equal(0.90, result.Availability, 2);
        Assert.Equal(0.80, result.Performance, 2);
        Assert.Equal(0.95, result.Quality, 2);
        Assert.Equal(result.Availability * result.Performance * result.Quality, result.Oee, 5);
    }

    [Fact]
    public void Compute_FactorsAreClampedToOne()
    {
        // Over-production and downtime exceeding planned time must not break the 0..1 bounds.
        var result = OeeCalculator.Compute(60, 120, 5000, 500, 100, 0);
        Assert.InRange(result.Availability, 0, 1);
        Assert.InRange(result.Performance, 0, 1);
        Assert.InRange(result.Quality, 0, 1);
        Assert.Equal(0, result.Availability, 5); // downtime >= planned -> zero availability
    }

    [Fact]
    public void Compute_ZeroPlannedTime_ReturnsEmpty()
    {
        var result = OeeCalculator.Compute(0, 0, 100, 500, 100, 0);
        Assert.Equal(0, result.Oee, 5);
    }
}
