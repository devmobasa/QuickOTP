using System.Text.Json.Serialization;

namespace QuickOTP.Core.Models;

public class TwoFasBackup
{
    public List<TwoFasService> Services { get; set; } = [];
    public List<TwoFasGroup> Groups { get; set; } = [];
    public long UpdatedAt { get; set; }
    public int SchemaVersion { get; set; }
    public string? AppVersionName { get; set; }
    public string? AppOrigin { get; set; }

    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    public string? ServicesEncrypted { get; set; }
}
