using Application.Validators;

namespace UnitTests;

public class GstRulesTests
{
    /// <summary>
    /// Two real, published GSTINs. Without a known-good pair the algorithm could be confidently
    /// wrong in the same direction every time and every test would still pass.
    /// </summary>
    [Theory]
    [InlineData("27AAPFU0939F1ZV")]
    [InlineData("29AAGCB7383J1Z4")]
    public void A_real_GSTIN_passes_its_own_checksum(string gstin)
    {
        Assert.True(GstRules.HasValidChecksum(gstin));
    }

    [Theory]
    [InlineData("27AAPFU0939F1ZA")]   // last character changed
    [InlineData("27AAPFU0939F1Z")]    // one short
    [InlineData("27aapfu0939f1zv")]   // lower case is not a GSTIN
    public void A_mistyped_GSTIN_does_not(string gstin)
    {
        Assert.False(GstRules.HasValidChecksum(gstin));
    }

    /// <summary>
    /// The first two characters of a GSTIN <i>are</i> the state code. A record where the two
    /// disagree bills tax for one state under a registration belonging to another, and the buyer's
    /// return never reconciles.
    /// </summary>
    [Theory]
    [InlineData("33AAECS1234F1ZV", "33", true)]
    [InlineData("27AAPFU0939F1ZV", "33", false)]
    [InlineData("07AAACG2115R1ZN", "7", true)]     // a single-digit state code still matches
    [InlineData(null, "33", true)]                 // no GSTIN, nothing to disagree with
    [InlineData("33AAECS1234F1ZV", null, true)]    // no state chosen yet
    public void A_GSTIN_must_agree_with_the_state_it_is_filed_under(
        string? gstin, string? stateCode, bool expected)
    {
        Assert.Equal(expected, GstRules.StateMatches(gstin, stateCode));
    }
}
