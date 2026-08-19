using Application.Common;

namespace UnitTests;

public class DocumentNumberingTests
{
    [Fact]
    public void FindGaps_OfAnUnbrokenSeries_IsEmpty()
    {
        Assert.Empty(DocumentNumbering.FindGaps([1, 2, 3, 4, 5]));
    }

    [Fact]
    public void FindGaps_FindsAHoleInTheMiddle()
    {
        Assert.Equal([3], DocumentNumbering.FindGaps([1, 2, 4, 5]));
    }

    [Fact]
    public void FindGaps_FindsSeveralHoles()
    {
        Assert.Equal([2, 5, 6], DocumentNumbering.FindGaps([1, 3, 4, 7]));
    }

    /// <summary>
    /// The check counts from 1, not from the lowest number present. A series that opens at 4 is
    /// missing its first three documents, and reporting it as unbroken would hide exactly the thing
    /// an auditor is looking for.
    /// </summary>
    [Fact]
    public void FindGaps_TreatsAMissingStartAsAGap()
    {
        Assert.Equal([1, 2, 3], DocumentNumbering.FindGaps([4, 5]));
    }

    [Fact]
    public void FindGaps_OfNothingIsEmpty()
    {
        // No documents yet is not the same as a broken series.
        Assert.Empty(DocumentNumbering.FindGaps([]));
    }

    [Fact]
    public void FindGaps_IgnoresOrderAndDuplicates()
    {
        Assert.Equal([2], DocumentNumbering.FindGaps([3, 1, 3, 4]));
    }

    [Fact]
    public void FindGaps_OfASingleFirstDocumentIsEmpty()
    {
        Assert.Empty(DocumentNumbering.FindGaps([1]));
    }
}
