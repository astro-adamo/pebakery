﻿/*
    Copyright (C) 2018 Hajin Jang
    Licensed under GPL 3.0
 
    PEBakery is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

    Additional permission under GNU GPL version 3 section 7

    If you modify this program, or any covered work, by linking
    or combining it with external libraries, containing parts
    covered by the terms of various license, the licensors of
    this program grant you additional permission to convey the
    resulting work. An external library is a library which is
    not derived from or based on this program. 
*/

using PEBakery.Helper;
using PEBakery.Ini;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using PEBakery.Core.ViewModels;

namespace PEBakery.Core
{
    #region Setting
    public class Setting
    {
        #region Subclasses
        public class ProjectSetting
        {
            public const string SectionName = "Project";

            public string DefaultProject;

            public ProjectSetting()
            {
                Default();
            }

            public void Default()
            {
                DefaultProject = string.Empty;
            }
        }

        public class GeneralSetting
        {
            public const string SectionName = "General";

            public bool OptimizeCode;
            public bool ShowLogAfterBuild;
            public bool StopBuildOnError;
            public bool EnableLongFilePath;
            public bool UseCustomUserAgent;
            public string CustomUserAgent;

            public GeneralSetting()
            {
                Default();
            }

            public void Default()
            {
                OptimizeCode = true;
                ShowLogAfterBuild = true;
                StopBuildOnError = true;
                EnableLongFilePath = false;
                UseCustomUserAgent = false;
                // Default custom User-Agent is set to Edge's on Windows 10 v1809
                CustomUserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36 Edge/18.17763";
                // Or Firefox 64?
                // Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:64.0) Gecko/20100101 Firefox/64.0
            }
        }

        public enum InterfaceSize
        {
            Adaptive = 0,
            Standard = 1,
            Small = 2,
        }

        public class InterfaceSetting
        {
            public const string SectionName = "Interface";

            public bool UseCustomTitle;
            public string CustomTitle;
            public bool UseCustomEditor;
            public string CustomEditorPath;
            public FontHelper.FontInfo MonospacedFont;
            public int ScaleFactor;
            public bool DisplayShellExecuteConOut;
            public InterfaceSize InterfaceSize;

            public FontFamily MonospacedFontFamily => MonospacedFont.FontFamily;
            public FontWeight MonospacedFontWeight => MonospacedFont.FontWeight;
            public int MonospacedFontSize => MonospacedFont.PointSize;

            public InterfaceSetting()
            {
                Default();
            }

            public void Default()
            {
                UseCustomTitle = false;
                CustomTitle = string.Empty;
                // Every Windows PC has notepad pre-installed.
                UseCustomEditor = false;
                CustomEditorPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "notepad.exe");
                // Every Windows PC has Consolas pre-installed.
                MonospacedFont = new FontHelper.FontInfo(new FontFamily("Consolas"), FontWeights.Regular, 12);
                ScaleFactor = 100;
                DisplayShellExecuteConOut = true;
                InterfaceSize = InterfaceSize.Adaptive;
            }
        }

        public enum ThemePreset
        {
            Black = 0,
            Red = 1,
            Green = 2,
            Blue = 3,
        }

        public class ThemeSetting
        {
            public const string SectionName = "Theme";

            // Preset
            public ThemePreset Preset;
            // Custom
            public bool UseCustomTheme;
            public Color CustomTopPanelBackground;
            public Color CustomTopPanelForeground;
            public Color CustomTreeViewBackground;
            public Color CustomTreeViewForeground;
            public Color CustomTreeViewHighlightForeground;
            public Color CustomScriptDescBackground;
            public Color CustomScriptDescForeground;
            public Color CustomStatusBarBackground;
            public Color CustomStatusBarForeground;

            public ThemeSetting()
            {
                Default();
            }

