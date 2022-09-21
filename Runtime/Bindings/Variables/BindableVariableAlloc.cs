using System;

namespace Unity.XR.CoreUtils.Bindings.Variables
{
    /// <summary>
    /// Generic class which contains a member variable of type <c>T</c> and provides a binding API to data changes.
    /// <c>T</c> is not <c>IEquatable</c>. When setting the Value, it calls <c>Equals</c> and will GC alloc.
    /// If T is IEquatable, use <see cref="BindableVariable{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of BindableVariable to allocate.</typeparam>
    /// <seealso cref="BindableVariable{T}"/>
    public class BindableVariableAlloc<T> : BindableVariableBase<T>
    {
        /// <inheritdoc/>
        public BindableVariableAlloc(T initialValue = default, bool checkEquality = true, Func<T, T, bool> equalityMethod = null, bool startInitialized = false)
            : base(initialValue, checkEquality, equalityMethod, startInitialized)
        {
        }
    }
}
