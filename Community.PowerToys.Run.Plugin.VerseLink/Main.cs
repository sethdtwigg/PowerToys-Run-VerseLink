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
using Wox.Plugin.Logger;
using System.IO;
using VerseLinkWindows;

namespace Community.PowerToys.Run.Plugin.VerseLink
{
    public class Main : IPlugin, ISettingProvider, IDisposable
    {
        private Typer _typer = new Typer();
        private bool _disposed;
        private PluginInitContext _context;
        private string _icon_path;
        private int _beginTypeDelay;
        private string _bibleversion = DefaultBibleVersion;
        private string _verseText;
        private string _errMsg;
        private VerseLinkWindows.VerseLink? _VL;
        private string? _bibleXml_path;

        private const string DefaultBibleVersion = "KJV";
        private const int DefaultBibleVersionValue = 1;
        private const bool DefaultIncludeReference = true;
        private const bool DefaultIncludeVerseNumbers = true;
        private const bool DefaultNewLineBetweenChapters = false;

        // Seeded with the declared defaults so Init can build a usable VerseLink
        // even if the host has not called UpdateSettings yet.
        private VerseLinkWindows.BibleReferenceVerseFormat _BibleReferenceVerseFormat = new BibleReferenceVerseFormat()
        {
            IncludeReference = DefaultIncludeReference,
            IncludeVerseNumbers = DefaultIncludeVerseNumbers,
            IncludeNewLineBetweenChapters = DefaultNewLineBetweenChapters,
        };

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
                ComboBoxItems = [.. _bibleVersions],
                ComboBoxValue = DefaultBibleVersionValue
            },
            new PluginAdditionalOption()
            {
                Key = "IncludeReference",
                DisplayLabel = "Include Reference",
                DisplayDescription = "Whether to include the Verse Reference when typing the verse.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = DefaultIncludeReference
            },
            new PluginAdditionalOption()
            {
                Key = "IncludeVerseNumbers",
                DisplayLabel = "Include Verse Numbers",
                DisplayDescription = "Whether to include the Verse Numbers when typing the verse.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = DefaultIncludeVerseNumbers
            },
            new PluginAdditionalOption()
            {
                Key = "NewLineBetweenChapters",
                DisplayLabel = "Include New-Line Between Chapters",
                DisplayDescription = "Whether to include a blank line between chapters when typing the verse.",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = DefaultNewLineBetweenChapters
            }
        };

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());

            string currPath = _context.CurrentPluginMetadata.ExecuteFilePath;
            _bibleXml_path = Path.GetDirectoryName(currPath) ?? "";

            RebuildVerseLink();
        }

        /// <summary>
        /// (Re)creates the VerseLink instance from the current version and format settings.
        /// VerseLink loads the version XML and captures the format object once, in its
        /// constructor, so it has to be rebuilt whenever either of those changes.
        /// </summary>
        private void RebuildVerseLink()
        {
            // Init supplies the path; until it has run there is nothing to load from.
            if (_bibleXml_path is null) return;

            _VL = new VerseLinkWindows.VerseLink(_bibleversion, _bibleXml_path, _BibleReferenceVerseFormat);
            if (_VL.Error)
            {
                string error = _VL.LastError;
                Log.Exception(error, new Exception("VerseLinkWindows.VerseLink(_bibleversion)"), this.GetType(), "RebuildVerseLink", "Main.cs", 0);
            }
        }

        /// <summary>
        /// Maps a BibleVersion combo box value onto a version name, falling back to the
        /// default for any value that is not in _bibleVersions.
        /// </summary>
        private string ResolveBibleVersion(int comboBoxValue)
        {
            // FirstOrDefault over KeyValuePair yields default(KeyValuePair), whose Key is null.
            return _bibleVersions.FirstOrDefault(x => x.Value == comboBoxValue.ToString()).Key ?? DefaultBibleVersion;
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

            if (_VL is null) return String.Empty;

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
            int bv = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "BibleVersion")?.ComboBoxValue ?? DefaultBibleVersionValue;
            _bibleversion = ResolveBibleVersion(bv);

            var format = new BibleReferenceVerseFormat();

            var ir = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "IncludeReference");
            format.IncludeReference = ir?.Value ?? DefaultIncludeReference;
            var ivn = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "IncludeVerseNumbers");
            format.IncludeVerseNumbers = ivn?.Value ?? DefaultIncludeVerseNumbers;
            var inlbc = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "NewLineBetweenChapters");
            format.IncludeNewLineBetweenChapters = inlbc?.Value ?? DefaultNewLineBetweenChapters;

            _BibleReferenceVerseFormat = format;

            // VerseLink caches the version XML and the format object, so it must be rebuilt.
            RebuildVerseLink();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;

            if (_context?.API is not null)
            {
                _context.API.ThemeChanged -= OnThemeChanged;
            }

            _disposed = true;
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