            public void Default()
            {
                Preset = ThemePreset.Black;
                UseCustomTheme = false;
                CustomTopPanelBackground = Colors.Black;
                CustomTopPanelForeground = Color.FromRgb(238, 238, 238);
                CustomTreeViewBackground = Color.FromRgb(204, 204, 204);
                CustomTreeViewForeground = Colors.Black;
                CustomTreeViewHighlightForeground = Colors.Red;
                CustomScriptDescBackground = Color.FromRgb(238, 238, 238);
                CustomScriptDescForeground = Colors.Black;
                CustomStatusBarBackground = Color.FromRgb(238, 238, 238);
                CustomStatusBarForeground = Colors.Black;
            }

            #region Properties
            public Color TopPanelBackground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomTopPanelBackground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                            return Colors.Black;
                        case ThemePreset.Red:
                            return Colors.DarkRed;
                        case ThemePreset.Green:
                            return Colors.DarkGreen;
                        case ThemePreset.Blue:
                            return Colors.DarkBlue;
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color TopPanelForeground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomTopPanelForeground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                            return Color.FromRgb(238, 238, 238);
                        case ThemePreset.Red:
                            return Colors.White;
                        case ThemePreset.Green:
                            return Colors.White;
                        case ThemePreset.Blue:
                            return Colors.White;
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color TreeViewBackground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomTreeViewBackground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                            return Color.FromRgb(204, 204, 204);
                        case ThemePreset.Red:
                            return Color.FromRgb(255, 211, 94);
                        case ThemePreset.Green:
                            return Color.FromRgb(180, 255, 180);
                        case ThemePreset.Blue:
                            return Color.FromRgb(180, 180, 255);
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color TreeViewForeground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomTreeViewForeground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                        case ThemePreset.Red:
                        case ThemePreset.Green:
                        case ThemePreset.Blue:
                            return Colors.Black;
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color TreeViewHighlightForeground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomTreeViewHighlightForeground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                            return Colors.Red;
                        case ThemePreset.Red:
                            return Colors.DarkRed;
                        case ThemePreset.Green:
                            return Colors.DarkGreen;
                        case ThemePreset.Blue:
                            return Colors.DarkBlue;
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color ScriptDescBackground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomScriptDescBackground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                            return Color.FromRgb(238, 238, 238);
                        case ThemePreset.Red:
                            return Color.FromRgb(255, 255, 192);
                        case ThemePreset.Green:
                            return Color.FromRgb(230, 255, 230);
                        case ThemePreset.Blue:
                            return Color.FromRgb(230, 230, 255);
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color ScriptDescForeground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomScriptDescForeground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                        case ThemePreset.Red:
                        case ThemePreset.Green:
                        case ThemePreset.Blue:
                            return Colors.Black;
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color StatusBarBackground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomStatusBarBackground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                            return Color.FromRgb(238, 238, 238);
                        case ThemePreset.Red:
                            return Color.FromRgb(255, 232, 208);
                        case ThemePreset.Green:
                            return Color.FromRgb(210, 255, 210);
                        case ThemePreset.Blue:
                            return Color.FromRgb(210, 210, 255);
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            public Color StatusBarForeground
            {
                get
                {
                    if (UseCustomTheme)
                        return CustomStatusBarForeground;
                    switch (Preset)
                    {
                        case ThemePreset.Black:
                        case ThemePreset.Red:
                        case ThemePreset.Green:
                        case ThemePreset.Blue:
                            return Colors.Black;
                        default:
                            throw new InvalidOperationException("Undefined theme preset");
                    }
                }
            }
            #endregion
        }

        public class ScriptSetting
        {
            public const string SectionName = "Script";

            public bool EnableCache;
            public bool AutoSyntaxCheck;

            public ScriptSetting()
            {
                Default();
            }

            public void Default()
            {
                // Script
                EnableCache = true;
                AutoSyntaxCheck = true;
            }
        }

        public class LogSetting
        {
            public const string SectionName = "Log";

            public LogDebugLevel DebugLevel;
            public bool DeferredLogging;
            public bool MinifyHtmlExport;

            public LogSetting()
            {
                Default();
            }

            public void Default()
            {
#if DEBUG
                DebugLevel = LogDebugLevel.PrintExceptionStackTrace;
#else
                DebugLevel = LogDebugLevel.Production;
#endif
                DeferredLogging = true;
                MinifyHtmlExport = true;
            }
        }
        #endregion

