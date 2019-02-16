using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicHash
{
    public static class Routines
    {
        public static string ListToString(this List<int> list, char separator)
        {
            var text = "";
            foreach (var item in list)
            {
                text += item.ToString() + ";";
            }
            if (text.Length > 0)
                text = text.Substring(0, text.Length - 1);
            return text;
        }

        public static void StringToList(this List<int> list, char separator, string data)
        {
            var array = data.Split(';');
            foreach (var s in array)
            {
                list.Add(Int32.Parse(s));
            }
        }
    }
}
