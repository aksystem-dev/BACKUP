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

        public static T SetSingleton<T>(T instance) where T : class
        {
            var type = typeof(T);
            if (singletons.ContainsKey(type))
                singletons[type] = instance;
            else
                singletons.Add(type, instance);
            impl_set(type, instance);
            return instance;
        }

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
            impl_set(type);
        }

        public static void SetTransient<T>(Func<T> factory) where T : class
        {
            var type = typeof(T);
            if (transients.ContainsKey(type))
                transients[type] = factory;
            else
                transients.Add(type, factory);
            impl_set(type);
        }

        public static void SetTransient<T>(IFactory<T> factory) where T : class
        {
            var type = typeof(T);
            var func = new Func<T>(() => factory.GetInstance());
            if (transients.ContainsKey(type))
                transients[type] = func;
            else
                transients.Add(type, func);
            impl_set(type);
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (singletons.ContainsKey(type))
                return singletons[type] as T;
            else if (transients.ContainsKey(type))
                return transients[type]() as T;
            else
                return null;
        }

        public static object Get(Type type)
        {
            if (singletons.ContainsKey(type))
                return singletons[type];
            else if (transients.ContainsKey(type))
                return transients[type]();
            else
                return null;
        }
    }

    public class ImplementationSetEventArgs
    {
        public Type Type { get; set; }
        public bool Singleton { get; set; }
        public object NewImplementation { get; set; }
    }
}
