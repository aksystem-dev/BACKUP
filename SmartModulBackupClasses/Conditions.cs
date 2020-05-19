using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Sada informací o tom, kdy se má pravidlo spustit.
    /// </summary>
    public class Conditions : INotifyPropertyChanged
    {
        [XmlIgnore]
        public MinMaxInt[] _month = { };
        [XmlIgnore]
        public MinMaxInt[] _dayInMonth = { };
        [XmlIgnore]
        public MinMaxInt[] _dayInYear = { };
        [XmlIgnore]
        public MinMaxInt[] _dayInWeek = { };
        [XmlIgnore]
        public MinMaxTime[] _time = { };

        public event PropertyChangedEventHandler PropertyChanged;

        private string _monthString = "";
        [DefaultValue("")]
        public string Month
        {
            get => _monthString;
            set
            {
                _monthString = value;
                try
                {
                    _month = MinMaxInt.ArrayFromString(value).ToArray();
                    MonthValid = true;
                }
                catch
                {
                    MonthValid = false;
                    _month = new MinMaxInt[] { };
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Month)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonthValid)));
            }
        }

        private string _dayInMonthString;
        [DefaultValue("")]
        public string DayInMonth
        {
            get => _dayInMonthString;
            set
            {
                _dayInMonthString = value;
                try
                {
                    _dayInMonth = MinMaxInt.ArrayFromString(value).ToArray();
                    DayInMonthValid = true;
                }
                catch
                {
                    DayInMonthValid = false;
                    _dayInMonth = new MinMaxInt[] { };
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DayInMonth)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DayInMonthValid)));
            }
        }

        private string _dayInYearString;
        [DefaultValue("")]
        public string DayInYear
        {
            get => _dayInYearString;
            set
            {
                _dayInYearString = value;
                try
                {
                    _dayInYear = MinMaxInt.ArrayFromString(value).ToArray();
                    DayInYearValid = true;
                }
                catch
                {
                    DayInYearValid = false;
                    _dayInYear = new MinMaxInt[] { };
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DayInYear)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DayInYearValid)));
            }
        }

        private string _dayInWeekString;
        [DefaultValue("")]
        public string DayInWeek
        {
            get => _dayInWeekString;
            set
            {
                _dayInWeekString = value;
                try
                {
                    _dayInWeek = MinMaxInt.ArrayFromString(value).ToArray();
                    DayInWeekValid = true;
                }
                catch
                {
                    DayInWeekValid = false;
                    _dayInWeek = new MinMaxInt[] { };
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DayInWeek)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DayInWeekValid)));
            }
        }

        private string _timeString = "12:00";
        [DefaultValue("")]
        public string Time
        {
            get => _timeString;
            set
            {
                _timeString = value;
                try
                {
                    _time = MinMaxTime.ArrayFromString(value).ToArray();

                    //pokud se mezi časy nachází hodnota, která má časové rozpětí a nulový interval, vrátit false
                    //jinak true
                    TimeValid = !_time.Any(f => f.Min != f.Max && f.Interval == TimeSpan.Zero);
                }
                catch
                {
                    TimeValid = false;
                    _time = new MinMaxTime[] { };
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Time)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimeValid)));
            }
        }

        [XmlIgnore]
        public bool MonthValid { get; private set; } = true;
        [XmlIgnore]
        public bool DayInMonthValid { get; private set; } = true;
        [XmlIgnore]
        public bool DayInYearValid { get; private set; } = true;
        [XmlIgnore]
        public bool DayInWeekValid { get; private set; } = true;
        [XmlIgnore]
        public bool TimeValid { get; private set; } = true;

        /// <summary>
        /// Vrátí, jestli jsou zadané podmínky platné.
        /// </summary>
        [XmlIgnore]
        public bool AllValid
        {
            get
            {
                bool valid = MonthValid && DayInMonthValid && DayInYearValid && DayInWeekValid && TimeValid;
                return valid;
            }
        }

        /// <summary>
        /// Jestli zadaný měsíc splňuje podmínku na Měsíce. (1 - 12)
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        public bool MonthFits(int month) => _month.Length == 0 || _month.Any(f => f.ContainsIntMod(month, 12));

        public bool DayInMonthFits(int dayInMonth, int daysInMonth) => _dayInMonth.Length == 0 || _dayInMonth.Any(f => f.ContainsIntMod(dayInMonth, daysInMonth));

        public bool DayInWeekFits(int dayInWeek) => _dayInWeek.Length == 0 || _dayInWeek.Any(f => f.ContainsIntMod((int)dayInWeek, 7));

        public bool DayInWeekFits(DayOfWeek dayInWeek) => _dayInWeek.Length == 0 || _dayInWeek.Any(f => f.ContainsIntMod((int)dayInWeek, 7));

        public bool DayInYearFits(int dayInYear, bool leap = false) => _dayInYear.Length == 0 || _dayInYear.Any(f => f.ContainsIntMod(dayInYear, leap ? 366 : 365));


        public bool DateFits(DateTime date)
        {
            return 
                MonthFits(date.Month) && 
                DayInMonthFits(date.Day, DateTime.DaysInMonth(date.Year, date.Month)) && 
                DayInWeekFits(date.DayOfWeek) && 
                DayInYearFits(date.DayOfYear);
        }

        public bool TimeFits(TimeSpan time)
        {
            return _time.Any(f => f.ContainsTime(time));
        }

        public void Validate()
        {
            foreach (MinMaxTime span in _time)
            {
                if (span.Min != span.Max && span.Interval == TimeSpan.Zero)
                    throw new Exception($"Pokud je zadané časové rozpětí (v tomto případě {span.Min} - {span.Max}), interval nemůže být 0");
            }

            if (_time.Length == 0)
                throw new Exception("Musí být zadán čas!");
        }

        /// <summary>
        /// Vrátí všechny DateTime, které splňují podmínky, od  start po end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="exclusiveStart">jestli bude true, budou všechny vrácené hodnoty větší než start. Jinak mohou být i rovny</param>
        /// <param name="exclusiveEnd">jestli true, všechny vrácené hodnoty menší než end. Jinak mohou být i rovny.</param>
        /// <returns></returns>
        public IEnumerable<DateTime> AvailableDateTimesInTimeSpan(DateTime start, DateTime end, bool exclusiveStart = false, bool exclusiveEnd = false)
        {
            Validate();

            //podle exclusiveStart se rozhodneme, jestli používat > nebo >=
            var minComparer = Functions.GetBiggerComparer(exclusiveStart);

            //podle exclusiveEnd se rozhodneme, jestli používat > nebo >=
            var isAfterEnd = Functions.GetBiggerComparer(!exclusiveEnd)(end);

            int days = -1;
            DateTime current;
            DateTime startDate = start.Date;

            do
            {
                //zvýšíme hodnotu days, a nastavíme current na dané datum
                days++;
                current = startDate.AddDays(days);

                //pokud je toto první den v časovém rozpětí, je třeba se ujistit, že vrátíme časy větší,
                //   než je čas u startovního DateTime.
                //jinak čas není omezen, co se minima týče.
                TimeSpan minTimeOfDay = days == 0 ? start.TimeOfDay : TimeSpan.Zero;


                //pokud jsme za koncem, skončíme
                if (current > end)
                    yield break;

                //pokud tento den nesedí, pokračovat
                if (!DateFits(current))
                    continue;

                //zde vytvoříme IEnumerable všech možných časů na zálohu
                IEnumerable<TimeSpan> validTimes = Enumerable.Empty<TimeSpan>();
                foreach (var span in _time)
                    validTimes = validTimes.Union(span.Enumerate());

                //zde je profiltrujeme a seřadíme
                var isBiggerThanMinTimeOfDay = minComparer(minTimeOfDay);
                validTimes = validTimes.Where(a => isBiggerThanMinTimeOfDay(a)).OrderBy(f => f);

                //zde je projdeme
                foreach (var t in validTimes)
                {
                    //vytvořit z času a data DateTime
                    DateTime dt = current + t;

                    //pokud jsme to přepískli, nazdar
                    if (isAfterEnd(dt))
                        yield break;
                    //jinak yield return
                    else
                        yield return dt;
                }
            }
            while (current < end);
        }
    }

    public class MinMaxInt
    {
        public int Min;
        public int Max;

        public MinMaxInt(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public MinMaxInt(int value) => Min = Max = value;

        public override string ToString()
        {
            if (Min == Max) return Min.ToString();
            else return $"{Min} - {Max}";
        }

        public static MinMaxInt FromString(string str)
        {
            string[] minmax = str.Split('-');

            if (minmax.Length == 1)
                return new MinMaxInt(int.Parse(str.Trim()));
            else if (minmax.Length == 2)
                return new MinMaxInt(int.Parse(minmax[0].Trim()), int.Parse(minmax[1].Trim()));
            else
                throw new FormatException($"Zadaná hodnota {str} není platná.");
        }

        public static IEnumerable<MinMaxInt> ArrayFromString(string str)
        {
            if (str == "")
                yield break;

            foreach (string s in str.Split(','))
                yield return MinMaxInt.FromString(s);
        }

        public bool ContainsInt(int test) => test >= Min && test <= Max;

        public bool ContainsIntMod(int test, int mod)
        {
            int minmod = Min % mod;
            int maxmod = Max % mod;
            int testmod = test % mod;

            while (minmod > maxmod)
                minmod -= mod;

            while (testmod > maxmod)
                testmod -= mod;

            return testmod >= minmod; // && testmod <= maxmod;
        }
    }

    public class MinMaxTime
    {
        public TimeSpan Min;
        public TimeSpan Max;
        public TimeSpan Interval;

        public MinMaxTime(TimeSpan min, TimeSpan max)
        {
            if (min.TotalDays > 1 || max.TotalDays > 1 || min > max)
                throw new FormatException($"Čas od {min} do {max} není validní.");
            Min = min;
            Max = max;
            Interval = new TimeSpan(1, 0, 0);
        }

        public MinMaxTime(TimeSpan min, TimeSpan max, TimeSpan interval)
        {
            if (min.TotalDays > 1 || max.TotalDays > 1 || min > max)
                throw new FormatException($"Čas od {min} do {max} není validní.");
            Min = min;
            Max = max;
            Interval = interval;
        }

        public MinMaxTime(TimeSpan value) => Min = Max = value;

        public override string ToString()
        {
            if (Min == Max) return Min.ToString();
            else if (Interval == new TimeSpan(1, 0, 0)) return $"{Min} - {Max}";
            else return $"{Min} - {Max} / {Interval}";
        }

        public static MinMaxTime FromString(string str)
        {
            string[] minmax = str.Split('-');

            if (minmax.Length == 1)
                return new MinMaxTime(TimeSpan.Parse(str.Trim()));
            else if (minmax.Length == 2)
            {
                string[] maxint = minmax[1].Split('/');
                if (maxint.Length == 1)
                    return new MinMaxTime(TimeSpan.Parse(minmax[0].Trim()), TimeSpan.Parse(minmax[1].Trim()));
                else if (maxint.Length == 2)
                     return new MinMaxTime(TimeSpan.Parse(minmax[0].Trim()), TimeSpan.Parse(maxint[0].Trim()), TimeSpan.Parse(maxint[1].Trim()));
                else
                    throw new FormatException($"Zadaná hodnota {str} není platná.");
            }
            else
                throw new FormatException($"Zadaná hodnota {str} není platná.");
        }

        public static IEnumerable<MinMaxTime> ArrayFromString(string str)
        {
            if (str == "")
                yield break;

            foreach (string s in str.Split(','))
                yield return MinMaxTime.FromString(s);
        }

        public IEnumerable<TimeSpan> Enumerate()
        {
            TimeSpan current = Min;
            do
            {
                yield return current;
                if (Interval == TimeSpan.Zero) break;
                current += Interval;
            }
            while (current <= Max);
        }

        public bool ContainsTime(TimeSpan time) => time >= Min && time <= Max;
    }

}
