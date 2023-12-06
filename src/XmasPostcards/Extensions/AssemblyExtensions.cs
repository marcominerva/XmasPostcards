﻿using System.Reflection;

namespace XmasPostcards.Extensions;

public static class AssemblyExtensions
{
    public static string GetAttribute<T>(this Assembly assembly, Func<T, string> value) where T : Attribute
    {
        var attribute = (T)Attribute.GetCustomAttribute(assembly, typeof(T))!;
        return value.Invoke(attribute);
    }
}