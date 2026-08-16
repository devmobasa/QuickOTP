using System;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;
using QuickOTP.Core.Services;
using Xunit;

namespace QuickOTP.Tests;

public class TotpServiceTests
{
    private const string LongTestSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [Fact]
    public void GenerateTotp_KnownVector_Succeeds()
    {
        // RFC 6238 test vector (SHA1, 8 digits, 30s, secret "12345678901234567890")
        var account = CreateAccount(AppConstants.Otp.Sha1, digits: 8, period: 30);

        var service = new TotpService();
        var time = new DateTimeOffset(1970, 1, 1, 0, 0, 59, TimeSpan.Zero); // T = 1
        var code = service.GenerateTotpAt(account, time);
        Assert.Equal("94287082", code);
    }

    [Theory]
    [InlineData(AppConstants.Otp.Sha1)]
    [InlineData(AppConstants.Otp.Sha256)]
    [InlineData(AppConstants.Otp.Sha512)]
    public void GenerateTotp_SupportsAlgorithms(string algorithm)
    {
        var account = CreateAccount(algorithm, digits: 6, period: 30);

        var service = new TotpService();
        var time = new DateTimeOffset(1970, 1, 1, 0, 1, 30, TimeSpan.Zero); // arbitrary
        var code = service.GenerateTotpAt(account, time);
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void ParseAccountFromOtpAuthUri_ReadsLabelAndQuery()
    {
        const string uri = "otpauth://totp/Example:alice@google.com?secret=JBSWY3DPEHPK3PXP&issuer=Example&algorithm=SHA256&digits=8&period=60";
        var service = new TotpService();

        var account = service.ParseAccountFromOtpAuthUri(uri);

        Assert.Equal("alice@google.com", account.Name);
        Assert.Equal("Example", account.Issuer);
        Assert.Equal("JBSWY3DPEHPK3PXP", account.Secret);
        Assert.Equal(AppConstants.Otp.Sha256, account.Algorithm);
        Assert.Equal(8, account.Digits);
        Assert.Equal(60, account.Period);
    }

    [Fact]
    public void ParseAccountFromOtpAuthUri_InvalidPrefix_Throws()
    {
        var service = new TotpService();
        Assert.ThrowsAny<Exception>(() => service.ParseAccountFromOtpAuthUri("https://example.com"));
    }

    [Fact]
    public void ParseAccountFromOtpAuthUri_HotpUri_Throws()
    {
        const string uri = "otpauth://hotp/Example:alice@google.com?secret=JBSWY3DPEHPK3PXP&counter=1";
        var service = new TotpService();

        var ex = Assert.ThrowsAny<Exception>(() => service.ParseAccountFromOtpAuthUri(uri));
        Assert.Contains("HOTP", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseAccountFromOtpAuthUri_PercentEncodedLabelSeparator_SplitsIssuerAndName()
    {
        const string uri = "otpauth://totp/Example%3Aalice@google.com?secret=JBSWY3DPEHPK3PXP";
        var service = new TotpService();

        var account = service.ParseAccountFromOtpAuthUri(uri);

        Assert.Equal("alice@google.com", account.Name);
        Assert.Equal("Example", account.Issuer);
        Assert.Equal("JBSWY3DPEHPK3PXP", account.Secret);
    }

    [Fact]
    public void GenerateTotp_InvalidSecret_Throws()
    {
        var account = new TotpAccount { Secret = "***not-base32***" };
        var service = new TotpService();
        Assert.ThrowsAny<Exception>(() => service.GenerateTotp(account));
    }

    private static TotpAccount CreateAccount(string algorithm, int digits, int period)
    {
        return new TotpAccount
        {
            Secret = LongTestSecret,
            Algorithm = algorithm,
            Digits = digits,
            Period = period
        };
    }
}
