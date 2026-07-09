using Agent.Core;
using Agent.Summarizer;
using FluentAssertions;

namespace Agent.Console.Tests;

public class NoOpSummarizerTests
{
    [Fact]
    public async Task SummarizeAsync_ReturnsNull()
    {
        var report = new DailyReport
        {
            Date = new DateOnly(2026, 7, 8),
            Summary = PortfolioSummary.From([]),
            Stocks = [],
        };

        var summary = await new NoOpSummarizer().SummarizeAsync(report);

        summary.Should().BeNull();
    }
}
