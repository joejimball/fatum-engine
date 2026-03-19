using System.Globalization;

namespace FatumApp.Maui.Services;

/// <summary>
/// Implementación de localización con 5 idiomas: Español, English, 中文, Français, Português.
/// Persiste el idioma en Preferences.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private const string PreferenceKey = "AppLanguage";
    private const string DefaultLanguage = "es";

    private static readonly IReadOnlyDictionary<string, string> LanguageNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = "Español",
        ["en"] = "English",
        ["zh"] = "中文",
        ["fr"] = "Français",
        ["pt"] = "Português"
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["es"] = new Dictionary<string, string>
        {
            ["Nav.Generate"] = "Generar",
            ["Nav.Settings"] = "Configuración",
            ["Nav.History"] = "Historial",
            ["Main.Title"] = "Fatum",
            ["Main.SearchType"] = "Tipo de búsqueda",
            ["Main.Attractor"] = "Atractor",
            ["Main.Void"] = "Vacío",
            ["Main.Power"] = "Poder",
            ["Main.Radius"] = "Radio",
            ["Main.CenterMyLocation"] = "Centrar en mi ubicación",
            ["Main.PickOnMap"] = "Elegir en el mapa",
            ["Main.PickOnMapTitle"] = "Elegir un punto en el mapa (útil si el GPS falla)",
            ["Main.Generate"] = "Generar",
            ["Main.Generating"] = "Generando...",
            ["Main.OpenInGoogleMaps"] = "Abrir en Google Maps",
            ["Main.ViewHistory"] = "Ver historial",
            ["Main.Coordinates"] = "Coordenadas",
            ["Main.Type"] = "Tipo",
            ["Main.Result"] = "Resultado",
            ["History.Title"] = "Historial",
            ["History.BackToGenerate"] = "← Generar",
            ["History.ExportJson"] = "Exportar JSON",
            ["History.ExportCsv"] = "Exportar CSV",
            ["History.ImportJson"] = "Importar JSON",
            ["History.DeleteAll"] = "Eliminar todo",
            ["History.Loading"] = "Cargando...",
            ["History.NoItems"] = "No hay fatums guardados.",
            ["History.ViewOnMap"] = "Ver en mapa",
            ["History.Heatmap"] = "Ver mapa de calor KDE",
            ["History.Delete"] = "Eliminar",
            ["MapPicker.Title"] = "Elegir en el mapa",
            ["MapPicker.Instructions"] = "Toca un punto en el mapa para elegir la ubicación. Luego pulsa \"Usar esta ubicación\".",
            ["MapPicker.UseLocation"] = "Usar esta ubicación",
            ["MapPicker.Cancel"] = "Cancelar",
            ["MapPicker.TapToMark"] = "Toca el mapa para marcar un punto.",
            ["Heatmap.Title"] = "Mapa de calor KDE",
            ["Heatmap.BackToHistory"] = "← Volver al historial",
            ["Heatmap.ShowPoints"] = "Mostrar puntos aleatorios en el mapa",
            ["Heatmap.IntensityScale"] = "Escala de intensidad",
            ["Heatmap.VeryLow"] = "Muy baja",
            ["Heatmap.Low"] = "Baja",
            ["Heatmap.Medium"] = "Media",
            ["Heatmap.High"] = "Alta",
            ["Heatmap.VeryHigh"] = "Muy alta",
            ["Heatmap.Loading"] = "Cargando...",
            ["Settings.Language"] = "Idioma de la aplicación",
            ["Settings.RngTitle"] = "Generador de números aleatorios",
            ["Settings.RngDescription"] = "PRNG usa la clase Random de C#. QRNG usa ANU y requiere API Key.",
            ["Settings.Generator"] = "Generador",
            ["Settings.PRNG"] = "PRNG (Random C#)",
            ["Settings.QRNG"] = "QRNG (ANU)",
            ["Settings.ApiKeyLabel"] = "API Key ANU",
            ["Settings.ApiKeyPlaceholder"] = "Opcional si ya está guardada",
            ["Settings.SaveValidateKey"] = "Guardar y validar API Key",
            ["Settings.DeleteKey"] = "Eliminar API Key",
            ["Settings.KeyStored"] = "✔ API Key almacenada correctamente",
            ["Settings.SaveGenerator"] = "Guardar generador seleccionado",
            ["NotFound.Message"] = "Página no encontrada.",
            ["NotFound.GoHome"] = "Ir al inicio",
        },
        ["en"] = new Dictionary<string, string>
        {
            ["Nav.Generate"] = "Generate",
            ["Nav.Settings"] = "Settings",
            ["Nav.History"] = "History",
            ["Main.Title"] = "Fatum",
            ["Main.SearchType"] = "Search type",
            ["Main.Attractor"] = "Attractor",
            ["Main.Void"] = "Void",
            ["Main.Power"] = "Power",
            ["Main.Radius"] = "Radius",
            ["Main.CenterMyLocation"] = "Center on my location",
            ["Main.PickOnMap"] = "Pick on map",
            ["Main.PickOnMapTitle"] = "Pick a point on the map (useful if GPS fails)",
            ["Main.Generate"] = "Generate",
            ["Main.Generating"] = "Generating...",
            ["Main.OpenInGoogleMaps"] = "Open in Google Maps",
            ["Main.ViewHistory"] = "View history",
            ["Main.Coordinates"] = "Coordinates",
            ["Main.Type"] = "Type",
            ["Main.Result"] = "Result",
            ["History.Title"] = "History",
            ["History.BackToGenerate"] = "← Generate",
            ["History.ExportJson"] = "Export JSON",
            ["History.ExportCsv"] = "Export CSV",
            ["History.ImportJson"] = "Import JSON",
            ["History.DeleteAll"] = "Delete all",
            ["History.Loading"] = "Loading...",
            ["History.NoItems"] = "No saved fatums.",
            ["History.ViewOnMap"] = "View on map",
            ["History.Heatmap"] = "View KDE heatmap",
            ["History.Delete"] = "Delete",
            ["MapPicker.Title"] = "Pick on map",
            ["MapPicker.Instructions"] = "Tap a point on the map to choose the location. Then tap \"Use this location\".",
            ["MapPicker.UseLocation"] = "Use this location",
            ["MapPicker.Cancel"] = "Cancel",
            ["MapPicker.TapToMark"] = "Tap the map to mark a point.",
            ["Heatmap.Title"] = "KDE heatmap",
            ["Heatmap.BackToHistory"] = "← Back to history",
            ["Heatmap.ShowPoints"] = "Show random points on map",
            ["Heatmap.IntensityScale"] = "Intensity scale",
            ["Heatmap.VeryLow"] = "Very low",
            ["Heatmap.Low"] = "Low",
            ["Heatmap.Medium"] = "Medium",
            ["Heatmap.High"] = "High",
            ["Heatmap.VeryHigh"] = "Very high",
            ["Heatmap.Loading"] = "Loading...",
            ["Settings.Language"] = "Application language",
            ["Settings.RngTitle"] = "Random number generator",
            ["Settings.RngDescription"] = "PRNG uses C# Random. QRNG uses ANU and requires API Key.",
            ["Settings.Generator"] = "Generator",
            ["Settings.PRNG"] = "PRNG (C# Random)",
            ["Settings.QRNG"] = "QRNG (ANU)",
            ["Settings.ApiKeyLabel"] = "ANU API Key",
            ["Settings.ApiKeyPlaceholder"] = "Optional if already saved",
            ["Settings.SaveValidateKey"] = "Save and validate API Key",
            ["Settings.DeleteKey"] = "Delete API Key",
            ["Settings.KeyStored"] = "✔ API Key stored successfully",
            ["Settings.SaveGenerator"] = "Save selected generator",
            ["NotFound.Message"] = "Page not found.",
            ["NotFound.GoHome"] = "Go to home",
        },
        ["zh"] = new Dictionary<string, string>
        {
            ["Nav.Generate"] = "生成",
            ["Nav.Settings"] = "设置",
            ["Nav.History"] = "历史",
            ["Main.Title"] = "Fatum",
            ["Main.SearchType"] = "搜索类型",
            ["Main.Attractor"] = "吸引子",
            ["Main.Void"] = "虚空",
            ["Main.Power"] = "功率",
            ["Main.Radius"] = "半径",
            ["Main.CenterMyLocation"] = "定位到我的位置",
            ["Main.PickOnMap"] = "在地图上选择",
            ["Main.PickOnMapTitle"] = "在地图上选点（GPS 失效时可用）",
            ["Main.Generate"] = "生成",
            ["Main.Generating"] = "生成中...",
            ["Main.OpenInGoogleMaps"] = "在 Google 地图中打开",
            ["Main.ViewHistory"] = "查看历史",
            ["Main.Coordinates"] = "坐标",
            ["Main.Type"] = "类型",
            ["Main.Result"] = "结果",
            ["History.Title"] = "历史",
            ["History.BackToGenerate"] = "← 生成",
            ["History.ExportJson"] = "导出 JSON",
            ["History.ExportCsv"] = "导出 CSV",
            ["History.ImportJson"] = "导入 JSON",
            ["History.DeleteAll"] = "全部删除",
            ["History.Loading"] = "加载中...",
            ["History.NoItems"] = "暂无记录。",
            ["History.ViewOnMap"] = "在地图上查看",
            ["History.Heatmap"] = "查看 KDE 热力图",
            ["History.Delete"] = "删除",
            ["MapPicker.Title"] = "在地图上选择",
            ["MapPicker.Instructions"] = "在地图上点击选择位置，然后点击「使用此位置」。",
            ["MapPicker.UseLocation"] = "使用此位置",
            ["MapPicker.Cancel"] = "取消",
            ["MapPicker.TapToMark"] = "点击地图标记点位。",
            ["Heatmap.Title"] = "KDE 热力图",
            ["Heatmap.BackToHistory"] = "← 返回历史",
            ["Heatmap.ShowPoints"] = "在地图上显示随机点",
            ["Heatmap.IntensityScale"] = "强度刻度",
            ["Heatmap.VeryLow"] = "很低",
            ["Heatmap.Low"] = "低",
            ["Heatmap.Medium"] = "中",
            ["Heatmap.High"] = "高",
            ["Heatmap.VeryHigh"] = "很高",
            ["Heatmap.Loading"] = "加载中...",
            ["Settings.Language"] = "应用语言",
            ["Settings.RngTitle"] = "随机数生成器",
            ["Settings.RngDescription"] = "PRNG 使用 C# Random，QRNG 使用 ANU 并需要 API Key。",
            ["Settings.Generator"] = "生成器",
            ["Settings.PRNG"] = "PRNG (C# Random)",
            ["Settings.QRNG"] = "QRNG (ANU)",
            ["Settings.ApiKeyLabel"] = "ANU API Key",
            ["Settings.ApiKeyPlaceholder"] = "已保存则可选",
            ["Settings.SaveValidateKey"] = "保存并验证 API Key",
            ["Settings.DeleteKey"] = "删除 API Key",
            ["Settings.KeyStored"] = "✔ API Key 已保存",
            ["Settings.SaveGenerator"] = "保存所选生成器",
            ["NotFound.Message"] = "页面未找到。",
            ["NotFound.GoHome"] = "返回首页",
        },
        ["fr"] = new Dictionary<string, string>
        {
            ["Nav.Generate"] = "Générer",
            ["Nav.Settings"] = "Paramètres",
            ["Nav.History"] = "Historique",
            ["Main.Title"] = "Fatum",
            ["Main.SearchType"] = "Type de recherche",
            ["Main.Attractor"] = "Attracteur",
            ["Main.Void"] = "Vide",
            ["Main.Power"] = "Puissance",
            ["Main.Radius"] = "Rayon",
            ["Main.CenterMyLocation"] = "Centrer sur ma position",
            ["Main.PickOnMap"] = "Choisir sur la carte",
            ["Main.PickOnMapTitle"] = "Choisir un point sur la carte (si le GPS échoue)",
            ["Main.Generate"] = "Générer",
            ["Main.Generating"] = "Génération...",
            ["Main.OpenInGoogleMaps"] = "Ouvrir dans Google Maps",
            ["Main.ViewHistory"] = "Voir l'historique",
            ["Main.Coordinates"] = "Coordonnées",
            ["Main.Type"] = "Type",
            ["Main.Result"] = "Résultat",
            ["History.Title"] = "Historique",
            ["History.BackToGenerate"] = "← Générer",
            ["History.ExportJson"] = "Exporter JSON",
            ["History.ExportCsv"] = "Exporter CSV",
            ["History.ImportJson"] = "Importer JSON",
            ["History.DeleteAll"] = "Tout supprimer",
            ["History.Loading"] = "Chargement...",
            ["History.NoItems"] = "Aucun fatum enregistré.",
            ["History.ViewOnMap"] = "Voir sur la carte",
            ["History.Heatmap"] = "Voir la carte thermique KDE",
            ["History.Delete"] = "Supprimer",
            ["MapPicker.Title"] = "Choisir sur la carte",
            ["MapPicker.Instructions"] = "Appuyez sur un point de la carte pour choisir l'emplacement, puis « Utiliser cet emplacement ».",
            ["MapPicker.UseLocation"] = "Utiliser cet emplacement",
            ["MapPicker.Cancel"] = "Annuler",
            ["MapPicker.TapToMark"] = "Appuyez sur la carte pour marquer un point.",
            ["Heatmap.Title"] = "Carte thermique KDE",
            ["Heatmap.BackToHistory"] = "← Retour à l'historique",
            ["Heatmap.ShowPoints"] = "Afficher les points aléatoires sur la carte",
            ["Heatmap.IntensityScale"] = "Échelle d'intensité",
            ["Heatmap.VeryLow"] = "Très faible",
            ["Heatmap.Low"] = "Faible",
            ["Heatmap.Medium"] = "Moyen",
            ["Heatmap.High"] = "Élevé",
            ["Heatmap.VeryHigh"] = "Très élevé",
            ["Heatmap.Loading"] = "Chargement...",
            ["Settings.Language"] = "Langue de l'application",
            ["Settings.RngTitle"] = "Générateur de nombres aléatoires",
            ["Settings.RngDescription"] = "PRNG utilise Random de C#. QRNG utilise ANU et nécessite une clé API.",
            ["Settings.Generator"] = "Générateur",
            ["Settings.PRNG"] = "PRNG (Random C#)",
            ["Settings.QRNG"] = "QRNG (ANU)",
            ["Settings.ApiKeyLabel"] = "Clé API ANU",
            ["Settings.ApiKeyPlaceholder"] = "Optionnel si déjà enregistrée",
            ["Settings.SaveValidateKey"] = "Enregistrer et valider la clé API",
            ["Settings.DeleteKey"] = "Supprimer la clé API",
            ["Settings.KeyStored"] = "✔ Clé API enregistrée",
            ["Settings.SaveGenerator"] = "Enregistrer le générateur sélectionné",
            ["NotFound.Message"] = "Page non trouvée.",
            ["NotFound.GoHome"] = "Retour à l'accueil",
        },
        ["pt"] = new Dictionary<string, string>
        {
            ["Nav.Generate"] = "Gerar",
            ["Nav.Settings"] = "Configurações",
            ["Nav.History"] = "Histórico",
            ["Main.Title"] = "Fatum",
            ["Main.SearchType"] = "Tipo de busca",
            ["Main.Attractor"] = "Atractor",
            ["Main.Void"] = "Vazio",
            ["Main.Power"] = "Potência",
            ["Main.Radius"] = "Raio",
            ["Main.CenterMyLocation"] = "Centralizar na minha localização",
            ["Main.PickOnMap"] = "Escolher no mapa",
            ["Main.PickOnMapTitle"] = "Escolher um ponto no mapa (útil se o GPS falhar)",
            ["Main.Generate"] = "Gerar",
            ["Main.Generating"] = "Gerando...",
            ["Main.OpenInGoogleMaps"] = "Abrir no Google Maps",
            ["Main.ViewHistory"] = "Ver histórico",
            ["Main.Coordinates"] = "Coordenadas",
            ["Main.Type"] = "Tipo",
            ["Main.Result"] = "Resultado",
            ["History.Title"] = "Histórico",
            ["History.BackToGenerate"] = "← Gerar",
            ["History.ExportJson"] = "Exportar JSON",
            ["History.ExportCsv"] = "Exportar CSV",
            ["History.ImportJson"] = "Importar JSON",
            ["History.DeleteAll"] = "Excluir tudo",
            ["History.Loading"] = "Carregando...",
            ["History.NoItems"] = "Nenhum fatum guardado.",
            ["History.ViewOnMap"] = "Ver no mapa",
            ["History.Heatmap"] = "Ver mapa de calor KDE",
            ["History.Delete"] = "Excluir",
            ["MapPicker.Title"] = "Escolher no mapa",
            ["MapPicker.Instructions"] = "Toque num ponto do mapa para escolher a localização. Depois toque em \"Usar esta localização\".",
            ["MapPicker.UseLocation"] = "Usar esta localização",
            ["MapPicker.Cancel"] = "Cancelar",
            ["MapPicker.TapToMark"] = "Toque no mapa para marcar um ponto.",
            ["Heatmap.Title"] = "Mapa de calor KDE",
            ["Heatmap.BackToHistory"] = "← Voltar ao histórico",
            ["Heatmap.ShowPoints"] = "Mostrar pontos aleatórios no mapa",
            ["Heatmap.IntensityScale"] = "Escala de intensidade",
            ["Heatmap.VeryLow"] = "Muito baixa",
            ["Heatmap.Low"] = "Baixa",
            ["Heatmap.Medium"] = "Média",
            ["Heatmap.High"] = "Alta",
            ["Heatmap.VeryHigh"] = "Muito alta",
            ["Heatmap.Loading"] = "Carregando...",
            ["Settings.Language"] = "Idioma da aplicação",
            ["Settings.RngTitle"] = "Gerador de números aleatórios",
            ["Settings.RngDescription"] = "PRNG usa Random do C#. QRNG usa ANU e requer API Key.",
            ["Settings.Generator"] = "Gerador",
            ["Settings.PRNG"] = "PRNG (Random C#)",
            ["Settings.QRNG"] = "QRNG (ANU)",
            ["Settings.ApiKeyLabel"] = "API Key ANU",
            ["Settings.ApiKeyPlaceholder"] = "Opcional se já guardada",
            ["Settings.SaveValidateKey"] = "Guardar e validar API Key",
            ["Settings.DeleteKey"] = "Eliminar API Key",
            ["Settings.KeyStored"] = "✔ API Key guardada com sucesso",
            ["Settings.SaveGenerator"] = "Guardar gerador seleccionado",
            ["NotFound.Message"] = "Página não encontrada.",
            ["NotFound.GoHome"] = "Ir ao início",
        },
    };

    private string _currentLanguage;

    public LocalizationService()
    {
        _currentLanguage = LoadSavedLanguage();
        ApplyCulture(_currentLanguage);
    }

    public string CurrentLanguage => _currentLanguage;
    public string CurrentLanguageName => LanguageNames.GetValueOrDefault(_currentLanguage, _currentLanguage);
    public IReadOnlyDictionary<string, string> SupportedLanguages => LanguageNames;
    public event Action? LanguageChanged;

    public string GetString(string key)
    {
        if (Translations.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        if (Translations.TryGetValue(DefaultLanguage, out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
            return fallbackValue;
        return key;
    }

    public void SetLanguage(string languageCode)
    {
        if (!LanguageNames.ContainsKey(languageCode))
            return;
        _currentLanguage = languageCode;
        ApplyCulture(languageCode);
        Preferences.Default.Set(PreferenceKey, languageCode);
        LanguageChanged?.Invoke();
    }

    private static string LoadSavedLanguage()
    {
        var saved = Preferences.Default.Get(PreferenceKey, DefaultLanguage);
        return LanguageNames.ContainsKey(saved) ? saved : DefaultLanguage;
    }

    private static void ApplyCulture(string languageCode)
    {
        try
        {
            var culture = languageCode switch
            {
                "es" => new CultureInfo("es"),
                "en" => new CultureInfo("en"),
                "zh" => new CultureInfo("zh-Hans"),
                "fr" => new CultureInfo("fr"),
                "pt" => new CultureInfo("pt"),
                _ => CultureInfo.CurrentCulture
            };
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch
        {
            // Ignore if culture not available
        }
    }
}
