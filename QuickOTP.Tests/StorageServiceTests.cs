using System;
using System.IO;
using QuickOTP.Core.Models;
using QuickOTP.Core.Services;
using Xunit;

namespace QuickOTP.Tests;

public class StorageServiceTests
{
    private const string SharedSecret = "GEZDGNBVGY3TQOJQ";

    [Fact]
    public void SaveAndLoad_RoundTripsAccounts()
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        var account = CreateAccount("alice", "example");

        storage.AddAccount(account);
        var loaded = storage.LoadAccounts();

        Assert.Single(loaded);
        Assert.Equal(account.Secret, loaded[0].Secret);
        Assert.Equal(account.Issuer, loaded[0].Issuer);
    }

    [Fact]
    public void ExportImport_Encrypted_RoundTrips()
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        var account = CreateAccount("bob", "example");
        storage.AddAccount(account);

        var exportPath = Path.Combine(env.TempRoot, "accounts.enc.json");
        storage.ExportAccounts(exportPath, encrypted: true);

        // new instance to ensure key handling still works with same env
        var storage2 = CreateStorageService();
        storage2.ImportAccounts(exportPath, encrypted: true);

        var loaded = storage2.LoadAccounts();
        Assert.Equal(2, loaded.Count); // original + imported copy
    }

    [Fact]
    public void UpdateAccount_ReplacesMatchingAccount()
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        var account = CreateAccount("alice", "example");
        storage.AddAccount(account);

        account.Name = "alice-renamed";
        account.Issuer = "example-renamed";

        var updated = storage.UpdateAccount(account);
        var loaded = storage.LoadAccounts();

        Assert.True(updated);
        var saved = Assert.Single(loaded);
        Assert.Equal(account.Id, saved.Id);
        Assert.Equal("alice-renamed", saved.Name);
        Assert.Equal("example-renamed", saved.Issuer);
        Assert.Equal(SharedSecret, saved.Secret);
    }

    [Fact]
    public void UpdateAccount_ReturnsFalseWhenAccountDoesNotExist()
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        var updated = storage.UpdateAccount(CreateAccount("missing", "example"));

        Assert.False(updated);
        Assert.Empty(storage.LoadAccounts());
    }

    [Theory]
    [InlineData("not-a-valid-vault")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[null]")]
    public void AddAccounts_UnreadableVault_DoesNotOverwriteFile(string vaultContents)
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        storage.AddAccount(CreateAccount("alice", "example"));

        var accountsFile = FindAccountsFile(env.TempRoot);
        File.WriteAllText(accountsFile, vaultContents);

        Assert.ThrowsAny<Exception>(() =>
            storage.AddAccounts([CreateAccount("imported", "other")]));

        Assert.Equal(vaultContents, File.ReadAllText(accountsFile));
        Assert.ThrowsAny<Exception>(() => storage.LoadAccounts());
    }

    [Fact]
    public void AddAccounts_EmptyJsonArray_IsAValidVault()
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        storage.AddAccount(CreateAccount("alice", "example"));

        var accountsFile = FindAccountsFile(env.TempRoot);
        File.WriteAllText(accountsFile, "[]");

        storage.AddAccounts([CreateAccount("imported", "other")]);

        var loaded = storage.LoadAccounts();
        var imported = Assert.Single(loaded);
        Assert.Equal("imported", imported.Name);
        Assert.Equal("other", imported.Issuer);
    }

    [Fact]
    public void ImportAccounts_PascalCaseJson_ReadsExistingVaultShape()
    {
        using var env = new TestEnv();
        env.Apply();

        var storage = CreateStorageService();
        var path = Path.Combine(env.TempRoot, "legacy.json");
        File.WriteAllText(path, """
            [{"Id":"id-1","Name":"alice","Issuer":"example","Secret":"GEZDGNBVGY3TQOJQ","Algorithm":"SHA1","Digits":6,"Period":30}]
            """);

        storage.ImportAccounts(path, encrypted: false);

        var loaded = storage.LoadAccounts();
        var account = Assert.Single(loaded);
        Assert.Equal("id-1", account.Id);
        Assert.Equal("alice", account.Name);
        Assert.Equal("example", account.Issuer);
        Assert.Equal(SharedSecret, account.Secret);
    }

    private static string FindAccountsFile(string root)
    {
        var matches = Directory.GetFiles(root, "accounts.json", SearchOption.AllDirectories);
        var file = Assert.Single(matches);
        return file;
    }

    private static StorageService CreateStorageService() => new();

    private static TotpAccount CreateAccount(string name, string issuer)
    {
        return new TotpAccount
        {
            Name = name,
            Issuer = issuer,
            Secret = SharedSecret
        };
    }
}
