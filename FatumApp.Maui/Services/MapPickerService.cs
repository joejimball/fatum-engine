namespace FatumApp.Maui.Services;

public sealed class MapPickerService : IMapPickerService
{
    public async Task<(double Latitude, double Longitude)?> PickLocationAsync(
        double initialLatitude,
        double initialLongitude,
        double radiusKm = 2)
    {
        var tcs = new TaskCompletionSource<(double, double)?>();

        var page = new Pages.MapPickerPage(
            initialLatitude,
            initialLongitude,
            radiusKm,
            async (lat, lon) =>
            {
                tcs.TrySetResult((lat, lon));

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.MainPage != null)
                        await Application.Current.MainPage.Navigation.PopModalAsync();
                });
            },
            async () =>
            {
                tcs.TrySetResult(null);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Application.Current?.MainPage != null)
                        await Application.Current.MainPage.Navigation.PopModalAsync();
                });
            });

        if (Application.Current?.MainPage != null)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(page);
            });
        }
        else
        {
            return null;
        }

        return await tcs.Task; // sin ConfigureAwait(false)
    }
}
