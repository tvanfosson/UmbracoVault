﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Umbraco.Core;
using UmbracoVault.Attributes;

namespace UmbracoVault.TypeHandlers
{
    /// <summary>
    ///     Loads available TypeHandlers and provides a method to retrieve a handler for a given type
    /// </summary>
    public class TypeHandlerFactory
    {
        private static TypeHandlerFactory _instance;
        private readonly Dictionary<Type, ITypeHandler> _typeHandlerDictionary;

        private TypeHandlerFactory()
        {
            List<ITypeHandler> typeHandlers = GetBuiltInTypeHandlers();
            typeHandlers.AddRange(GetExternalTypeHandlers());

            _typeHandlerDictionary = new Dictionary<Type, ITypeHandler>();

            foreach (ITypeHandler typeHandler in typeHandlers)
            {
                if (!_typeHandlerDictionary.ContainsKey(typeHandler.TypeSupported))
                {
                    _typeHandlerDictionary.Add(typeHandler.TypeSupported, typeHandler);
                }
                else
                {
                    Trace.TraceWarning(
                        "TypeHandler {0} for data type {1} cannot be registered bceause type {1} already has a registration for typehandler {2}", 
                        typeHandler, 
                        typeHandler.TypeSupported, 
                        _typeHandlerDictionary[typeHandler.TypeSupported]);
                }
            }
        }

        public static TypeHandlerFactory Instance => _instance ?? (_instance = new TypeHandlerFactory());

        private IEnumerable<ITypeHandler> GetExternalTypeHandlers()
        {
            var result = new List<ITypeHandler>();
            var flaggedAssemblies = new List<Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetCustomAttributes(typeof(ContainsUmbracoVaultTypeHandlersAttribute), false).Any())
                    {
                        flaggedAssemblies.Add(assembly);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unable to check assembly {assembly} for ContainsUmbracoVaultTypeHandlersAttribute.\n{e.Message}\n{e.StackTrace}");
                }
            }

            foreach (var externalAssembly in flaggedAssemblies)
            {
                try
                {
                    var types = externalAssembly.GetTypes()
                        .Where(IsTypeHandlerThatIsNotAutoLoadIgnored)
                        .Select(CreateInstanceOfTypeHandler);

                    result.AddRange(types);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unable to load TypeHandlers from assembly {externalAssembly}.\n{e.Message}\n{e.StackTrace}");
                }

            }

            return result;
        }

        private List<ITypeHandler> GetBuiltInTypeHandlers()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(IsTypeHandlerThatIsNotAutoLoadIgnored)
                .Select(CreateInstanceOfTypeHandler)
                .Where(x => x != null)
                .ToList();
        }

        private static bool IsTypeHandlerThatIsNotAutoLoadIgnored(Type x)
        {
            return x.GetInterfaces().Contains(typeof (ITypeHandler)) && x.IsClass &&
                   !x.GetCustomAttributes(typeof (IgnoreTypeHandlerAutoRegistrationAttribute), true).Any();
        }

        /// <summary>
        ///     Retrieves a TypeHandler for a given type. Returns null if the TypeHandler does not exist
        /// </summary>
        /// <param name="t">Type for which to retrieve the TypeHandler for</param>
        /// <returns>A TypeHandler for the given type, or null if one is not available for that type.</returns>
        public ITypeHandler GetHandlerForType(Type t)
        {
            if (t.IsGenericType)
            {
                // If it's generic, match on the type name, as the exact definition will vary based on the generic type
                return _typeHandlerDictionary.FirstOrDefault(x => x.Key.Name == t.Name).Value;
            }
            bool hasType = _typeHandlerDictionary.ContainsKey(t);
            if (hasType)
            {
                return _typeHandlerDictionary[t];
            }
            return null;
        }

        public void RegisterTypeHandler<T>() where T : ITypeHandler
        {
            var handler = CreateInstanceOfTypeHandler<T>();
            if (_typeHandlerDictionary.ContainsKey(handler.TypeSupported))
            {
                return; //Ignore, already exists
            }
            
            _typeHandlerDictionary.Add(handler.TypeSupported, handler);
        }

        private static ITypeHandler CreateInstanceOfTypeHandler<T>() where T : ITypeHandler
        {
            return CreateInstanceOfTypeHandler(typeof (T));
        }

        private static ITypeHandler CreateInstanceOfTypeHandler(Type t)
        {
            if (!t.Implements<ITypeHandler>())
                return null;

            var constructorInfo = t.GetConstructor(Type.EmptyTypes);
            var result = constructorInfo?.Invoke(null);
            return result as ITypeHandler;
        }
    }
}