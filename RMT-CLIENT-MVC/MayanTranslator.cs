using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMT_CLIENT_MVC
{
    public static class MayanTranslator
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            // 
            { "tzol", "c" },
            { "kin", "m" },
            { "baktun", "d" },
            { "pop", "." },
            { "tun", "e" },
            { "katun", "x" },
            { "haab", "e" },

            // 
            { "kab", "/c" },
            { "shu", "shutdown" },
            { "sal", "/s" },
            { "niil", "/r" },
            { "luum", "/l" },
            { "chan", "/t" },
            { "hun", "0" },
            { "nah", "/f" },

            {"áalkab ","run32" },
            {"libroʼob", "dll" },
            {"máakÓoxp'éelyéetelka'ap'éel", "user32" }, // Persona = user = [máak]  32 = tres y dos = []
            {"jantej", "," }, // coma = [jantej]
            {"privarlemeyaj", "LockWorkStation"}
        };

        public static string Decode(string mayanText)
        {
            var parts = mayanText.Split('-');
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (Map.TryGetValue(part, out string value))
                {
                    sb.Append(value);
                }
            }
            return sb.ToString();
        }
    }
}
