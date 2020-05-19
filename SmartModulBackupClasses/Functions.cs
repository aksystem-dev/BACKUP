using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Poskytuje funkce které vrací funkce které vrací funkce
    /// </summary>
    public static class Functions
    {
        /// <summary>
        /// Vrátí funkci f(x), která vrátí funkci g(y), která vrací bool; funkce g(y) závisí na parametru exclusive:
        ///     exclusive = true => g(y) = y > x |
        ///     exclusive = false => g(y) = g >= x
        /// </summary>
        /// <param name="exclusive"></param>
        /// <returns></returns>
        public static Func<IComparable, Func<IComparable, bool>> GetBiggerComparer(bool exclusive) =>
            exclusive ?
                new Func<IComparable, Func<IComparable, bool>>(span => new Func<IComparable, bool>(f => f.CompareTo(span) > 0)) :
                new Func<IComparable, Func<IComparable, bool>>(span => new Func<IComparable, bool>(f => f.CompareTo(span) >= 0));

        /// <summary>
        /// Vrátí funkci f(x), která vrátí funkci g(y), která vrací bool; funkce g(y) závisí na parametru exclusive:
        ///     exclusive = true => g(y) = y &lt; x |
        ///     exclusive = false => g(y) = y &lt;= x
        /// </summary>
        /// <param name="exclusive"></param>
        /// <returns></returns>
        public static Func<IComparable, Func<IComparable, bool>> GetSmallerComparer(bool exclusive) =>
            exclusive ?
                new Func<IComparable, Func<IComparable, bool>>(span => new Func<IComparable, bool>(f => f.CompareTo(span) < 0)) :
                new Func<IComparable, Func<IComparable, bool>>(span => new Func<IComparable, bool>(f => f.CompareTo(span) <= 0));
    }
}
