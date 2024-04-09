using FMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil
{
    public class LocalisationExt
    {
        public const LocalisationEnums.StringBanks LocalisationExtID = (LocalisationEnums.StringBanks)int.MinValue;

        public static Dictionary<LocalisationEnums.StringBanks, LocExtCategory> LOCExt =
            new Dictionary<LocalisationEnums.StringBanks, LocExtCategory>();


        public static bool TryGetFrom(LocalisationEnums.StringBanks category, int ID, ref string result)
        {
            if (LOCExt.TryGetValue(category, out LocExtCategory LEC) && 
                LEC.bank.TryGetValue(ID, out string LES))
            {
                result = LES.ToString();
                return true;
            }
            return false;
        }

        public static void Register(LocalisationEnums.StringBanks category, int ID, string String)
        {
            if (!LOCExt.TryGetValue(category, out LocExtCategory LEC))
            {
                LEC = new LocExtCategory();
                LOCExt.Add(category, LEC);
            }
            if (!LEC.bank.ContainsKey(ID))
            {
                if (LEC.StartIndex > ID)
                {

                }
                LEC.bank.Add(ID, String);
            }
        }
        public static void ResetLookupList()
        {
            foreach (var item in LOCExt)
            {
                item.Value.bank.Clear();
            }
        }
    }
    public class LocExtCategory
    {
        public int StartIndex;
        public Dictionary<int, string> bank = new Dictionary<int, string>();
        public LocExtCategory()
        {
            StartIndex = 9001;
        }
        public LocExtCategory(Type enumType)
        {
            StartIndex = Enum.GetValues(enumType).Length;
        }
    }
    public class LocExtString
    {
        public LocExtString(string Data) { data = Data; }
        readonly string data;
        public override string ToString()
        {
            return data;
        }
        public void Register(LocalisationEnums.StringBanks category, int ID)
        {
            LocalisationExt.Register(category, ID, data);
        }
    }
}
