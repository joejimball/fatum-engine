using System.Globalization;

namespace FatumApp.Maui;

public partial class App : Application
{
    public App()
    {
        ApplySavedLanguage();
        InitializeComponent();
        MainPage = new MainPage();
    }

    private static void ApplySavedLanguage()
    {
        var code = Preferences.Default.Get("AppLanguage", "es");
        var culture = code switch
        {
            "es" => new CultureInfo("es"),
            "en" => new CultureInfo("en"),
            "zh" => new CultureInfo("zh-Hans"),
            "fr" => new CultureInfo("fr"),
            "pt" => new CultureInfo("pt"),
            _ => new CultureInfo("es")
        };
        try
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch { /* ignore */ }
    }
}
