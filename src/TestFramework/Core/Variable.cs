// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Protocols.TestTools.Messages.Runtime;
using System;

namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Type representing a transacted variable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IVariable<T>
    {
        /// <summary>
        /// Determines whether the variable is bound.
        /// </summary>
        bool IsBound { get; }

        /// <summary>
        /// Resets the variable to unbound state, so that it
        /// can be bound to new value later.
        /// </summary>
        void Unbind();

        /// <summary>
        /// Gets or sets a value. If the variable is bound, setting
        /// will result in an equality check on its current value
        /// with the given value. If the variable is unbound, getting
        /// will result in generation of a default value.
        /// </summary>
        T Value { get; set; }
    }

    abstract class VariableBase
    {
        internal abstract void InternalUnbind();
        internal abstract string Name { get; }
        internal abstract object ObjectValue { get; }
    }

    class Variable<T> : VariableBase, IVariable<T>
    {
        string name;
        T value;
        bool isBound;
        IProtocolTestsManager manager;

        internal Variable(string name, IProtocolTestsManager manager)
        {
            this.name = name;
            this.manager = manager;
        }

        public void Unbind()
        {
            InternalUnbind();
        }

        internal override void InternalUnbind()
        {
            isBound = false;
            value = default(T);
        }

        internal override string Name
        {
            get { return name; }
        }

        internal override object ObjectValue
        {
            get { return value; }
        }

        #region IVariable<T>

        /// <summary>
        /// Determines whether the variable is bound.
        /// </summary>
        public bool IsBound
        {
            get { return isBound; }
        }


        /// <summary>
        /// Gets or sets a value. If the variable is bound, setting
        /// will result in an equality check on its current value
        /// with the given value. If the variable is unbound, getting
        /// will result in generation of a default value.
        /// </summary>
        public T Value
        {
            get
            {
                if (!isBound)
                {
                    throw new UnboundVariableException("Variable's value cannot be read before it is bound");
                }
                return value;
            }
            set
            {
                if (!isBound)
                {
                    this.value = value;
                    isBound = true;
                }
                else
                {
                    manager.Assert(Object.Equals(this.value, value),
                                   String.Format(
                            "bound variable '{0}' can only be assigned to equal value (bound value: '{1}', new value: '{2}')",
                            this.name, this.value, value));
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// TransactionEventKind
    /// </summary>
    public enum TransactionEventKind
    {
        /// <summary>
        /// Assert
        /// </summary>
        Assert,
        /// <summary>
        /// Assume
        /// </summary>
        Assume,
        /// <summary>
        /// Checkpoint
        /// </summary>
        Checkpoint,
        /// <summary>
        /// Comment
        /// </summary>
        Comment,
        /// <summary>
        /// VariableBound
        /// </summary>
        VariableBound
    }

    /// <summary>
    /// TransactionEvent
    /// </summary>
    public struct TransactionEvent
    {
        internal TransactionEventKind Kind;
        internal bool condition;
        internal string description;
        internal VariableBase variable;

        internal TransactionEvent(TransactionEventKind kind, bool condition, string description, VariableBase variable)
        {
            this.Kind = kind;
            this.condition = condition;
            this.description = description;
            this.variable = variable;
        }
    }
}