        #region Fields and Properties
        private readonly string _settingFile;

        public ProjectSetting Project { get; }
        public GeneralSetting General { get; }
        public InterfaceSetting Interface { get; }
        public ThemeSetting Theme { get; }
        public ScriptSetting Script { get; }
        public LogSetting Log { get; }
        #endregion

        #region Constructor
        public Setting(string settingFile)
        {
            _settingFile = settingFile;

            Project = new ProjectSetting();
            General = new GeneralSetting();
            Interface = new InterfaceSetting();
            Theme = new ThemeSetting();
            Script = new ScriptSetting();
            Log = new LogSetting();

            ReadFromFile();
        }
        #endregion

        #region ApplySetting
        public void ApplySetting()
        {
            // AppContext
            // Enabled  = Path Length Limit = 32767
            // Disabled = Path Length Limit = 260
            AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling", !General.EnableLongFilePath);

            // Static
            Engine.StopBuildOnError = General.StopBuildOnError;
            Logger.DebugLevel = Log.DebugLevel;
            Logger.MinifyHtmlExport = Log.MinifyHtmlExport;

            // MainViewModel (Without Theme)
            Global.MainViewModel.TitleBar = Interface.UseCustomTitle ? Interface.CustomTitle : MainViewModel.DefaultTitleBar;
            Global.MainViewModel.MonospacedFont = Interface.MonospacedFont;
            Global.MainViewModel.DisplayShellExecuteConOut = Interface.DisplayShellExecuteConOut;
            Global.MainViewModel.InterfaceSize = Interface.InterfaceSize;

            // MainViewModel (Theme)
            Global.MainViewModel.TopPanelBackground = Theme.TopPanelBackground;
            Global.MainViewModel.TopPanelForeground = Theme.TopPanelForeground;
            Global.MainViewModel.TreeViewBackground = Theme.TreeViewBackground;
            Global.MainViewModel.TreeViewForeground = Theme.TreeViewForeground;
            Global.MainViewModel.TreeViewHighlightForeground = Theme.TreeViewHighlightForeground;
            Global.MainViewModel.ScriptDescBackground = Theme.ScriptDescBackground;
            Global.MainViewModel.ScriptDescForeground = Theme.ScriptDescForeground;
            Global.MainViewModel.StatusBarBackground = Theme.StatusBarBackground;
            Global.MainViewModel.StatusBarForeground = Theme.StatusBarForeground;
        }
        #endregion

        #region SetToDefault
        public void SetToDefault()
        {
            Project.Default();
            General.Default();
            Interface.Default();
            Script.Default();
            Log.Default();
        }
        #endregion

