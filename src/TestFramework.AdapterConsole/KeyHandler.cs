// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools.AdapterConsole
{
    internal class KeyHandler
    {
        private ConsoleKeyInfo _keyInfo;
        private Dictionary<string, Action> _keyActions;
        private StringBuilder _text;

        internal KeyHandler()
        {
            _keyActions = new Dictionary<string, Action>();
            _text = new StringBuilder();
        }

        private string BuildKeyInput()
        {
            return (_keyInfo.Modifiers != ConsoleModifiers.Control && _keyInfo.Modifiers != ConsoleModifiers.Shift) ?
                _keyInfo.Key.ToString() : _keyInfo.Modifiers.ToString() + _keyInfo.Key.ToString();
        }

        private void WriteContent() => WriteContent(_keyInfo.KeyChar);

        private void WriteContent(char c)
        {
            _text.Append(c);
            Console.Write(c.ToString());
        }

        public string Text
        {
            get
            {
                return _text.ToString();
            }
        }

        public void Handle(ConsoleKeyInfo keyInfo)
        {
            _keyInfo = keyInfo;

            Action action;
            _keyActions.TryGetValue(BuildKeyInput(), out action);
            action = action ?? WriteContent;
            action.Invoke();
        }
    }
}
