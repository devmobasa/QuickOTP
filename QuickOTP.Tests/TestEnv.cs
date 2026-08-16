using System;
using System.Collections.Generic;
using System.IO;

namespace QuickOTP.Tests;

internal sealed class TestEnv : IDisposable
{
    private static readonly (string Name, string? Value)[] DefaultEnvironment =
    [
        ("HOME", null),
        ("XDG_CONFIG_HOME", null),
        ("APPDATA", null),
        ("LOCALAPPDATA", null),
        ("QUICKOTP_DISABLE_KEYCHAIN", "1"),
        ("QUICKOTP_MASTER_PASSWORD", null),
        ("QUICKOTP_MASTER_PASSWORD_FILE", null)
    ];

    private readonly string _tempRoot;
    private readonly Dictionary<string, string?> _originalEnv = new();
    private bool _disposed;

    public TestEnv()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "2fac-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public string TempRoot => _tempRoot;

    public void Apply()
    {
        foreach (var (name, value) in DefaultEnvironment)
        {
            var resolvedValue = value == null && ShouldUseTempRoot(name)
                ? _tempRoot
                : value;

            SetEnv(name, resolvedValue);
        }
    }

    private void SetEnv(string name, string? value)
    {
        if (!_originalEnv.ContainsKey(name))
        {
            _originalEnv[name] = Environment.GetEnvironmentVariable(name);
        }

        Environment.SetEnvironmentVariable(name, value);
    }

    private static bool ShouldUseTempRoot(string name)
    {
        return name is "HOME" or "XDG_CONFIG_HOME" or "APPDATA" or "LOCALAPPDATA";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _originalEnv)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
