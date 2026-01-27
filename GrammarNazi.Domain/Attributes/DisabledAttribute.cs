using System;

namespace GrammarNazi.Domain.Attributes;

/// <summary>
/// Marks an enum value as disabled, preventing it from being shown in user selection menus.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class DisabledAttribute : Attribute
{
}
