using FatumCommon.Enums;

namespace FatumApp.Maui.Services;

public interface IQuantumSettingsService 
{
    Task<string?> GetApiKeyAsync();

    Task SetApiKeyAsync(string key);

    Task<string?> GetWhat3WordsApiKeyAsync();

    Task SetWhat3WordsApiKeyAsync(string key);

    Task<EnumTypeRnd> GetTypeRnd();

    Task SetTypeRnd(string type);

    void RemoveApiKey();

    void RemoveWhat3WordsApiKey();
}
