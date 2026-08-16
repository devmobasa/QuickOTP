using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuickOTP.Core.Models;
using QuickOTP.Core.Serialization;
using QuickOTP.Core.Services;
using Xunit;

namespace QuickOTP.Tests;

public class TwoFasImportServiceTests
{
    private const string SharedSecret = "GEZDGNBVGY3TQOJQ";

    [Fact]
    public void ImportFromPlainJson_Works()
    {
        var backup = new TwoFasBackup
        {
            Services =
            [
                CreateBackupService("id-1", "Example", "alice")
            ]
        };

        var json = JsonSerializer.Serialize( backup, QuickOTPJsonContext.Default.TwoFasBackup );
        var path = BuildTempPath("plain.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);

        var svc = new TwoFasImportService();
        var accounts = svc.ImportFrom2FasFile(path);

        Assert.Single(accounts);
        Assert.Equal("Example", accounts[0].Issuer);
        Assert.Equal("alice", accounts[0].Name);
        Assert.Equal(SharedSecret, accounts[0].Secret);
    }

    [Fact]
    public void ExportAndReimport_RoundTrips()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "2fac-tests", "roundtrip");
        Directory.CreateDirectory(tmpDir);
        var path = Path.Combine(tmpDir, "backup.2fas");

        var accounts = new[]
        {
            new TotpAccount { Name = "alice", Issuer = "Example", Secret = SharedSecret },
            new TotpAccount { Name = "bob", Issuer = "Example", Secret = "ONSWG4TFOQ======" }
        }.ToList();

        var svc = new TwoFasImportService();
        svc.Export2FasFormat(accounts, path, encrypt: false);

        var imported = svc.ImportFrom2FasFile(path);
        Assert.Equal(2, imported.Count);
        Assert.Contains(imported, a => a.Name == "alice");
        Assert.Contains(imported, a => a.Name == "bob");
    }

    [Fact]
    public void ImportFromOfficialEncryptedEnvelope_Works()
    {
        const string password = "example-password";
        var services = new List<TwoFasService>
        {
            CreateBackupService("id-1", "Example", "alice")
        };

        var envelope = new TwoFasBackup
        {
            Services = [],
            Groups = [],
            ServicesEncrypted = EncryptOfficialEnvelopePayload(
                JsonSerializer.Serialize( services, QuickOTPJsonContext.Default.ListTwoFasService ),
                password ),
            SchemaVersion = 4,
            AppVersionName = "5.4.2",
            AppOrigin = "ios"
        };

        var path = BuildTempPath("official-encrypted.2fas");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(envelope, QuickOTPJsonContext.Default.TwoFasBackup));

        var svc = new TwoFasImportService();
        var accounts = svc.ImportFrom2FasFile(path, password);

        Assert.Single(accounts);
        Assert.Equal("Example", accounts[0].Issuer);
        Assert.Equal("alice", accounts[0].Name);
        Assert.Equal(SharedSecret, accounts[0].Secret);
    }

    private static TwoFasService CreateBackupService(string id, string issuer, string accountName)
    {
        return new TwoFasService
        {
            Id = id,
            Name = issuer,
            Secret = SharedSecret,
            Otp = new TwoFasOtp { Account = accountName, Issuer = issuer }
        };
    }

    private static string BuildTempPath(string fileName)
    {
        return Path.Combine(Path.GetTempPath(), "2fac-tests", fileName);
    }

    private static string EncryptOfficialEnvelopePayload(string plainText, string password)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var salt = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(12);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10_000, HashAlgorithmName.SHA256, 32);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(iv, plainBytes, cipherBytes, tag);

        var combined = new byte[cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(cipherBytes, 0, combined, 0, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, combined, cipherBytes.Length, tag.Length);

        return $"{Convert.ToBase64String(combined)}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(iv)}";
    }
}
