namespace System.Runtime.CompilerServices;

/// <summary>
/// The attribute the compiler marks nullable reference types with, as it would make itself.
/// </summary>
/// <remarks>
/// From game version 2.58, the game's <c>UnityEngine.CoreModule</c> has a public type with this name and no
/// constructors, which the compiler would otherwise find and fail to use. One defined here is used instead.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.GenericParameter
    | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    /// <summary>Whether each part of the type is nullable: 0 oblivious, 1 not nullable, 2 nullable.</summary>
    public readonly byte[] NullableFlags;

    /// <summary>Marks the whole type the same way.</summary>
    /// <param name="value">0 oblivious, 1 not nullable, 2 nullable.</param>
    public NullableAttribute(byte value) => NullableFlags = [value];

    /// <summary>Marks each part of the type.</summary>
    /// <param name="value">The flag for each part.</param>
    public NullableAttribute(byte[] value) => NullableFlags = value;
}
