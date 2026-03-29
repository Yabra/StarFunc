using System;
using System.Collections.Generic;

namespace StarFunc.Core
{
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
                throw new InvalidOperationException(
                    $"ServiceLocator: service of type {type.Name} is already registered.");

            _services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
                return (T)service;

            throw new InvalidOperationException(
                $"ServiceLocator: service of type {type.Name} is not registered.");
        }

        public static bool Contains<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        public static void Reset()
        {
            _services.Clear();
        }
    }
}
