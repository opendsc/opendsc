// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;

namespace OpenDsc.Server.Services;

public enum ThemePreference
{
    Light,
    Dark,
    System
}

public class ThemeService
{
    private const string StorageKey = "opendsc-theme";
    
    public ThemePreference Preference { get; private set; } = ThemePreference.System;
    public bool IsDarkMode { get; private set; }
    
    public event Action? OnThemeChanged;

    public async Task InitializeAsync(ProtectedLocalStorage localStorage)
    {
        try
        {
            var result = await localStorage.GetAsync<string>(StorageKey);
            if (result.Success && !string.IsNullOrEmpty(result.Value))
            {
                if (Enum.TryParse<ThemePreference>(result.Value, out var preference))
                {
                    Preference = preference;
                }
            }
        }
        catch
        {
            // Storage not available yet (prerendering), use default
        }

        // For System preference, default to light mode
        // Browser-based system detection would require JavaScript
        if (Preference == ThemePreference.System)
        {
            IsDarkMode = false; // Default to light
        }
        else
        {
            IsDarkMode = Preference == ThemePreference.Dark;
        }
    }

    public async Task SetPreferenceAsync(ThemePreference preference, ProtectedLocalStorage localStorage)
    {
        Preference = preference;
        
        try
        {
            await localStorage.SetAsync(StorageKey, preference.ToString());
        }
        catch
        {
            // Storage operation failed, continue anyway
        }

        if (Preference == ThemePreference.System)
        {
            IsDarkMode = false; // Default to light for System
        }
        else
        {
            IsDarkMode = Preference == ThemePreference.Dark;
        }

        OnThemeChanged?.Invoke();
    }

    public async Task ToggleAsync(ProtectedLocalStorage localStorage)
    {
        var newPreference = IsDarkMode ? ThemePreference.Light : ThemePreference.Dark;
        await SetPreferenceAsync(newPreference, localStorage);
    }

    public void UpdateSystemPreference(bool isDark)
    {
        if (Preference == ThemePreference.System)
        {
            IsDarkMode = isDark;
            OnThemeChanged?.Invoke();
        }
    }
}
