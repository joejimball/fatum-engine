using Android.App;
using Android.Content.PM;
using Android.OS;

namespace FatumApp.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
#if DEBUG
            // Depuración del WebView: en Chrome en el PC abre chrome://inspect para ver consola y errores JS
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        }
    }
}
