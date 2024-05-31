// Copyright (c) Seth Twigg. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.InputTyper
{
    public class Main : IPlugin, ISettingProvider
    {
        private Typer _typer = new Typer();
        private PluginInitContext _context;
        private string _icon_path;
        private int _beginTypeDelay;

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
                        Task.Run(() => _typer!.Type(text, _beginTypeDelay));
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
                        Task.Run(() => RunAsSTAThread(() => _typer.TypeClipboard(_beginTypeDelay)));
                        return true;
                    }
                }) ;
            }

            return results;
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _icon_path = "Images/VerseLink.light.png";
            }
            else
            {
                _icon_path = "Images/VerseLink.dark.png";
            }
        }

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions is null)
            {
                return;
            }

            var typeDelay = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "BeginTypeDelay");
            _beginTypeDelay = (int)(typeDelay?.NumberValue ?? 200);
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
