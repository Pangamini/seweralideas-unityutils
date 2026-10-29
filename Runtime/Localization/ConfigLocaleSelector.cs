#nullable enable
using System;
using SeweralIdeas.Config;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SeweralIdeas.LocalizationExtensions
{
    /// <summary>
    /// Keeps the selected locale and a <see cref="StringConfigField"/> holding its code in sync. As a startup locale selector
    /// it picks the locale stored in the field, so there is no flash of the system language. Once the game runs, changes of the
    /// field switch the locale, and changes of the locale are written back to the field.
    /// Add it to the Startup Locale Selectors in the Localization settings, before the System Locale Selector.
    /// </summary>
    [Serializable]
    public class ConfigLocaleSelector : IStartupLocaleSelector
    {
        [SerializeField] private StringConfigField _localeCode = null!;

        public Locale? GetStartupLocale(ILocalesProvider availableLocales)
        {
            if(_localeCode == null)
                return null;

            // Called on each initialization (and the selector outlives it), so unhook first to never subscribe twice.
            _localeCode.ValueChanged -= OnConfigChanged;
            _localeCode.ValueChanged += OnConfigChanged;
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

            return FindLocale(availableLocales, _localeCode.Value);
        }

        private static Locale? FindLocale(ILocalesProvider availableLocales, string? code)
        {
            return string.IsNullOrEmpty(code) ? null : availableLocales.GetLocale(new LocaleIdentifier(code));
        }

        private void OnConfigChanged(string code)
        {
            // Before the initialization is done the startup selection is still to come, and it reads the field itself.
            if(!LocalizationSettings.InitializationOperation.IsDone)
                return;

            Locale? locale = FindLocale(LocalizationSettings.AvailableLocales, code);
            if(locale != null && locale != LocalizationSettings.SelectedLocale)
                LocalizationSettings.SelectedLocale = locale;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            if(locale != null)
                _localeCode.Value = locale.Identifier.Code; // no-op when equal, which ends the round trip
        }
    }
}
