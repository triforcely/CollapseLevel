using CollapseLevel.Core;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace CollapseLevel
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CollapseLevel
    {
        /// <summary>
        /// Command ID. Maps command number to the internal command id. <see cref="MenuItemCallback"/>
        /// </summary>
        public static readonly Dictionary<int, int> CommandIdMapping = new Dictionary<int, int>()
        {
            {0x101, 1},
            {0x102, 2},
            {0x103, 3},
            {0x104, 4},
            {0x105, 5},
            {0x106, 6},
            {0x107, 7},
            {0x108, 8},
            {0x109, 9},
        };

        /// <summary>
        /// Command menu group.
        /// </summary>
        public static readonly Guid CommandSet = new Guid("40b66aa9-8017-4fb7-a3b5-765d2f97b2b1");

        private const int MaxCollapseLevel = 30;

        /// <summary>
        /// VS Package that provides this command.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Text manager to get information about active views.
        /// </summary>
        private readonly IVsTextManager textManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseLevel"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        private CollapseLevel(Package package, IVsTextManager textManager)
        {
            this.package = package ?? throw new ArgumentNullException("package");
            this.textManager = textManager;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                foreach (var pair in CommandIdMapping)
                {
                    var menuCommandID = new CommandID(CommandSet, pair.Key);
                    var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                    commandService.AddCommand(menuItem);
                }
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CollapseLevel Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Keep information about last input for "Custom level" command.
        /// </summary>
        private int? LastCustomLevel { get; set; }

        private IOutliningManager OutliningManager { get; set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Visual Studio's status bar. Allows to display status.
        /// </summary>
        private IVsStatusbar StatusBar
        {
            get
            {
                return (IVsStatusbar)ServiceProvider.GetService(typeof(SVsStatusbar));
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package, IVsTextManager textManager)
        {
            Instance = new CollapseLevel(package, textManager);
        }

        /// <summary>
        /// Walk through all the regions, calculate their levels and collapse all regions where n = level +1.
        /// </summary>
        private void Collapse(int level)
        {
            var tv = TextViewRegistry.GetActiveTextView(this.textManager);

            if (tv == null)
                return;

            var componentModel = this.ServiceProvider.GetService(typeof(SCompon`entModel)) as IComponentModel;

            if (componentModel != null)
            {
                var outliningManagerService = componentModel.GetService<IOutliningManagerService>();

                if (outliningManagerService != null)
                    OutliningManager = outliningManagerService.GetOutliningManager(tv);
            }

            if (OutliningManager != null)
            {
                try
                {
                    var length = tv.TextSnapshot.Length;
                    var textViewSpan = new SnapshotSpan(tv.TextSnapshot, 0, length);
                    var regions = OutliningManager.GetAllRegions(textViewSpan);

                    Dictionary<ICollapsible, int> regionCount = new Dictionary<ICollapsible, int>();

                    foreach (var region in regions)
                    {
                        var extent = region.Extent;
                        var span = extent.GetSpan(tv.TextSnapshot);

                        foreach (var compareTo in regions)
                        {
                            if (compareTo.Equals(region))
                                continue;

                            if (!regionCount.ContainsKey(region))
                                regionCount[region] = 1;

                            var compareToSpan = compareTo.Extent.GetSpan(tv.TextSnapshot);
                            var regionSpan = region.Extent.GetSpan(tv.TextSnapshot);

                            if (compareToSpan.Contains(regionSpan))
                            {
                                regionCount[region] = regionCount[region] + 1;
                            }
                        }
                    }

                    OutliningManager.ExpandAll(textViewSpan, (y) => (true));

                    foreach (var pair in regionCount)
                    {
                        if (pair.Value == level)
                        {
                            OutliningManager.TryCollapse(pair.Key);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // TODO Investigate, happened once while testing some early version.
                }
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            MenuCommand command = sender as MenuCommand;
            int commandId = CommandIdMapping[command.CommandID.ID]; // Find folding level

            switch (commandId)
            {
                case 9: // Custom level
                    var level = PromptForCustomLevel();

                    if (level != null)
                    {
                        Collapse(level.Value);
                        NotifySuccess(level.Value);
                    }
                    break;

                default: // Levels mapped in *.vsct file - in this case commandId is the level.
                    Collapse(commandId);
                    NotifySuccess(commandId);
                    break;
            }
        }

        private void NotifySuccess(int level)
        {
            StatusBar.SetText($"Collapse Level: Collapsed everything above level {level} .");
        }

        private int? PromptForCustomLevel()
        {
            int level = LastCustomLevel ?? 3;

            var response = Microsoft.VisualBasic.Interaction.InputBox("Please specify collapse level", "Collapse Level", level.ToString(), -1, -1);

            int readLevel;

            if (string.IsNullOrWhiteSpace(response) || !int.TryParse(response, out readLevel) || !(readLevel >= 0 && readLevel <= MaxCollapseLevel))
            {
                StatusBar.SetText($"Collapse Level: Please specify number between 1 - {MaxCollapseLevel}");
                return null;
            }

            LastCustomLevel = readLevel;
            return readLevel;
        }
    }
}