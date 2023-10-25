﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Noggin.NetCoreAuth.Extensions;

internal static class DictionaryExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Src: https://stackoverflow.com/questions/4943817/mapping-object-to-dictionary-and-vice-versa</remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    public static TReturn ToObject<TReturn, TValue>(this IDictionary<string, TValue> source)
    {
        var someObject = Activator.CreateInstance<TReturn>();
        Type someObjectType = someObject?.GetType()
            ?? throw new ArgumentException("Unable to create TReturn object.");

        foreach (var item in source)
        {
            var propertyName = item.Key.Replace("_", "", StringComparison.OrdinalIgnoreCase);
            var property = someObjectType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null) continue;

            try
            {
                var itemValue = Convert.ChangeType(item.Value.ToString(), property.PropertyType);
                property.SetValue(someObject, itemValue, null);
            }
            catch(Exception)
            {

            }
        }

        return someObject;
    }

}