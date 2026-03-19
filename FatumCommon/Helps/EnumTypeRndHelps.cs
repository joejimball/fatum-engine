using FatumCommon.Enums;
using System;
using System.Linq;

namespace FatumCommon.Helps
{
    public static class EnumTypeRndHelps
    {
        public static EnumTypeRnd Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return EnumTypeRnd.PRNG;
            string tmp = name.Trim().ToUpper();
            var query = Enum.GetValues(typeof(EnumTypeRnd)).Cast<EnumTypeRnd>();
            var result = query.SingleOrDefault(x => x.ToString().Contains(tmp));
            return result;
        }
    }
}