        #region ReadFromFile, WriteToFile
        public void ReadFromFile()
        {
            // Use default value if key/value does not exist or malformed.
            SetToDefault();

            if (!File.Exists(_settingFile))
                return;

            IniKey[] keys =
            {
                // Project
                new IniKey(ProjectSetting.SectionName, nameof(Project.DefaultProject)), // String
                // General
                new IniKey(GeneralSetting.SectionName, nameof(General.OptimizeCode)), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.ShowLogAfterBuild)), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.StopBuildOnError)), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.EnableLongFilePath)), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.UseCustomUserAgent)), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.CustomUserAgent)), // String
                // Interface
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.UseCustomTitle)), // Boolean
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.CustomTitle)), // String
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.UseCustomEditor)), // Boolean
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.CustomEditorPath)), // String
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.MonospacedFontFamily)), // FontFamily
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.MonospacedFontWeight)), // FontWeight
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.MonospacedFontSize)), // FontSize
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.ScaleFactor)), // Integer (70 - 200)
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.DisplayShellExecuteConOut)), // Boolean
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.InterfaceSize)), // Integer (0 - 2)
                // Theme
                new IniKey(ThemeSetting.SectionName, nameof(Theme.Preset)), // Integer
                new IniKey(ThemeSetting.SectionName, nameof(Theme.UseCustomTheme)), // Boolean
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTopPanelBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTopPanelForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTreeViewBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTreeViewForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTreeViewHighlightForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomScriptDescBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomScriptDescForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomStatusBarBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomStatusBarForeground)), // Color
                // Script
                new IniKey(ScriptSetting.SectionName, nameof(Script.EnableCache)), // Boolean
                new IniKey(ScriptSetting.SectionName, nameof(Script.AutoSyntaxCheck)), // Boolean
                // Log
                new IniKey(LogSetting.SectionName, nameof(Log.DebugLevel)), // Integer (0 - 2)
                new IniKey(LogSetting.SectionName, nameof(Log.DeferredLogging)), // Boolean
                new IniKey(LogSetting.SectionName, nameof(Log.MinifyHtmlExport)), // Boolean
            };
            keys = IniReadWriter.ReadKeys(_settingFile, keys);
            Dictionary<string, Dictionary<string, string>> keyDict = keys
                .GroupBy(x => x.Section)
                .ToDictionary(
                    x => x.Key, 
                    y => y.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            // Project
            if (keyDict.ContainsKey(ProjectSetting.SectionName))
            {
                Dictionary<string, string> projectDict = keyDict[ProjectSetting.SectionName];

                Project.DefaultProject = DictParser.ParseString(projectDict, nameof(Project.DefaultProject), string.Empty);
            }

            // General
            if (keyDict.ContainsKey(GeneralSetting.SectionName))
            {
                Dictionary<string, string> generalDict = keyDict[GeneralSetting.SectionName];

                General.OptimizeCode = DictParser.ParseBoolean(generalDict, GeneralSetting.SectionName, nameof(General.OptimizeCode), General.OptimizeCode);
                General.ShowLogAfterBuild = DictParser.ParseBoolean(generalDict, GeneralSetting.SectionName, nameof(General.ShowLogAfterBuild), General.ShowLogAfterBuild);
                General.StopBuildOnError = DictParser.ParseBoolean(generalDict, GeneralSetting.SectionName, nameof(General.StopBuildOnError), General.StopBuildOnError);
                General.EnableLongFilePath = DictParser.ParseBoolean(generalDict, GeneralSetting.SectionName, nameof(General.EnableLongFilePath), General.EnableLongFilePath);
                General.UseCustomUserAgent = DictParser.ParseBoolean(generalDict, GeneralSetting.SectionName, nameof(General.UseCustomUserAgent), General.UseCustomUserAgent);
                General.CustomUserAgent = DictParser.ParseString(generalDict, nameof(General.CustomUserAgent), General.CustomUserAgent);
            }

            // Interface
            if (keyDict.ContainsKey(InterfaceSetting.SectionName))
            {
                Dictionary<string, string> ifaceDict = keyDict[InterfaceSetting.SectionName];

                // Parse MonospacedFont
                FontFamily monoFontFamily = Interface.MonospacedFont.FontFamily;
                FontWeight monoFontWeight = Interface.MonospacedFont.FontWeight;
                if (ifaceDict[nameof(Interface.MonospacedFontFamily)] != null)
                    monoFontFamily = new FontFamily(ifaceDict[nameof(Interface.MonospacedFontFamily)]);
                if (ifaceDict[nameof(Interface.MonospacedFontWeight)] != null)
                    monoFontWeight = FontHelper.ParseFontWeight(ifaceDict[nameof(Interface.MonospacedFontWeight)]);
                int monoFontSize = DictParser.ParseInteger(ifaceDict, InterfaceSetting.SectionName, nameof(Interface.MonospacedFontSize), Interface.MonospacedFont.PointSize, 1, -1);
                Interface.MonospacedFont = new FontHelper.FontInfo(monoFontFamily, monoFontWeight, monoFontSize);

                Interface.UseCustomTitle = DictParser.ParseBoolean(ifaceDict, InterfaceSetting.SectionName, nameof(Interface.UseCustomTitle), Interface.UseCustomTitle);
                Interface.CustomTitle = DictParser.ParseString(ifaceDict, nameof(Interface.CustomTitle), Interface.CustomTitle);
                Interface.UseCustomEditor = DictParser.ParseBoolean(ifaceDict, InterfaceSetting.SectionName, nameof(Interface.UseCustomEditor), Interface.UseCustomEditor);
                Interface.CustomEditorPath = DictParser.ParseString(ifaceDict, nameof(Interface.CustomEditorPath), Interface.CustomEditorPath);
                Interface.ScaleFactor = DictParser.ParseInteger(ifaceDict, InterfaceSetting.SectionName, nameof(Interface.ScaleFactor), Interface.ScaleFactor, 70, 200);
                Interface.DisplayShellExecuteConOut = DictParser.ParseBoolean(ifaceDict, InterfaceSetting.SectionName, nameof(Interface.DisplayShellExecuteConOut), Interface.DisplayShellExecuteConOut);
                Interface.InterfaceSize = (InterfaceSize)DictParser.ParseInteger(ifaceDict, InterfaceSetting.SectionName, nameof(Interface.InterfaceSize), (int)Interface.InterfaceSize, 0, Enum.GetValues(typeof(InterfaceSize)).Length - 1);
            }

            // Theme
            if (keyDict.ContainsKey(ThemeSetting.SectionName))
            {
                Dictionary<string, string> scDict = keyDict[ThemeSetting.SectionName];

                Theme.Preset = (ThemePreset)DictParser.ParseInteger(scDict, ThemeSetting.SectionName, nameof(Theme.Preset), (int)Theme.Preset, 0, Enum.GetValues(typeof(ThemePreset)).Length - 1);
                Theme.UseCustomTheme = DictParser.ParseBoolean(scDict, ThemeSetting.SectionName, nameof(Theme.UseCustomTheme), Theme.UseCustomTheme);
                Theme.CustomTopPanelBackground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomTopPanelBackground), Theme.CustomTopPanelBackground);
                Theme.CustomTopPanelForeground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomTopPanelForeground), Theme.CustomTopPanelForeground);
                Theme.CustomTreeViewBackground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomTreeViewBackground), Theme.CustomTreeViewBackground);
                Theme.CustomTreeViewForeground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomTreeViewForeground), Theme.CustomTreeViewForeground);
                Theme.CustomTreeViewHighlightForeground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomTreeViewHighlightForeground), Theme.CustomTreeViewHighlightForeground);
                Theme.CustomScriptDescBackground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomScriptDescBackground), Theme.CustomScriptDescBackground);
                Theme.CustomScriptDescForeground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomScriptDescForeground), Theme.CustomScriptDescForeground);
                Theme.CustomStatusBarBackground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomStatusBarBackground), Theme.CustomStatusBarBackground);
                Theme.CustomStatusBarForeground = DictParser.ParseColor(scDict, ThemeSetting.SectionName, nameof(Theme.CustomStatusBarForeground), Theme.CustomStatusBarForeground);
            }

            // Script
            if (keyDict.ContainsKey(ScriptSetting.SectionName))
            {
                Dictionary<string, string> scDict = keyDict[ScriptSetting.SectionName];

                Script.EnableCache = DictParser.ParseBoolean(scDict, ScriptSetting.SectionName, nameof(Script.EnableCache), Script.EnableCache);
                Script.AutoSyntaxCheck = DictParser.ParseBoolean(scDict, ScriptSetting.SectionName, nameof(Script.AutoSyntaxCheck), Script.AutoSyntaxCheck);
            }

            // Log
            if (keyDict.ContainsKey(LogSetting.SectionName))
            {
                Dictionary<string, string> logDict = keyDict[LogSetting.SectionName];

                Log.DebugLevel = (LogDebugLevel)DictParser.ParseInteger(logDict, LogSetting.SectionName, nameof(Log.DebugLevel), (int)Log.DebugLevel, 0, Enum.GetValues(typeof(LogDebugLevel)).Length - 1);
                Log.DeferredLogging = DictParser.ParseBoolean(logDict, LogSetting.SectionName, nameof(Log.DeferredLogging), Log.DeferredLogging);
                Log.MinifyHtmlExport = DictParser.ParseBoolean(logDict, LogSetting.SectionName, nameof(Log.MinifyHtmlExport), Log.MinifyHtmlExport);
            }
        }

        public void WriteToFile()
        {
            string WriteColor(Color c) => $"{c.R}, {c.G}, {c.B}";

            IniKey[] keys =
            {
                // Project
                new IniKey(ProjectSetting.SectionName, nameof(Project.DefaultProject), Project.DefaultProject), // String
                // General
                new IniKey(GeneralSetting.SectionName, nameof(General.EnableLongFilePath), General.EnableLongFilePath.ToString()), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.OptimizeCode), General.OptimizeCode.ToString()), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.ShowLogAfterBuild), General.ShowLogAfterBuild.ToString()), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.StopBuildOnError), General.StopBuildOnError.ToString()), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.UseCustomUserAgent), General.UseCustomUserAgent.ToString()), // Boolean
                new IniKey(GeneralSetting.SectionName, nameof(General.CustomUserAgent), General.CustomUserAgent), // String
                // Interface
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.UseCustomTitle), Interface.UseCustomTitle.ToString()), // Boolean
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.CustomTitle), Interface.CustomTitle), // String
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.UseCustomEditor), Interface.UseCustomEditor.ToString()), // Boolean
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.CustomEditorPath), Interface.CustomEditorPath), // String
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.MonospacedFontFamily), Interface.MonospacedFont.FontFamily.Source),
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.MonospacedFontWeight), Interface.MonospacedFont.FontWeight.ToString()),
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.MonospacedFontSize), Interface.MonospacedFont.PointSize.ToString()),
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.ScaleFactor), Interface.ScaleFactor.ToString(CultureInfo.InvariantCulture)), // Integer
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.DisplayShellExecuteConOut), Interface.DisplayShellExecuteConOut.ToString()), // Boolean
                new IniKey(InterfaceSetting.SectionName, nameof(Interface.InterfaceSize), ((int)Interface.InterfaceSize).ToString()), // Integer
                // Theme
                new IniKey(ThemeSetting.SectionName, nameof(Theme.Preset), ((int)Theme.Preset).ToString()), // Integer
                new IniKey(ThemeSetting.SectionName, nameof(Theme.UseCustomTheme), Theme.UseCustomTheme.ToString()), // Boolean
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTopPanelBackground), WriteColor(Theme.CustomTopPanelBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTopPanelForeground), WriteColor(Theme.CustomTopPanelForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTreeViewBackground), WriteColor(Theme.CustomTreeViewBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTreeViewForeground), WriteColor(Theme.CustomTreeViewForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomTreeViewHighlightForeground), WriteColor(Theme.CustomTreeViewHighlightForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomScriptDescBackground), WriteColor(Theme.CustomScriptDescBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomScriptDescForeground), WriteColor(Theme.CustomScriptDescForeground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomStatusBarBackground), WriteColor(Theme.CustomStatusBarBackground)), // Color
                new IniKey(ThemeSetting.SectionName, nameof(Theme.CustomStatusBarForeground), WriteColor(Theme.CustomStatusBarForeground)), // Color
                // Script
                new IniKey(ScriptSetting.SectionName, nameof(Script.EnableCache), Script.EnableCache.ToString()), // Boolean
                new IniKey(ScriptSetting.SectionName, nameof(Script.AutoSyntaxCheck), Script.AutoSyntaxCheck.ToString()), // Boolean
                // Log
                new IniKey(LogSetting.SectionName, nameof(Log.DebugLevel), ((int)Log.DebugLevel).ToString()), // Integer
                new IniKey(LogSetting.SectionName, nameof(Log.DeferredLogging), Log.DeferredLogging.ToString()), // Boolean
                new IniKey(LogSetting.SectionName, nameof(Log.MinifyHtmlExport), Log.MinifyHtmlExport.ToString()), // Boolean
            };
            IniReadWriter.WriteKeys(_settingFile, keys);
        }
        #endregion
    }
    #endregion

    #region DictParser
    public static class DictParser
    {
        public static string ParseString(Dictionary<string, string> dict, string key, string defaultValue)
        {
            return dict[key] ?? defaultValue;
        }

        public static bool ParseBoolean(Dictionary<string, string> dict, string key, bool defaultValue)
        {
            string valStr = dict[key];
            if (valStr == null) // No warning, just use default value
                return defaultValue;

            if (valStr.Equals("True", StringComparison.OrdinalIgnoreCase))
                return true;
            if (valStr.Equals("False", StringComparison.OrdinalIgnoreCase))
                return false;

            return defaultValue;
        }

        public static bool ParseBoolean(Dictionary<string, string> dict, string section, string key, bool defaultValue)
        {
            string valStr = dict[key];
            if (valStr == null) // No warning, just use default value
                return defaultValue;

            if (valStr.Equals("True", StringComparison.OrdinalIgnoreCase))
                return true;
            if (valStr.Equals("False", StringComparison.OrdinalIgnoreCase))
                return false;

            Global.Logger.SystemWrite(new LogInfo(LogState.Error, $"Setting [{section}.{key}] has wrong value: {valStr}"));
            return defaultValue;
        }

        public static int ParseInteger(Dictionary<string, string> dict, string key, int defaultValue, int min, int max)
        {
            string valStr = dict[key];
            if (valStr == null) // No warning, just use default value
                return defaultValue;

            if (int.TryParse(valStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valInt))
            {
                if (min == -1)
                { // No Min
                    if (max == -1) // No Max
                        return valInt;
                    if (valInt <= max) // Have Min
                        return valInt;
                }
                else
                { // Have Min
                    if (max == -1 && min <= valInt) // No Max
                        return valInt;
                    if (min <= valInt && valInt <= max) // Have Min
                        return valInt;
                }
            }

            return defaultValue;
        }

        public static int ParseInteger(Dictionary<string, string> dict, string section, string key, int defaultValue, int min, int max)
        {
            string valStr = dict[key];
            if (valStr == null) // No warning, just use default value
                return defaultValue;

            if (int.TryParse(valStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valInt))
            {
                if (min == -1)
                { // No Min
                    if (max == -1) // No Max
                        return valInt;
                    if (valInt <= max) // Have Min
                        return valInt;
                }
                else
                { // Have Min
                    if (max == -1 && min <= valInt) // No Max
                        return valInt;
                    if (min <= valInt && valInt <= max) // Have Min
                        return valInt;
                }
            }

            Global.Logger.SystemWrite(new LogInfo(LogState.Error, $"Setting [{section}.{key}] has wrong value: {valStr}"));
            return defaultValue;
        }

        public static Color ParseColor(Dictionary<string, string> dict, string section, string key, Color defaultValue)
        {
            string valStr = dict[key];
            if (valStr == null) // No warning, just use default value
                return defaultValue;

            // Format = R, G, B (in base 10)
            string[] colorStrs = valStr.Split(',').Select(x => x.Trim()).ToArray();
            if (colorStrs.Length != 3)
            {
                Global.Logger.SystemWrite(new LogInfo(LogState.Error, $"Setting [{section}.{key}] has wrong value: {valStr}"));
                return defaultValue;
            }

            byte[] c = new byte[3]; // R, G, B
            for (int i = 0; i < 3; i++)
            {
                string colorStr = colorStrs[i];
                if (!byte.TryParse(colorStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte valByte))
                {
                    char ch;
                    switch (i)
                    {
                        case 0: // Red
                            ch = 'R';
                            break;
                        case 1: // Green
                            ch = 'G';
                            break;
                        case 2: // Blue
                            ch = 'B';
                            break;
                        default: // Unknown
                            ch = 'U';
                            break;
                    }
                    Debug.Assert(ch != 'U', "Unknown color parsing index");
                    Global.Logger.SystemWrite(new LogInfo(LogState.Error, $"Setting [{section}.{key}] has wrong [{ch}] value: {colorStr}"));
                    return defaultValue;
                }
                c[i] = valByte;
            }

            return Color.FromRgb(c[0], c[1], c[2]);
        }
    }
    #endregion
}
