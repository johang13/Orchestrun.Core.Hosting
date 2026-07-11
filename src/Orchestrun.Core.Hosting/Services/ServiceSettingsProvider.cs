using System.Collections.Concurrent;
using Orchestrun.Core.Hosting.Consumers;

namespace Orchestrun.Core.Hosting.Services;

/// <summary>
/// Interface for accessing service settings.
/// </summary>
public interface IServiceSettingsProvider
{
    string? Get(string key);
    string GetRequired(string key);
}

/// <summary>
/// Internal interface for writing service settings.
/// Only to be used by <see cref="ServiceSettingsChangedConsumer"/> for setting service settings in response to
/// a service settings changed event.
/// </summary>
internal interface IServiceSettingsWriter
{
    void Set(string key, string value, bool global = false);
}

/// <summary>
/// TODO
/// </summary>
internal sealed class ServiceSettingsProvider : IServiceSettingsProvider, IServiceSettingsWriter
{
    private readonly ConcurrentDictionary<string, string> _globalSettings = new();
    private readonly ConcurrentDictionary<string, string> _scopedSettings = new();

    public string? Get(string key)
        =>
            _scopedSettings.TryGetValue(key, out var scoped)
                ? scoped
                : _globalSettings.TryGetValue(key, out var global) 
                    ? global
                    : null;

    public string GetRequired(string key)
        =>
            _scopedSettings.TryGetValue(key, out var scoped)
                ? scoped
                : _globalSettings.TryGetValue(key, out var global)
                    ? global
                    : throw new KeyNotFoundException($"Key '{key}' not found in settings.");

    public void Set(string key, string value, bool global = false)
    {
        if (global)
            _globalSettings[key] = value;
        else
            _scopedSettings[key] = value;
    }
}