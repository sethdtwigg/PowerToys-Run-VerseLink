﻿// Copyright (c) Seth Twigg. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//https://github.com/8LWXpg/PowerToysRun-PluginTemplate
//https://conductofcode.io/post/creating-custom-powertoys-run-plugins/

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using System.Text;
using System;
using Wox.Plugin;
using static System.Net.Mime.MediaTypeNames;
using Wox.Plugin.Logger;
using System.IO;
using VerseLinkWindows;

namespace Community.PowerToys.Run.Plugin.VerseLink
{
    public class Main : IPlugin, ISettingProvider
    {
        private Typer _typer = new Typer();
        private PluginInitContext _context;
        private string _icon_path;
        private int _beginTypeDelay;
        private string _bibleversion;
        private string _verseText;
        private string _errMsg;
        private VerseLinkWindows.VerseLink _VL;
        private VerseLinkWindows.BibleReferenceVerseFormat _BibleReferenceVerseFormat;
        private Dictionary<string, string> _bibleVersions = new Dictionary<string, string>()
        {
            { "ESV" ,"0"},
            { "KJV" ,"1"},
            { "NASB","2" }
        };

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
            new PluginAdditionalOption()
            {
                Key = "BibleVersion",
                DisplayLabel = "Bible Version",
                DisplayDescription = "The Bible Translation Version that will be used to type the verses.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems =
                [
                    _bibleVersions.ElementAt(0),
                    _bibleVersions.ElementAt(1),
                    _bibleVersions.ElementAt(2)
                ],
                ComboBoxValue = 1
            },
            new PluginAdditionalOption()
            {
                Key = "IncludeReference",
                DisplayLabel = "Include Reference",
                DisplayDescription = "Whether to include the Verse Reference when typing the verse.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = true
            },
            new PluginAdditionalOption()
            {
                Key = "IncludeVerseNumbers",
                DisplayLabel = "Include Verse Numbers",
                DisplayDescription = "Whether to include the Verse Numbers when typing the verse.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = true
            },
            new PluginAdditionalOption()
            {
                Key = "NewLineBetweenChapters",
                DisplayLabel = "Include New-Line Between Chapters",
                DisplayDescription = "Whether to include a blank line between chapters when typing the verse.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = false
            }
        };

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());

            string currPath = _context.CurrentPluginMetadata.ExecuteFilePath;
            string _bibleXml_path = Path.GetDirectoryName(currPath) ?? "";

            _VL = new VerseLinkWindows.VerseLink(_bibleversion, _bibleXml_path, _BibleReferenceVerseFormat);
            if (_VL.Error)
            {
                string error = _VL.LastError;
                Log.Exception(error, new Exception("VerseLinkWindows.VerseLink(_bibleversion)"),this.GetType(), "Init", "Main.cs", 61);
            }
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
                        if(_beginTypeDelay == 0)
                        {
                            Task.Run(() => _typer!.Paste(vt));
                        }
                        else
                        {
                            Task.Run(() => _typer!.Type(vt, _beginTypeDelay));
                        }                        
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

                        if (_beginTypeDelay == 0)
                        {
                            Task.Run(() => _typer!.Paste(vt));
                        }
                        else
                        {
                            Task.Run(() => _typer!.Type(vt, _beginTypeDelay));
                        }
                        //Task.Run(() => RunAsSTAThread(() => _typer.TypeClipboard(_beginTypeDelay)));
                        return true;
                    }
                }) ;
            }

            return results;
        }

        private string GetVerseText(string input)
        {
            _verseText = "";
            _errMsg = "";

            _verseText = _VL.VerseLinkRetrieve(input);
            if (_VL.Error)
            {
                string error = _VL.LastError;
                Log.Exception(error, new Exception("_VL.VerseLinkRetrieve(input)"), this.GetType(), "GetVerseText", "Main.cs", 119);
            }
            return _verseText;
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
            int bv = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "BibleVersion")?.ComboBoxValue ?? 0;
            _bibleversion = _bibleVersions.FirstOrDefault(x => x.Value == bv.ToString()).Key;

            _BibleReferenceVerseFormat = new BibleReferenceVerseFormat();

            var ir = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "IncludeReference");
            _BibleReferenceVerseFormat.IncludeReference = ir?.Value ?? true;
            var ivn = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "IncludeVerseNumbers");
            _BibleReferenceVerseFormat.IncludeVerseNumbers = ivn?.Value ?? true;
            var inlbc = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "NewLineBetweenChapters");
            _BibleReferenceVerseFormat.IncludeNewLineBetweenChapters = inlbc?.Value ?? true;
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
