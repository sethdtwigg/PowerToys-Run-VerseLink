// Copyright (c) Seth Twigg. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//https://github.com/8LWXpg/PowerToysRun-PluginTemplate
//https://conductofcode.io/post/creating-custom-powertoys-run-plugins/

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Text;
using System;
using Wox.Plugin;
using static System.Net.Mime.MediaTypeNames;

namespace Community.PowerToys.Run.Plugin.VerseLink
{
    public class Main : IPlugin, ISettingProvider
    {
        private Typer _typer = new Typer();
        private PluginInitContext _context;
        private string _icon_path;
        private int _beginTypeDelay;
        private string _bibleversion;
        private StringBuilder _verseText;
        private StringBuilder _errMsg;

        public string Name => "VerseLink";

        public string Description => "Types the Verse text for the given reference.";

        public static string PluginID => "53a0ea3fe92e3cb5af0dc68fe619f5e1";

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = "BeginTypeDelay",
                DisplayLabel = "Begin Type Delay (ms)",
                DisplayDescription = "Sets how long in milliseconds to wait before typing begins.",
                NumberValue = 0,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            },
            //new PluginAdditionalOption()
            //{
            //    Key = "BibleVersion",
            //    DisplayLabel = "Bible Version",
            //    DisplayDescription = "Select the Bible Translation you would like the verse to be typed.",
            //    NumberValue = 0,
            //    PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
            //}
        };

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());

            _verseText = new StringBuilder(5000000);
            _errMsg = new StringBuilder(256);
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var text = query.Search.Trim();
                results.Add(new Result
                {
                    Title = $"Reference: {text}",
                    SubTitle = "Types the verse for the given reference into the selected input.",
                    IcoPath = _icon_path,
                    Action = c =>
                    {
                        string vt = GetVerseText(text);
                        if (String.IsNullOrEmpty(vt)) return false;
                        Task.Run(() => _typer!.Type(vt, _beginTypeDelay));
                        return true;
                    },
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = "Reference Clipboard",
                    SubTitle = "Types the verse for the reference stored in clipboard into the selected input.",
                    IcoPath = _icon_path,
                    Action = c =>
                    {
                        var t = _typer.GetClipboard();
                        if(String.IsNullOrEmpty(t)) return false;
                        string vt = GetVerseText(t);
                        if (String.IsNullOrEmpty(vt)) return false;

                        Task.Run(() => _typer!.Type(vt, _beginTypeDelay));
                        //Task.Run(() => RunAsSTAThread(() => _typer.TypeClipboard(_beginTypeDelay)));
                        return true;
                    }
                }) ;
            }

            return results;
        }

        private string GetVerseText(string input)
        {
            int ret = VerseLink.VerseLinkInit();
            if (ret != 0) return "";

            _verseText.Clear();
            _errMsg.Clear();

            int retrieveResult = VerseLink.VerseLinkRetrieve(input, _bibleversion, _verseText, _verseText.Capacity, _errMsg, _errMsg.Capacity);
            if (retrieveResult == 0)
            {
                string vt = _verseText.ToString();
                return vt;
            }
            else
            {
                string error = _errMsg.ToString();
                Console.WriteLine($"Error retrieving verse: {error}");
                return "";
            }
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            _icon_path = (theme == Theme.Light || theme == Theme.HighContrastWhite) ?
                "Images/VerseLink.light.png" : "Images/VerseLink.dark.png";
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions is null) return;

            var typeDelay = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "BeginTypeDelay");
            _beginTypeDelay = (int)(typeDelay?.NumberValue ?? 200);
            _bibleversion = "KJV";//work here to load from settings
        }

        /// <summary>
        /// Start an Action within an STA Thread
        /// </summary>
        /// <param name="action">The action to execute in the STA thread</param>
        static void RunAsSTAThread(Action action)
        {
            AutoResetEvent @event = new AutoResetEvent(false);
            Thread thread = new Thread(
                () =>
                {
                    action();
                    @event.Set();
                });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            @event.WaitOne();
        }
    }
}
