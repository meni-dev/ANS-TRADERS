using Application.Common;

namespace UnitTests;

public class PaymentAllocationPlannerTests
{
    private static OpenDocument Bill(string date, decimal outstanding, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), DateOnly.Parse(date), outstanding);

    [Fact]
    public void Plan_ClosesTheOldestBillFirst()
    {
        var oldest = Bill("2026-07-01", 1000);
        var newest = Bill("2026-08-01", 1000);

        var plan = PaymentAllocationPlanner.Plan(1000, [newest, oldest]);

        var only = Assert.Single(plan);
        Assert.Equal(oldest.DocumentId, only.DocumentId);
        Assert.Equal(1000, only.Amount);
    }

    [Fact]
    public void Plan_SpillsOntoTheNextBillOnceTheFirstIsFull()
    {
        var first = Bill("2026-07-01", 600);
        var second = Bill("2026-07-15", 900);

        var plan = PaymentAllocationPlanner.Plan(1000, [first, second]);

        Assert.Equal(2, plan.Count);
        Assert.Equal(600, plan[0].Amount);
        Assert.Equal(400, plan[1].Amount);
        Assert.Equal(second.DocumentId, plan[1].DocumentId);
    }

    [Fact]
    public void Plan_NeverGivesADocumentMoreThanItsOutstanding()
    {
        var plan = PaymentAllocationPlanner.Plan(5000, [Bill("2026-07-01", 300)]);

        Assert.Equal(300, Assert.Single(plan).Amount);
    }

    /// <summary>Money with nowhere to go is an advance, not an error.</summary>
    [Fact]
    public void Unallocated_IsWhatIsLeftOver()
    {
        var bills = new[] { Bill("2026-07-01", 300), Bill("2026-07-02", 200) };

        var plan = PaymentAllocationPlanner.Plan(1000, bills);

        Assert.Equal(500, plan.Sum(p => p.Amount));
        Assert.Equal(500, PaymentAllocationPlanner.Unallocated(1000, plan));
    }

    [Fact]
    public void Unallocated_IsZeroWhenEverythingLanded()
    {
        var plan = PaymentAllocationPlanner.Plan(500, [Bill("2026-07-01", 500)]);

        Assert.Equal(0, PaymentAllocationPlanner.Unallocated(500, plan));
    }

    [Fact]
    public void Plan_SkipsDocumentsWithNothingOutstanding()
    {
        var settled = Bill("2026-06-01", 0);
        var open = Bill("2026-07-01", 400);

        var plan = PaymentAllocationPlanner.Plan(400, [settled, open]);

        Assert.Equal(open.DocumentId, Assert.Single(plan).DocumentId);
    }

    [Fact]
    public void Plan_OfNothingIsEmpty()
    {
        Assert.Empty(PaymentAllocationPlanner.Plan(0, [Bill("2026-07-01", 500)]));
        Assert.Empty(PaymentAllocationPlanner.Plan(500, []));
    }

    [Fact]
    public void Plan_IgnoresANegativeAmount()
    {
        Assert.Empty(PaymentAllocationPlanner.Plan(-100, [Bill("2026-07-01", 500)]));
    }

    /// <summary>
    /// Two bills the same day must be consumed in the same order every run, or a re-run of the
    /// backfill would produce a different set of allocations from the same inputs.
    /// </summary>
    [Fact]
    public void Plan_BreaksASameDayTieDeterministically()
    {
        var low = Bill("2026-07-01", 500, new Guid("11111111-0000-0000-0000-000000000000"));
        var high = Bill("2026-07-01", 500, new Guid("22222222-0000-0000-0000-000000000000"));

        var forwards = PaymentAllocationPlanner.Plan(500, [low, high]);
        var backwards = PaymentAllocationPlanner.Plan(500, [high, low]);

        Assert.Equal(low.DocumentId, Assert.Single(forwards).DocumentId);
        Assert.Equal(low.DocumentId, Assert.Single(backwards).DocumentId);
    }

    [Fact]
    public void Plan_RoundsToPaiseSoTheSumIsExact()
    {
        var plan = PaymentAllocationPlanner.Plan(100.005m, [Bill("2026-07-01", 100.004m)]);

        Assert.Equal(100.00m, Assert.Single(plan).Amount);
        Assert.Equal(0.01m, PaymentAllocationPlanner.Unallocated(100.005m, plan));
    }
}
