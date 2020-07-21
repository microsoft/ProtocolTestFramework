// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text;

namespace Microsoft.Protocols.TestTools.Messages.Runtime
{
    /// <summary>
    /// A type to describe an expected event.
    /// </summary>
    public struct ExpectedEvent
    {
        /// <summary>
        /// The event waited for, identified by its
        /// reflection representation.
        /// </summary>
        public EventInfo Event
        {
            get;
            private set;
        }

        /// <summary>
        /// The target of the event (the instance object where the event
        /// belongs too), or null, if it is a static or an adapter event.
        /// </summary>
        public object Target
        {
            get;
            private set;
        }

        /// <summary>
        /// The checker to be called when the event
        /// arrives. 
        /// </summary>
        public Delegate Checker
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs an expected event.
        /// </summary>
        /// <param name="eventInfo">The reflection information of the event.</param>
        /// <param name="target">The target object. Must be null for static events and for adapter events.</param>
        /// <param name="checker">
        ///     The checker. Must match the type of the event. A compatible type is a delegate type
        ///     either taking an array of objects as arguments, exactly the arguments of the event, or
        ///     exactly the arguments of the event preceded by an instance of the event target.      
        /// </param>
        /// <returns></returns>
        public ExpectedEvent(EventInfo eventInfo, object target, Delegate checker) : this()
        {
            this.Event = eventInfo;
            this.Target = target;
            this.Checker = checker;
        }

        /// <summary>
        /// Delivers readable representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (Target != null)
            {
                result.Append(MessageRuntimeHelper.Describe(Target));
                result.Append(".");
            }
            result.Append("event ");
            result.Append(Event.Name);
            result.Append("(...)");
            return result.ToString();
        }

    }
}
