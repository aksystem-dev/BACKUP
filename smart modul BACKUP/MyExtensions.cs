using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    public static class MyExtensions
    {
        /// <summary>
        /// Zvýší číslo na konci stringu o 1. Pokud tam číslo není, přidělá se tam 1.
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static string Increment(this string me)
        {
            int endNumberStartIndex = me.Length;
            for (int i = me.Length - 1; i >= 0; i--)
            {
                if (!Char.IsDigit(me[i]))
                {
                    endNumberStartIndex = i + 1;
                    break;
                }
            }

            string numString = me.Substring(endNumberStartIndex);
            int num = -1;
            int.TryParse(numString, out num);

            string cut = me.Substring(0, endNumberStartIndex);

            return cut + (num + 1).ToString();
        }

        /// <summary>
        /// Posune adresu o adresář nahoru.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string PathMoveUp(this string path)
        {
            string[] split = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string toReturn = "";
            int max = split.Length - 1;
            for (int i = 0; i < max; i++)
            {
                var curr = split[i];
                toReturn += curr;
                if (curr != "")
                    if (i < max - 1 || curr[curr.Length - 1] == ':')
                        toReturn += Path.DirectorySeparatorChar;
            }

            return toReturn;
        }

        public static string[] PathProgression(this string path)
        {
            List<string> paths = new List<string>();

            string newPath = path;
            do
            {
                path = newPath;
                paths.Add(path);
                newPath = path.PathMoveUp();
            }
            while (path.Length != newPath.Length);

            paths.Reverse();
            return paths.ToArray();
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T i in enumerable)
                action(i);
        }

        public static bool IsIn<T>(this T me, params T[] values)
            => values.Contains(me);
    }
}
