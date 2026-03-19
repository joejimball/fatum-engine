namespace FatumApp.Maui.Services;

/// <summary>
/// Servicio de idioma de la aplicación. Devuelve el texto traducido según el idioma seleccionado.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Código del idioma actual (es, en, zh, fr, pt).</summary>
    string CurrentLanguage { get; }

    /// <summary>Nombre del idioma actual para mostrar.</summary>
    string CurrentLanguageName { get; }

    /// <summary>Idiomas soportados: código -> nombre (ej. "es" -> "Español").</summary>
    IReadOnlyDictionary<string, string> SupportedLanguages { get; }

    /// <summary>Se dispara cuando el usuario cambia el idioma.</summary>
    event Action? LanguageChanged;

    /// <summary>Obtiene el texto para la clave dada en el idioma actual.</summary>
    string GetString(string key);

    /// <summary>Establece el idioma y persiste la preferencia.</summary>
    void SetLanguage(string languageCode);
}
