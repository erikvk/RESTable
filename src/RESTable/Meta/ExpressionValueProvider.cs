﻿using System;
using System.Reflection;

namespace RESTable.Meta
{
    /// <summary>
    /// Get and set values for a <see cref="MemberInfo"/> using dynamic methods.
    /// </summary>
    internal class ExpressionValueProvider
    {
        private readonly PropertyInfo _memberInfo;
        private Func<object, object?>? _getter;
        private Action<object, object?>? _setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionValueProvider"/> class.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        public ExpressionValueProvider(PropertyInfo memberInfo)
        {
            _memberInfo = memberInfo;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="target">The target to set the value on.</param>
        /// <param name="value">The value to set on the target.</param>
        public void SetValue(object target, object? value)
        {
            try
            {
                if (_setter == null)
                {
                    _setter = ExpressionReflectionDelegateFactory.Instance.CreateSet<object>(_memberInfo);
                }
                _setter(target, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error setting value to '{_memberInfo.Name}' on '{target.GetType()}'.", ex);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="target">The target to get the value from.</param>
        /// <returns>The value.</returns>
        public object? GetValue(object target)
        {
            try
            {
                if (_getter == null)
                {
                    _getter = ExpressionReflectionDelegateFactory.Instance.CreateGet<object>(_memberInfo);
                }
                return _getter(target);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting value from '{_memberInfo.Name}' on '{target.GetType()}'.", ex);
            }
        }
    }
}