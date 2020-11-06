using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Poskytuje přístup k různým hodícím se objektům (podobné funkce jako Dependency Injection v ASP.NET core)
    /// </summary>
    public static class Manager
    {
        private static Dictionary<Type, object> singletons = new Dictionary<Type, object>();
        private static Dictionary<Type, Func<object>> transients = new Dictionary<Type, Func<object>>();
        public static event Action<ImplementationSetEventArgs> OnImplementationSet;

        private static void impl_set(Type t, object obj)
        {
            OnImplementationSet?.Invoke(new ImplementationSetEventArgs()
            {
                Type = t,
                NewImplementation = obj,
                Singleton = true
            });
        }

        private static void impl_set(Type t)
        {
            OnImplementationSet?.Invoke(new ImplementationSetEventArgs()
            {
                Type = t,
                NewImplementation = false,
                Singleton = false
            });
        }

        
        /// <summary>
        /// Nastaví konkrétní instanci objektu. Get pro tento typ bude vždy vracet tuto stejnou instanci.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static T SetSingleton<T>(T instance) where T : class
        {
            var type = typeof(T);

            //do slovníku singletonů vrazit typ
            if (singletons.ContainsKey(type))
                singletons[type] = instance;
            else
                singletons.Add(type, instance);

            //pokud je tu transient se stejným typem, poslat ho pryč
            if (transients.ContainsKey(type))
                transients.Remove(type);

            impl_set(type, instance);
            return instance;
        }

        /// <summary>
        /// Nastaví daný typ jako transientní. Get vždy zavolá bezparametrový konstruktor tohoto typu a vrátí novou instanci.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void SetTransient<T>() where T : class
        {
            var type = typeof(T);
            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException("Typ musí mít konstruktor bez parametrů!");
            var func = new Func<object>(() => ctor.Invoke(new object[0]));

            if (transients.ContainsKey(type))
                transients[type] = func;
            else
                transients.Add(type, func);

            if (singletons.ContainsKey(type))
                singletons.Remove(type);

            impl_set(type);
        }

        /// <summary>
        /// Nastaví daný typ jako transientní. Get vždy zavolá instantiatingFunction a vrátí novou instanci.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instantiatingFunction"></param>
        public static void SetTransient<T>(Func<T> instantiatingFunction) where T : class
        {
            var type = typeof(T);

            if (transients.ContainsKey(type))
                transients[type] = instantiatingFunction;
            else
                transients.Add(type, instantiatingFunction);

            if (singletons.ContainsKey(type))
                singletons.Remove(type);

            impl_set(type);
        }

        /// <summary>
        /// Nastaví daný typ jako transientní. Get vždy zavolá factory.GetInstance a vrátí novou instanci.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        public static void SetTransient<T>(IFactory<T> factory) where T : class
        {
            var type = typeof(T);

            var func = new Func<T>(() => factory.GetInstance());
            if (transients.ContainsKey(type))
                transients[type] = func;
            else
                transients.Add(type, func);

            if (singletons.ContainsKey(type))
                singletons.Remove(type);

            impl_set(type);
        }

        /// <summary>
        /// Vrátí objekt daného typu z nastavených typů (pomocí SetSingleton / SetTransient). Pokud typ nebyl nalezen,
        /// vrátí null. Pokud se jedná o Transient typ a při jeho instanciaci dojde k výjimce, zařídíse to podle
        /// catchExceptions - jestliže true, vrátí null, jestliže false, vyhodí výjimku.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>(bool catchExceptions = false) where T : class
        {
            try
            {
                var type = typeof(T);
                if (singletons.ContainsKey(type))
                    return singletons[type] as T;
                else if (transients.ContainsKey(type))
                    return transients[type]() as T;
                else
                    return null;
            }
            catch
            {
                if (!catchExceptions)
                    throw;
                return null;
            }
        }

        /// <summary>
        /// Vrátí objekt daného typu z nastavených typů (pomocí SetSingleton / SetTransient). Pokud typ nebyl nalezen,
        /// vrátí null pokud throwException == false, nebo vyplivne InvalidOperationException pokud throwException == true
        /// </summary>
        /// <returns></returns>
        public static object Get(Type type, bool throwException = false)
        {
            if (singletons.ContainsKey(type))
                return singletons[type];
            else if (transients.ContainsKey(type))
                return transients[type]();
            else
            {
                if (throwException)
                    throw new InvalidOperationException("Daný typ nebyl nalezen.");
                return null;
            }
        }

        /// <summary>
        /// Vrátí objekt daného typu z nastavených typů (pomocí SetSingleton / SetTransient) a zároveň daný typ 
        /// zapomene. Pokud typ nebyl nalezen,
        /// vrátí null pokud throwException == false, nebo vyplivne InvalidOperationException pokud throwException == true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ForGet<T>(bool throwException = false) where T : class
        {
            var type = typeof(T);

            if (singletons.ContainsKey(type))
            {
                var instance = singletons[type] as T;
                singletons.Remove(type);
                return instance;
            }
            else if (transients.ContainsKey(type))
            {
                var instance = transients[type]() as T;
                transients.Remove(type);
                return instance;
            }
            else
            {
                if (throwException)
                    throw new InvalidOperationException("Daný typ nebyl nalezen.");
                return null;
            }
        }


        /// <summary>
        /// Vrátí objekt daného typu z nastavených typů (pomocí SetSingleton / SetTransient) a zároveň daný typ 
        /// zapomene. Pokud typ nebyl nalezen,
        /// vrátí null pokud throwException == false, nebo vyplivne InvalidOperationException pokud throwException == true
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ForGet<T>(Type type, bool throwException = false) where T : class
        {

            if (singletons.ContainsKey(type))
            {
                var instance = singletons[type] as T;
                singletons.Remove(type);
                return instance;
            }
            else if (transients.ContainsKey(type))
            {
                var instance = transients[type]() as T;
                transients.Remove(type);
                return instance;
            }
            else
            {
                if (throwException)
                    throw new InvalidOperationException("Daný typ nebyl nalezen.");
                return null;
            }
        }
    }

    public class ImplementationSetEventArgs
    {
        public Type Type { get; set; }
        public bool Singleton { get; set; }
        public object NewImplementation { get; set; }
    }
}
