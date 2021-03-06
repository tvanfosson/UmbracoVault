﻿using System;

namespace UmbracoVault.TypeHandlers.Primitives
{
    public class ByteTypeHandler : ITypeHandler
    {
        private static object Get(string stringValue)
        {
            byte result;

            byte.TryParse(stringValue, out result);

            return result;
        }

    	public object GetAsType<T>(object input)
    	{
			return Get(input.ToString());
    	}

    	public Type TypeSupported => typeof(byte);
    }
}