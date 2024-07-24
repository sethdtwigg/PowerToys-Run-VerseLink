using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Community.PowerToys.Run.Plugin.VerseLink
{
    public sealed partial class VerseLink
    {
        [DllImport("VerseLink.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VerseLinkInit();

        [DllImport("VerseLink.dll")]
        public static extern int VerseLinkRetrieve(string reference, string version, StringBuilder verseText, int verseTextSize, StringBuilder errMsg, int errMsgSize);

    }
}
