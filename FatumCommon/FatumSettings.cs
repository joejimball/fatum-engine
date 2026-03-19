using FatumCommon.Enums;

namespace FatumCommon
{
    public class FatumSettings : IFatumSettings
    {
        private readonly EnumTypeRnd _typeRnd;
        private readonly string _apiKey;
        private readonly string _sqliteDBPath;
        private readonly string _botToken;

        public FatumSettings(EnumTypeRnd _typeRnd, string _apiKey, string _sqliteDBPath, string botToken)
        {
            this._typeRnd = _typeRnd;
            this._apiKey = _apiKey;
            this._sqliteDBPath = _sqliteDBPath;
            _botToken = botToken;
        }

        public EnumTypeRnd TypeRnd => _typeRnd;

        public string ApiKey => _apiKey;

        public string SqliteDBPath => _sqliteDBPath;

        public string BotToken => _botToken;
    }
}
