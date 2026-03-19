namespace FatumApp.Maui.Helps
{
    public static class UrlHelps
    {
        public static async Task OpenPlusCodeAsync(string? plusCode)
        {
            if (string.IsNullOrWhiteSpace(plusCode))
                return;

            var url = $"https://plus.codes/{Uri.EscapeDataString(plusCode)}";
            await OpenUrlSafeAsync(url);
        }

        public static async Task OpenGeoHashAsync(string? geoHash)
        {
            if (string.IsNullOrWhiteSpace(geoHash))
                return;

            var url = $"https://www.geohash.es/decode?geohash={Uri.EscapeDataString(geoHash)}";
            await OpenUrlSafeAsync(url);
        }

        public static async Task OpenWhat3WordsAsync(string? words)
        {
            if (string.IsNullOrWhiteSpace(words))
                return;

            // w3w.co redirige a la web y, si procede, al app.
            var url = $"https://w3w.co/{Uri.EscapeDataString(words)}";
            await OpenUrlSafeAsync(url);
        }

        private static async Task OpenUrlSafeAsync(string url)
        {
            await Launcher.Default.OpenAsync(url).ConfigureAwait(true);
        }
    }
}
