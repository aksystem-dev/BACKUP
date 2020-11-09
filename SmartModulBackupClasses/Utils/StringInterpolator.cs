using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Utils
{
    /// <summary>
    /// Slouží k vložení parametrů do řetězců. Parametry lze do vstupního řetězce vložit
    /// takto: ${název parametru}
    /// </summary>
    public class StringInterpolator
    {
        private readonly string _str;

        private string[] _strings; //části řetězce, které nejsou parametry
        private StringInterpolatorParameter[] _params; //části řetězce, které jsou parametry       

        /// <summary>
        /// Seznam všech parametrů.
        /// </summary>
        public ReadOnlyCollection<StringInterpolatorParameter> Params =>
            new ReadOnlyCollection<StringInterpolatorParameter>(_params.Distinct().ToList());

        public const char ESCAPE_CHAR = '\\';
        public const char INTERPOLATION_CHAR = '$';
        public const char OPENING_BRACKET = '{';
        public const char CLOSING_BRACKET = '}';

        public StringInterpolator(string str)
        {
            this._str = str;
            initParams();
        }

        /// <summary>
        /// Inicializuje _strings a _keys.
        /// </summary>
        private void initParams()
        {
            string buffer = "";
            List<string> strings = new List<string>();
            List<string> @params = new List<string>();
            for (int i = 0; i < _str.Length; i++)
            {
                var ch = _str[i];
                switch (ch)
                {
                    case INTERPOLATION_CHAR:
                        int memorize_i = i;
                        i += 1;
                        var keystr = getParam(ref i);
                        if (keystr != null)
                        {
                            strings.Add(buffer);
                            buffer = "";
                            @params.Add(keystr);
                        }
                        else
                            buffer += _str.Substring(memorize_i, i - memorize_i);
                        break;
                    case ESCAPE_CHAR:
                        switch (_str[i + 1])
                        {
                            case INTERPOLATION_CHAR:
                                i += 1;
                                buffer += INTERPOLATION_CHAR;
                                break;
                            default:
                                buffer += ESCAPE_CHAR;
                                break;
                        }
                        break;
                    default:
                        buffer += ch;
                        break;
                }
            }

            if (buffer != "")
                strings.Add(buffer);

            _strings = strings.ToArray();
            _params = paramsFromStrings(@params).ToArray();
        }

        /// <summary>
        /// vrátí název parametru, začínaje na i. Pakliže se ukáže, že parametr je zadán nesprávně,
        /// vrátí null.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private string getParam(ref int i)
        {
            int depth = 0;
            string buffer = "";
            for (; i < _str.Length; i++)
            {
                if (_str[i] == OPENING_BRACKET)
                    depth++;
                else if (_str[i] == CLOSING_BRACKET)
                    depth--;

                buffer += _str[i];
                if (depth == 0)
                    break;
            }

            if (buffer.Length <= 2 || depth != 0)
                return null;

            return buffer.Substring(1, buffer.Length - 2);
        }

        /// <summary>
        /// vytvoří pole insancí StringInterpolatorParameter z řady řetězců názvů parametrů.
        /// pokud se jeden název objeví vícekrát, ve výsledném poli bude pro každý výskyt
        /// sdílená insance StringInterpolatorParameter.
        /// </summary>
        /// <param name="paramNames"></param>
        /// <returns></returns>
        private StringInterpolatorParameter[] paramsFromStrings(IEnumerable<string> paramNames)
        {
            List<StringInterpolatorParameter> parameters = new List<StringInterpolatorParameter>();
            foreach (var key in paramNames)
            {
                var existing = parameters.FirstOrDefault(p => p.key == key);
                parameters.Add(existing ?? new StringInterpolatorParameter(key));
            }

            return parameters.ToArray();
        }

        /// <summary>
        /// Nastaví hodnotu daného parametru a vrátí true.
        /// Pokud daný parametr nezná, vrátí false.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Set(string key, string value)
        {
            var param = _params.FirstOrDefault(p => p.key == key);

            if (param == null)
                return false;

            param.value = value;
            return true;
        }

        /// <summary>
        /// Vrátí řetězec s vloženými hodnotami parametrů.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //řetězec se poskládá takto:
            //_strings[0] + _keys[0] + _strings[1] + _keys[1] + _strings[2] + ...

            StringBuilder builder = new StringBuilder();
            for (int i = 0; ; i++)
            {
                builder.Append(_strings[i]);

                if (_params.Length > i)
                    builder.Append(_params[i].value ?? _params[i].Raw);
                else
                    break;
            }

            return builder.ToString();
        }

    }

    public class StringInterpolatorParameter
    {
        public StringInterpolatorParameter(string key)
        {
            this.key = key;
        }

        /// <summary>
        /// Název parametru.
        /// </summary>
        public readonly string key;

        /// <summary>
        /// Hodnota parametru. Pokud null, použije se Raw.
        /// </summary>
        public string value;

        public string Raw => $"{StringInterpolator.INTERPOLATION_CHAR}{StringInterpolator.OPENING_BRACKET}{key}{StringInterpolator.CLOSING_BRACKET}";
    }
}
