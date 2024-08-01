﻿// Copyright (c) Seth Twigg. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace Community.PowerToys.Run.Plugin.VerseLink
{
    internal sealed class Typer
    {
        private readonly char[] _specialCharacters = { '{', '}', '+', '^', '%', '~', '(', ')'  };
        private const int INTERKEYDELAY = 0;

        public bool TypeEnter { get; set; } = true;

        public void Type(string str, int delay = 2000)
        {
            Thread.Sleep(delay);
            foreach (var c in str.ToCharArray())
            {
                // Some characters have special meaning and must be surrounded by '{}'
                // https://docs.microsoft.com/en-us/office/vba/language/reference/user-interface-help/sendkeys-statement
                if (_specialCharacters.Contains(c))
                {
                    SendKeys.SendWait("{" + c + "}");
                }
                else
                {
                    SendKeys.SendWait(c.ToString());
                }
               
                Thread.Sleep(INTERKEYDELAY);
            }
        }

        internal void TypeClipboard(int beginTypeDelay)
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }

            var text = Clipboard.GetText();
            Type(text, beginTypeDelay);
        }

        internal string GetClipboard()
        {
            return (Clipboard.ContainsText()) ? Clipboard.GetText() : "";
        }

        internal void Paste(string text)
        {
            Thread thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
     
            SendKeys.SendWait("^{v}");
        }
    }
}
