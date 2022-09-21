using System;

namespace Unity.XR.CoreUtils.Bindings.Variables
{
    /// <summary>
    /// Generic class which contains a member variable of type <c>T</c> and provides a binding API to data changes.
    /// T is IEquatable to prevent Equals gc alloc.
    /// </summary>
    /// <typeparam name="T">BindableVariable type</typeparam>
    public class BindableVariable<T> : BindableVariableBase<T> where T : IEquatable<T>
    {
        /// <inheritdoc/>
        public BindableVariable(T initialValue = default, bool checkEquality = true, Func<T, T, bool> equalityMethod = null, bool startInitialized = false)
            : base(initialValue, checkEquality, equalityMethod, startInitialized)
        {
        }
    }
}
