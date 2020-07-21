using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Dělá kouzla pomocí System.Reflection
    /// </summary>
    public static class Reflector
    {
        /// <summary>
        /// Zkopíruje data (věřejná pole a veřejné vlastnostii) z instance "source" do instance "target"
        /// </summary>
        /// <typeparam name="T">Typ, který řešíme</typeparam>
        /// <param name="source">Zdroj</param>
        /// <param name="target">Cíl</param>
        /// <param name="propertyFilter">Filtr pro vlastnosti. Defaultně se berou všechny vlastnosti.</param>
        /// <param name="fieldFilter">Filtr pro pole. Defaultně se berou všechna pole.</param>
        public static void ShallowMirror<T>(T source, T target, 
            Func<PropertyInfo, bool> propertyFilter = null, Func<FieldInfo, bool> fieldFilter = null)
            where T : class
        {
            var type = typeof(T);

            //VLASTNOSTI

            //pokud je propertyFilter null, nastavíme ho na funkci, která vždy vrací true
            propertyFilter = propertyFilter ?? new Func<PropertyInfo, bool>(_ => true); 

            var properties = type.GetProperties(); //získat seznam vlastností tohoto typu
            foreach(var property in properties) //projít jej
            {
                //pokud vlastnost neprojde filtrem, přeskočit iteraci
                if (!propertyFilter(property))
                    continue;

                //pokud vlastnost nemá dostupnou set nebo get funkci, přeskočit iteraci
                if (property.SetMethod?.IsPublic != true || property.GetMethod?.IsPublic != true)
                    continue;
                
                var value = property.GetValue(source); //získat hodnotu ze zdroje
                property.SetValue(target, value); //nastavit hodnotu do cíle
            }

            //POLE

            //pokud je fieldFilter null, nastavíme ho na funkci, která vždy vrací true
            fieldFilter = fieldFilter ?? new Func<FieldInfo, bool>(_ => true);

            var fields = type.GetFields();
            foreach(var field in fields)
            {
                //pokud pole neprojde filtrem, přeskočit iteraci
                if (!fieldFilter(field))
                    continue;

                var value = field.GetValue(source); //získat hodnotu ze zdroje
                field.SetValue(target, value); //nastavit hodnotu do cíle
            }
        }

        /// <summary>
        /// Nastaví kolekci target na source s tím, že: <br />
        ///    - pokud pro prvek v kolekci source A a prvek v kolekci target B platí, že instanceComparator(A, B), zavolá se ShallowMirror(A, B) <br />
        ///    - pokud pro prvek v kolekci source A platí, že v kolekci target neexistuje žádný prvek B, pro nějž platí instanceComparator(A, B), prvek A se přidá do kolekce target <br />
        ///    - pokud pro prvek v kolekci target B platí, že v kolekci source neexistuje žádný prvek A, pro nějž platí instanceComparator(A, B), prvek B se odstraní z kolekce target
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="instanceComparator"></param>
        public static void UpdateCollectionByCompare<T>(this ICollection<T> target, IEnumerable<T> source,
            Func<T, T, bool> instanceComparator) where T : class
        {
            //zde budou instance v kolekci target, u nichž jsme našli odpovídající instance v kolekci source
            HashSet<T> setInTarget = new HashSet<T>(); 

            //projít kolekci source
            foreach(T sourceItem in source)
            {
                //získat z kolekce target všechny prvky, které odpovídají sourceItem
                var correspondingInTarget = target.Where(i => instanceComparator(i, sourceItem)).ToArray();

                //pokud v kolekci target žádné odpovídající prvky nejsou, přidat na ni sourceItem
                if (!correspondingInTarget.Any())
                {
                    target.Add(sourceItem);
                    setInTarget.Add(sourceItem);
                    continue;
                }

                //jinak projít odpovídající prvky a vždy nastavit data v instanci v target podle dat v sourceItem
                foreach (var targetItem in correspondingInTarget)
                {
                    ShallowMirror(sourceItem, targetItem);
                    setInTarget.Add(targetItem);
                }
            }

            //projít v kolekci target všechny prvky
            foreach (T targetItem in target.ToArray())
            {
                //přeskočit ty prvky, pro něž jsme v kolekci source našli odpovídající prvky
                if (setInTarget.Contains(targetItem))
                    continue;

                //odstranit ty prvky, pro něž jsme v kolekci source nenašli odpovídající prvky
                target.Remove(targetItem);
            }
        }
    }
}
