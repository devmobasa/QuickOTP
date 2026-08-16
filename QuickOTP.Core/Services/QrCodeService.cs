using QRCoder;
using QuickOTP.Core.Configuration;

namespace QuickOTP.Core.Services;

public class QrCodeService
{
    public void GenerateQrCode( string data, string outputPath )
    {
        var qrCodeBytes = GeneratePngBytes( data, 20 );
        File.WriteAllBytes( outputPath, qrCodeBytes );
    }

    public byte[] GeneratePngBytes( string data, int pixelsPerModule = 8 ) =>
        CreatePngBytes( data, QRCodeGenerator.ECCLevel.Q, pixelsPerModule );

    public void DisplayQrCodeInConsole( string data )
    {
        Console.WriteLine( GenerateQrCodeAscii( data ) );
    }

    public string GenerateQrCodeAscii( string data )
    {
        using var qrGenerator = new QRCodeGenerator( );
        var qrCodeData = qrGenerator.CreateQrCode( data, QRCodeGenerator.ECCLevel.L );

        using var qrCode = new AsciiQRCode( qrCodeData );
        return qrCode.GetGraphic( 1, "██", "  " );
    }

    public string GenerateOtpAuthUri(
        string secret,
        string accountName,
        string issuer,
        string algorithm = AppConstants.Otp.Sha1,
        int digits = AppConstants.Otp.DefaultDigits,
        int period = AppConstants.Otp.DefaultPeriod )
    {
        return $"{AppConstants.OtpAuth.TotpPrefix}{Uri.EscapeDataString( issuer )}:{Uri.EscapeDataString( accountName )}?"
               + $"{AppConstants.OtpAuth.SecretParameter}={secret}&"
               + $"{AppConstants.OtpAuth.IssuerParameter}={Uri.EscapeDataString( issuer )}&"
               + $"{AppConstants.OtpAuth.AlgorithmParameter}={algorithm}&"
               + $"{AppConstants.OtpAuth.DigitsParameter}={digits}&"
               + $"{AppConstants.OtpAuth.PeriodParameter}={period}";
    }

    private static byte[] CreatePngBytes( string data, QRCodeGenerator.ECCLevel level, int pixelsPerModule )
    {
        using var qrGenerator = new QRCodeGenerator( );
        var qrCodeData = qrGenerator.CreateQrCode( data, level );

        using var qrCode = new PngByteQRCode( qrCodeData );
        return qrCode.GetGraphic( pixelsPerModule );
    }
}
