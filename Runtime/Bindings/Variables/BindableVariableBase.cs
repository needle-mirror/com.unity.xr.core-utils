using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.XR.CoreUtils.Bindings;
using UnityEngine;

namespace Unity.XR.CoreUtils.Bindings.Variables
{
    /// <summary>
    /// Generic class which contains a member variable of type <c>T</c> and provides a binding API to data changes.
    /// </summary>
    /// <typeparam name="T">BindableVariableBase type</typeparam>
    [Serializable]
    public class BindableVariableBase<T> : IReadOnlyBindableVariable<T>
    {
        T m_InternalValue;
        readonly bool m_CheckEquality;
        bool m_IsInitialized;
        readonly Func<T, T, bool> m_EqualityMethod;
        int m_BindingCount;

        /// <summary>
        /// The internal variable value. When setting the value, subscribers may be notified.
        /// The subscribers will not be notified if this variable is initialized, is configured to check for equality,
        /// and the new value is equivalent.
        /// </summary>
        public T Value
        {
            get => m_InternalValue;
            set
            {
                if (m_IsInitialized && (m_CheckEquality && (m_EqualityMethod?.Invoke(m_InternalValue, value) ?? ValueEquals(value))))
                    return;

                m_IsInitialized = true;
                m_InternalValue = value;
                BroadcastValue();
            }
        }

        /// <summary>
        /// Get number of subscribed binding callbacks.
        /// Note that if you manually call <see cref="Unsubscribe"/> with the same callback several times this value may be inaccurate.
        /// For best results leverage the <see cref="IEventBinding"/> returned by the subscribe call and use that to unsubscribe as needed.
        /// </summary>
        public int BindingCount => m_BindingCount;

        /// <summary>
        /// Update the value of the internal bindable variable.
        /// </summary>
        /// <param name="value">New Value to set.</param>
        public void SetValue(T value) => Value = value;

        event Action<T> ValueUpdated;

        /// <inheritdoc />
        public IEventBinding Subscribe(Action<T> callback)
        {
            EventBinding newBinding = new EventBinding();
            if (callback != null)
            {
                Action<T> callbackReference = callback;
                newBinding.BindAction = () =>
                {
                    ValueUpdated += callbackReference;
                    IncrementReferenceCount();
                };
                newBinding.UnBindAction = () =>
                {
                    ValueUpdated -= callbackReference;
                    DecrementReferenceCount();
                };
                newBinding.Bind();
            }

            return newBinding;
        }

        /// <inheritdoc />
        public IEventBinding SubscribeAndUpdate(Action<T> callback)
        {
            if (callback != null)
            {
                callback(m_InternalValue);
            }

            return Subscribe(callback);
        }

        /// <inheritdoc />
        public void Unsubscribe(Action<T> callback)
        {
            if (callback != null)
            {
                ValueUpdated -= callback;
                DecrementReferenceCount();
            }
        }

        void IncrementReferenceCount()
        {
            m_BindingCount++;
        }

        void DecrementReferenceCount()
        {
            m_BindingCount = Mathf.Max(0, m_BindingCount - 1);
        }

        /// <summary>
        /// Constructor for bindable variable, which is a variable that notifies listeners when the internal value changes.
        /// </summary>
        /// <param name="initialValue">Value to initialize variable with. Uses type default if field empty.</param>
        /// <param name="checkEquality">Setting true checks whether to compare new value to old before triggering callback. Default false.</param>
        /// <param name="equalityMethod">Func used to provide custom equality checking behavior. Default is Equatable check.</param>
        /// <param name="startInitialized">Setting false results in initial value setting will trigger registered callbacks, regardless of whether the value is the same as the initial one.</param>
        public BindableVariableBase(T initialValue = default, bool checkEquality = true, Func<T, T, bool> equalityMethod = null, bool startInitialized = false)
        {
            m_IsInitialized = startInitialized;
            m_InternalValue = initialValue;
            m_CheckEquality = checkEquality;
            m_EqualityMethod = equalityMethod;
            m_BindingCount = 0;
        }

        /// <summary>
        /// Triggers a callback for all subscribed listeners with the current internal variable value.
        /// </summary>
        public void BroadcastValue()
        {
            ValueUpdated?.Invoke(m_InternalValue);
        }

        /// <inheritdoc />
        public Task<T> Task(Func<T, bool> awaitPredicate, CancellationToken token = default)
        {
            if (awaitPredicate != null && awaitPredicate.Invoke(m_InternalValue))
                return System.Threading.Tasks.Task.FromResult(m_InternalValue);

            return new BindableVariableTaskPredicate<T>(this, awaitPredicate, token).Task;
        }

        /// <inheritdoc />
        public Task<T> Task(T awaitState, CancellationToken token = default)
        {
            if (ValueEquals(awaitState))
                return System.Threading.Tasks.Task.FromResult(m_InternalValue);

            return new BindableVariableTaskState<T>(this, awaitState, token).task;
        }

        // IEquatable API
        /// <inheritdoc />
        public virtual bool ValueEquals(T other) => m_InternalValue.Equals(other);
    }
}
