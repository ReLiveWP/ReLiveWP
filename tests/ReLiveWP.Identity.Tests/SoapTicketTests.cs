using ReLiveWP.Identity.Soap;

namespace ReLiveWP.Identity.Tests;

public class SoapTicketTests
{
    [Theory]
    [InlineData("t=eyJ.payload.sig&p=", "eyJ.payload.sig")]
    [InlineData("t=eyJ.payload.sig&amp;p=", "eyJ.payload.sig")]
    [InlineData("t=eyJ.payload.sig", "eyJ.payload.sig")]
    public void The_jwt_comes_out_of_the_passport_ticket(string ticket, string expected)
        => Assert.Equal(expected, SoapTicketVerifier.ExtractJwt(ticket));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("p=")]
    [InlineData("t=&p=")]
    public void A_ticket_without_a_jwt_is_missing(string? ticket)
        => Assert.Null(SoapTicketVerifier.ExtractJwt(ticket));

    // profile.asmx faulted with these before the verifier was shared, and the phone may care
    [Theory]
    [InlineData(SoapTicketStatus.Missing, "Missing TicketToken.")]
    [InlineData(SoapTicketStatus.ServiceError, "Authentication service error.")]
    [InlineData(SoapTicketStatus.Invalid, "Invalid TicketToken.")]
    public void Failures_keep_their_fault_wording(SoapTicketStatus status, string expected)
    {
        var result = new SoapTicketResult(status, "", new Dictionary<string, string>());

        Assert.False(result.IsValid);
        Assert.Equal(expected, result.FailureMessage);
    }

    [Fact]
    public void An_empty_claim_reads_as_absent()
    {
        var result = new SoapTicketResult(SoapTicketStatus.Valid, "user",
            new Dictionary<string, string> { ["preferred_username"] = "", ["email"] = "wam@example.test" });

        Assert.Null(result.Claim("preferred_username"));
        Assert.Equal("wam@example.test", result.Claim("email"));
        Assert.Null(result.Claim("nope"));
    }
}
