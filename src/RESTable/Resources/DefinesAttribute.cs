﻿using System;

// ReSharper disable All
#pragma warning disable 1591

namespace RESTable.Resources;

/// <summary>
///     Used to indiate that some other property/properties are a reflection of
///     the state of this property
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DefinesAttribute : Attribute
{
    public DefinesAttribute(params string[] terms) => Terms = terms;
    internal string[] Terms { get; }
}
