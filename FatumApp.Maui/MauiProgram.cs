using FatumApp.Maui.Infrastructure.Data;
using FatumApp.Maui.Services;
using FatumCommon;
using FatumCommon.Domain;
using Microsoft.Extensions.Logging;
using FatumCommon.Infrastructure.Data;
using FatumCommon.Services;



#if ANDROID
using Android.Views;
using Android.Webkit;
using Microsoft.Maui.Handlers;
#endif

namespace FatumApp.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if ANDROID
        // Configuración del WebView para evitar pantalla en blanco en Android (BlazorWebView usa WebView internamente)
        WebViewHandler.Mapper.AppendToMapping("AndroidWebViewSettings", (handler, _) =>
        {
            if (handler.PlatformView is Android.Webkit.WebView wv)
            {
                wv.SetLayerType(LayerType.Hardware, null);
                wv.Settings.JavaScriptEnabled = true;
                wv.Settings.DomStorageEnabled = true;
                wv.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            }
        });
#endif

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // No se registra el control nativo Map: los mapas se usan en Blazor (Leaflet) en todas las plataformas.
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // FatumCommon: calculadora KDE (sin reescribir lógica)
        builder.Services.AddScoped<IKdeCalculator, KdeCalculator>();

        // Base de datos SQLite
        builder.Services.AddSingleton<ISQLiteConnectionFactory, MauiSQLiteConnectionFactory>();
        builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        builder.Services.AddScoped<IDapperContext, SqliteDapperContext>();
        builder.Services.AddScoped<IUnitOfWork, SqliteDapperUnitOfWork>();

        // Servicios de aplicación
        builder.Services.AddScoped<IFatumGeneratorService, FatumGeneratorService>();
        builder.Services.AddScoped<IExportService, ExportService>();
        builder.Services.AddScoped<IMapPickerService, MapPickerService>();
        builder.Services.AddScoped<IQuantumSettingsService, QuantumSettingsService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

        // Program.cs o Startup.cs
        builder.Services.AddHttpClient("QuantumRandom", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddHttpClient("What3Words", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddScoped<IRandomBytesProvider, QuantumRandomBytesProvider>();
        return builder.Build();
    }
}
