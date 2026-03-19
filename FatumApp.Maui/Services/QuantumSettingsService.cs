using FatumCommon.Enums;

namespace FatumApp.Maui.Services;

public class QuantumSettingsService : IQuantumSettingsService
{
    private const EnumTypeRnd DefaultRngType = EnumTypeRnd.PRNG;
    private const string What3WordsApiKeyStorageKey = "W3W_API_KEY";

    public async Task<string?> GetApiKeyAsync()
    {
        var key = await SecureStorage.Default.GetAsync("ANU_API_KEY");
        return key ?? string.Empty;
    }

    public async Task SetApiKeyAsync(string key)
        => await SecureStorage.Default.SetAsync("ANU_API_KEY", key ?? string.Empty);

    public void RemoveApiKey()
        => SecureStorage.Default.Remove("ANU_API_KEY");

    public async Task<string?> GetWhat3WordsApiKeyAsync()
    {
        var key = await SecureStorage.Default.GetAsync(What3WordsApiKeyStorageKey);
        return key ?? string.Empty;
    }

    public async Task SetWhat3WordsApiKeyAsync(string key)
        => await SecureStorage.Default.SetAsync(What3WordsApiKeyStorageKey, key ?? string.Empty);

    public void RemoveWhat3WordsApiKey()
        => SecureStorage.Default.Remove(What3WordsApiKeyStorageKey);

    public async Task<EnumTypeRnd> GetTypeRnd()
    {
        string? rngTypeStr = await SecureStorage.Default.GetAsync("RNG_TYPE");
        if (string.IsNullOrEmpty(rngTypeStr))
        {
            await SecureStorage.Default.SetAsync("RNG_TYPE", DefaultRngType.ToString());
            return EnumTypeRnd.PRNG;
        }
        return (EnumTypeRnd)Enum.Parse(typeof(EnumTypeRnd), rngTypeStr);
    }

    public async Task SetTypeRnd(string type)
    {
        await SecureStorage.Default.SetAsync("RNG_TYPE", type ?? DefaultRngType.ToString());
    }
}
