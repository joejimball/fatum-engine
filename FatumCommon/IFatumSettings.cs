
using FatumCommon.Enums;

namespace FatumCommon
{
    public interface IFatumSettings 
    {
        EnumTypeRnd TypeRnd { get; }
        string ApiKey { get; } // Reemplaza con tu API key real
        string BotToken { get; }
    }
}
