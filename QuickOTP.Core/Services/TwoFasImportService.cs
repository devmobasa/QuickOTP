using System.Text.Json;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;
using QuickOTP.Core.Serialization;

namespace QuickOTP.Core.Services;

public partial class TwoFasImportService
{
    public bool BackupRequiresPassword( string filePath )
    {
        var fileContent = File.ReadAllText( filePath );
        return IsOfficialEncryptedBackupEnvelope( fileContent ) || IsEncrypted( fileContent );
    }

    public List<TotpAccount> ImportFrom2FasFile( string filePath, string? password = null )
    {
        try
        {
            var fileContent = ReadBackupContent( filePath, password );
            var backup = DeserializeBackup( fileContent );
            return MapServicesToAccounts( backup.Services );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to import 2FAS backup: {ex.Message}", ex );
        }
    }

    public void Export2FasFormat( List<TotpAccount> accounts, string filePath, bool encrypt = false, string? password = null )
    {
        try
        {
            var backup = CreateBackupEnvelope( );
            AddServicesForAccounts( backup, accounts );

            var json = AppJson.SerializeBackup( backup );
            WriteBackupContent( filePath, json, encrypt, password );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to export in 2FAS format: {ex.Message}", ex );
        }
    }

    private string ReadBackupContent( string filePath, string? password )
    {
        var fileContent = File.ReadAllText( filePath );
        if ( IsOfficialEncryptedBackupEnvelope( fileContent ) )
        {
            if ( string.IsNullOrEmpty( password ) )
            {
                throw new Exception( "This backup file is encrypted. Please provide a password." );
            }

            return DecryptOfficial2FasBackupEnvelope( fileContent, password );
        }

        if ( !IsEncrypted( fileContent ) )
        {
            return fileContent;
        }

        if ( string.IsNullOrEmpty( password ) )
        {
            throw new Exception( "This backup file is encrypted. Please provide a password." );
        }

        return Decrypt2FasBackup( fileContent, password );
    }

    private static TwoFasBackup DeserializeBackup( string fileContent )
    {
        if ( TryDeserializeServicesArray( fileContent, out var services ) )
        {
            return new TwoFasBackup { Services = services };
        }

        var backup = AppJson.DeserializeBackup( fileContent );
        if ( backup?.Services == null || !backup.Services.Any( ) )
        {
            throw new Exception( "No services found in the backup file." );
        }

        return backup;
    }

    private static bool TryDeserializeServicesArray( string fileContent, out List<TwoFasService> services )
    {
        services = [];

        try
        {
            using var document = JsonDocument.Parse( fileContent );
            if ( document.RootElement.ValueKind != JsonValueKind.Array )
            {
                return false;
            }

            services = AppJson.DeserializeServices( fileContent ) ?? [];
            return true;
        }
        catch ( JsonException )
        {
            services = [];
            return false;
        }
    }

    private static List<TotpAccount> MapServicesToAccounts( List<TwoFasService> services )
    {
        var accounts = new List<TotpAccount>( );

        foreach ( var service in services )
        {
            if ( string.IsNullOrEmpty( service.Secret ) )
            {
                continue;
            }

            accounts.Add( MapServiceToAccount( service ) );
        }

        return accounts;
    }

    private static TotpAccount MapServiceToAccount( TwoFasService service )
    {
        return new TotpAccount
        {
            Id = service.Id ?? Guid.NewGuid( ).ToString( ),
            Name = service.Otp?.Account ?? service.Name ?? AppConstants.Display.Unknown,
            Issuer = service.Otp?.Issuer ?? service.Name ?? AppConstants.Display.Unknown,
            Secret = service.Secret!.Replace( " ", "" ).ToUpper( ),
            Algorithm = service.Otp?.Algorithm ?? AppConstants.Otp.Sha1,
            Digits = service.Otp?.Digits ?? AppConstants.Otp.DefaultDigits,
            Period = service.Otp?.Period ?? AppConstants.Otp.DefaultPeriod,
            Icon = service.Icon?.Selected ?? service.Icon?.Brand?.Id,
            CreatedAt = service.UpdatedAt.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds( service.UpdatedAt.Value ).DateTime
                : DateTime.UtcNow
        };
    }

    private static TwoFasBackup CreateBackupEnvelope( )
    {
        return new TwoFasBackup
        {
            Services = [],
            Groups = [],
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds( ),
            SchemaVersion = AppConstants.TwoFas.SchemaVersion,
            AppVersionName = AppConstants.TwoFas.BackupAppVersionName,
            AppOrigin = AppConstants.TwoFas.BackupAppOrigin
        };
    }

    private static void AddServicesForAccounts( TwoFasBackup backup, List<TotpAccount> accounts )
    {
        foreach ( var account in accounts )
        {
            backup.Services.Add( MapAccountToService( account ) );
        }
    }

    private static TwoFasService MapAccountToService( TotpAccount account )
    {
        var service = new TwoFasService
        {
            Id = account.Id,
            Name = account.Issuer,
            Secret = account.Secret,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds( ),
            Order = new TwoFasOrder { Position = 0 },
            Otp = new TwoFasOtp
            {
                Account = account.Name,
                Issuer = account.Issuer,
                Digits = account.Digits,
                Period = account.Period,
                Algorithm = account.Algorithm,
                TokenType = AppConstants.TwoFas.TokenTypeTotp,
                Source = AppConstants.TwoFas.SourceManual
            }
        };

        service.Otp!.Link = BuildOtpAuthLink( account );
        return service;
    }

    private static string BuildOtpAuthLink( TotpAccount account )
    {
        return $"{AppConstants.OtpAuth.TotpPrefix}{Uri.EscapeDataString( account.Issuer )}:{Uri.EscapeDataString( account.Name )}?"
               + $"{AppConstants.OtpAuth.SecretParameter}={account.Secret}&{AppConstants.OtpAuth.IssuerParameter}={Uri.EscapeDataString( account.Issuer )}&"
               + $"{AppConstants.OtpAuth.AlgorithmParameter}={account.Algorithm}&{AppConstants.OtpAuth.DigitsParameter}={account.Digits}&{AppConstants.OtpAuth.PeriodParameter}={account.Period}";
    }

    private void WriteBackupContent( string filePath, string json, bool encrypt, string? password )
    {
        if ( encrypt && !string.IsNullOrEmpty( password ) )
        {
            var encryptedContent = Encrypt2FasBackup( json, password );
            File.WriteAllText( filePath, encryptedContent );
            return;
        }

        File.WriteAllText( filePath, json );
    }
}
