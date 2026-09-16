// Copyright (c) Seth Twigg. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace Community.PowerToys.Run.Plugin.VerseLink
{
    internal sealed class Typer
    {
        private readonly char[] _specialCharacters = { '{', '}', '+', '^', '%', '~', '(', ')'  };
        private const int INTERKEYDELAY = 0;

        // How long the pasted text stays on the clipboard before the previous contents
        // are put back. Restoring immediately would race the target app's paste.
        private const int CLIPBOARDRESTOREDELAY = 300;

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

        internal string GetClipboard()
        {
            var text = "";
            // Callers may be on a thread pool thread; the clipboard requires STA.
            RunOnSTAThread(() => text = Clipboard.ContainsText() ? Clipboard.GetText() : "");
            return text;
        }

        internal void Paste(string text)
        {
            var previous = GetClipboard();

            SetClipboard(text);
            SendKeys.SendWait("^{v}");

            // Hand the user their clipboard back rather than leaving the verse on it.
            Thread.Sleep(CLIPBOARDRESTOREDELAY);
            SetClipboard(previous);
        }

        private static void SetClipboard(string text)
        {
            RunOnSTAThread(() =>
            {
                if (String.IsNullOrEmpty(text))
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetText(text);
                }
            });
        }

        private static void RunOnSTAThread(Action action)
        {
            Thread thread = new Thread(() =>
            {
                // The clipboard can be locked by another process; a failed read or write
                // is not worth tearing down the paste over.
                try
                {
                    action();
                }
                catch (Exception)
                {
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
