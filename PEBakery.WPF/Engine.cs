﻿/*
    Copyright (C) 2016-2017 Hajin Jang
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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PEBakery.Lib;
using System.IO;
using PEBakery.Helper;
using PEBakery.Exceptions;

namespace PEBakery.Core
{
    /// <summary>
    /// How much information will be logged if an Exception is catched in ExecuteCommand?
    /// </summary>
    public enum DebugLevel
    {
        Production = 0, // Only Exception message
        PrintExceptionType = 1, // Print Exception message with Exception type
        PrintExceptionStackTrace = 2, // Print Exception message, type, and stack trace
    }

    public class Engine
    {
        public static DebugLevel DebugLevel = DebugLevel.PrintExceptionStackTrace;
        public EngineState s;

        public Engine(EngineState state)
        {
            s = state;
            s.Variables.LoadDefaultPluginVariables(s.CurrentPlugin);
        }

        /// <summary>
        /// Ready to run an plugin
        /// </summary>
        private void ReadyToRunPlugin(Plugin p = null)
        {
            // Turn off System,ErrorOff
            s.Logger.ErrorOffCount = 0;
            // Turn off System,Log,Off
            s.Logger.SuspendLog = false;

            if (p == null)
                p = s.CurrentPlugin;
            else
                s.CurrentPlugin = p;
            PluginSection section = p.Sections["Process"];
            s.Logger.BuildLog_Write(s.BuildId, $"Processing plugin [{p.ShortPath}] ({s.Plugins.IndexOf(p)}/{s.Plugins.Count})");

            s.Variables.ResetVariables(VarsType.Local);
            s.Variables.LoadDefaultPluginVariables(s.CurrentPlugin);

            // s.SectionParams = new Stack<List<string>>();
            s.CurSectionParams = new List<string>();
        }

        public void Build()
        {
            while (true)
            {
                ReadyToRunPlugin(s.CurrentPlugin);
                Engine.RunSection(s, new SectionAddress(s.CurrentPlugin, s.CurrentPlugin.Sections["Process"]), new List<string>(), 0, false);
                // End of Plugin
                s.Logger.BuildLog_Write(s.BuildId, $"End of plugin [{s.CurrentPlugin.ShortPath}]");
                try
                {
                    int curPluginIdx = s.Plugins.IndexOf(s.CurrentPlugin);
                    if (curPluginIdx + 1 < s.Plugins.Count)
                        s.NextPluginIdx = curPluginIdx + 1;
                }
                catch (EndOfPluginLevelException)
                { // End of plugins, build done. Exit.
                  // OnBuildExit event callback
                    Engine.CheckAndRunCallback(s, ref s.OnBuildExit, "OnBuildExit");
                    break;
                }
            }
        }

        #region LogStartOfSection, LogEndOfSection
        public static void LogStartOfSection(EngineState s, SectionAddress addr, int depth, bool logPluginName, CodeCommand cmd = null)
        {
            if (logPluginName)
                LogStartOfSection(s, addr.Section.SectionName, depth, cmd);
            else
                LogStartOfSection(s, addr.Plugin.ShortPath, addr.Section.SectionName, depth, cmd);
        }

        public static void LogStartOfSection(EngineState s, string sectionName, int depth, CodeCommand cmd = null)
        {
            string msg = $"Processing Section [{sectionName}]";
            if (cmd == null)
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, depth));
            else
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, cmd, depth));
        }

        public static void LogStartOfSection(EngineState s, string pluginName, string sectionName, int depth, CodeCommand cmd = null)
        {
            string msg = $"Processing [{pluginName}]'s Section [{sectionName}]";
            if (cmd == null)
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, depth));
            else
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, cmd, depth));
        }

        public static void LogEndOfSection(EngineState s, SectionAddress addr, int depth, bool logPluginName, CodeCommand cmd = null)
        {
            if (logPluginName)
                LogEndOfSection(s, addr.Section.SectionName, depth, cmd);
            else
                LogEndOfSection(s, addr.Plugin.ShortPath, addr.Section.SectionName, depth, cmd);
        }

        public static void LogEndOfSection(EngineState s, string sectionName, int depth, CodeCommand cmd = null)
        {
            string msg = $"End of Section [{sectionName}]";
            if (cmd == null)
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, depth));
            else
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, cmd, depth));
        }

        public static void LogEndOfSection(EngineState s, string pluginName, string sectionName, int depth, CodeCommand cmd = null)
        {
            string msg = $"End of [{pluginName}]'s Section [{sectionName}]";
            if (cmd == null)
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, depth));
            else
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, msg, cmd, depth));
        }
        #endregion

        public static void RunSection(EngineState s, SectionAddress addr, List<string> sectionParams, int depth, bool callback)
        {
            List<CodeCommand> codes = addr.Section.GetCodes(true);
            s.Logger.BuildLog_Write(s.BuildId, addr.Section.LogInfos);

            RunCommands(s, codes, sectionParams, depth, callback);
        }

        public static void RunCommands(EngineState s, List<CodeCommand> codes, List<string> sectionParams, int depth, bool callback = false)
        {
            CodeCommand curCommand = codes[0];
            for (int idx = 0; idx < codes.Count; idx++)
            {
                try
                {
                    curCommand = codes[idx];
                    s.CurDepth = depth;
                    s.CurSectionParams = sectionParams;
                    ExecuteCommand(s, curCommand);
                }
                catch (CriticalErrorException)
                { // Critical Error, stop build
                    break;
                }
            }
        }

        private static void CheckAndRunCallback(EngineState s, ref CodeCommand cbCmd, string eventName)
        {
            if (cbCmd != null)
            {
                s.Logger.BuildLog_Write(s.BuildId, $"Processing callback of event [{eventName}]");

                if (cbCmd.Type == CodeType.Run || cbCmd.Type == CodeType.Exec)
                {
                    s.CurDepth = -1;
                    CommandBranch.RunExec(s, cbCmd, false, true);
                }
                else
                {
                    s.CurDepth = 0;
                    ExecuteCommand(s, cbCmd);
                }
                s.Logger.BuildLog_Write(s.BuildId, new LogInfo(LogState.Info, $"End of callback [{eventName}]{Environment.NewLine}", s.CurDepth));
                cbCmd = null;
            }
        }

        private static void ExecuteCommand(EngineState s, CodeCommand cmd)
        {
            List<LogInfo> logs = null;
            try
            {
                switch (cmd.Type)
                {
                    #region 00 Misc
                    // 00 Misc
                    case CodeType.None:
                        logs = new List<LogInfo> { new LogInfo(LogState.Ignore, "NOP", cmd) };
                        break;
                    case CodeType.Comment:
                        logs = new List<LogInfo> { new LogInfo(LogState.Ignore, "Comment", cmd) };
                        break;
                    case CodeType.Error:
                        logs = new List<LogInfo> { new LogInfo(LogState.Error, "Error", cmd) };
                        break;
                    case CodeType.Unknown:
                        logs = new List<LogInfo> { new LogInfo(LogState.Ignore, "Unknown", cmd) };
                        break;
                    #endregion
                    /*
                    #region 01 File
                    // 01 File
                    case CodeType.CopyOrExpand:
                        break;
                    case CodeType.DirCopy:
                        break;
                    case CodeType.DirDelete:
                        break;
                    case CodeType.DirMove:
                        break;
                    case CodeType.DirMake:
                        break;
                    case CodeType.Expand:
                        break;
                    */
                    //case CodeType.FileCopy:
                    //    break;
                    /*
                    case CodeType.FileDelete:
                        break;
                    case CodeType.FileRename:
                        break;
                    case CodeType.FileMove:
                        break;
                    case CodeType.FileCreateBlank:
                        break;
                    case CodeType.FileByteExtract:
                        break;
                    #endregion
                    #region 02 Registry
                    // 02 Registry
                    case CodeType.RegHiveLoad:
                        break;
                    case CodeType.RegHiveUnload:
                        break;
                    case CodeType.RegImport:
                        break;
                    case CodeType.RegWrite:
                        break;
                    case CodeType.RegRead:
                        break;
                    case CodeType.RegDelete:
                        break;
                    case CodeType.RegWriteBin:
                        break;
                    case CodeType.RegReadBin:
                        break;
                    case CodeType.RegMulti:
                        break;
                    #endregion
                        */
                    #region 03 Text
                    // 03 Text
                    case CodeType.TXTAddLine:
                        logs = CommandText.TXTAddLine(s, cmd);
                        break;
                    case CodeType.TXTReplace:
                        logs = CommandText.TXTReplace(s, cmd);
                        break;
                    case CodeType.TXTDelLine:
                        logs = CommandText.TXTDelLine(s, cmd);
                        break;
                    case CodeType.TXTDelSpaces:
                        logs = CommandText.TXTDelSpaces(s, cmd);
                        break;
                    case CodeType.TXTDelEmptyLines:
                        logs = CommandText.TXTDelEmptyLines(s, cmd);
                        break;
                    #endregion
                    #region 04 INI
                    // 04 INI
                    case CodeType.INIRead:
                        logs = CommandINI.INIRead(s, cmd);
                        break;
                    case CodeType.INIWrite:
                        logs = CommandINI.INIWrite(s, cmd);
                        break;
                    case CodeType.INIDelete:
                        logs = CommandINI.INIDelete(s, cmd);
                        break;
                    case CodeType.INIAddSection:
                        logs = CommandINI.INIAddSection(s, cmd);
                        break;
                    case CodeType.INIDeleteSection:
                        logs = CommandINI.INIDeleteSection(s, cmd);
                        break;
                    //case CodeType.INIWriteTextLine:
                    //    break;
                    //case CodeType.INIMerge:
                    //    break;
                    #endregion
                    /*
                    #region 05 Network
                    // 05 Network
                    case CodeType.WebGet:
                        break;
                    case CodeType.WebGetIfNotExist:
                        break;
                    #endregion
                    #region 06 Attach, Interface
                    // 06 Attach, Interface
                    case CodeType.ExtractFile:
                        break;
                    case CodeType.ExtractAndRun:
                        break;
                    case CodeType.ExtractAllFiles:
                        break;
                    case CodeType.ExtractAllFilesIfNotExist:
                        break;
                    case CodeType.Encode:
                        break;
                    #endregion
                    #region 07 UI
                    // 07 UI*/
                    case CodeType.Message:
                        logs = CommandUI.Message(s, cmd);
                        break;
                    case CodeType.Echo:
                        logs = CommandUI.Echo(s, cmd);
                        break;
                        /*
                    case CodeType.Retrieve:
                        break;
                    case CodeType.Visible:
                        break;
                    #endregion
                    #region 08 StringFormat
                    // 08 StringFormat
                    case CodeType.StrFormat:
                        break;
                    #endregion
                    #region 09 System
                    // 09 System
                    case CodeType.System:
                        break;
                    case CodeType.ShellExecute:
                        break;
                    case CodeType.ShellExecuteEx:
                        break;
                    case CodeType.ShellExecuteDelete:
                        break;
                    #endregion
                    */
                    #region 10 Branch
                    // 10 Branch
                    case CodeType.Run:
                    case CodeType.Exec:
                        logs = new List<LogInfo>();
                        CommandBranch.RunExec(s, cmd);
                        break;
                    case CodeType.Loop:
                        logs = new List<LogInfo>();
                        CommandBranch.Loop(s, cmd);
                        break;
                    case CodeType.If:
                        logs = new List<LogInfo>();
                        CommandBranch.If(s, cmd);
                        break;
                    case CodeType.Else:
                        logs = new List<LogInfo>();
                        CommandBranch.Else(s, cmd);
                        break;
                    case CodeType.Begin:
                        throw new InternalParserException("[Begin] must have already parsed");
                    case CodeType.End:
                        throw new InternalParserException("[End] must have already parsed");
                    #endregion
                    #region 11 Control
                    // 11 Control
                    case CodeType.Set:
                        logs = CommandControl.Set(s, cmd);
                        break;
                    case CodeType.GetParam:
                        logs = CommandControl.GetParam(s, cmd);
                        break;
                    case CodeType.PackParam:
                        logs = CommandControl.PackParam(s, cmd);
                        break;
                    /*
                    case CodeType.AddVariables:
                        break;
                    case CodeType.Exit:
                        break;
                    case CodeType.Halt:
                        break;
                    case CodeType.Wait:
                        break;
                    case CodeType.Beep:
                        break;
                    */
                    #endregion
                    #region 12 External Macro
                    // 12 External Macro
                    case CodeType.Macro:
                        logs = new List<LogInfo>();
                        CommandMacro.Macro(s, cmd);
                        break;
                    #endregion
                    #region Error
                    // Error
                    default:
                        throw new InvalidCodeCommandException($"Cannot execute [{cmd.Type}] command", cmd);
                        #endregion
                }
            }
            catch (Exception e)
            {
                logs = new List<LogInfo>()
                {
                    new LogInfo(LogState.Error, e.Message, cmd, s.CurDepth),
                };
            }

            for (int i = 0; i < logs.Count; i++)
            {
                LogInfo log = logs[i];
                log.Depth = s.CurDepth;
                s.Logger.BuildLog_Write(s.BuildId, log);
            }
        }
    }

    public class EngineState
    {
        // Fields used globally
        public Project Project;
        public List<Plugin> Plugins;
        public Variables Variables { get => Project.Variables; }
        public Macro Macro;
        public Logger Logger;
        public bool RunOnePlugin;
        public DebugLevel DebugLevel;
        public long BuildId; // Used in logging

        // Properties
        public string BaseDir { get => Project.BaseDir; }
        public Plugin MainPlugin { get => Project.MainPlugin; }

        // Fields : Engine's state
        public Plugin CurrentPlugin;
        public int NextPluginIdx;
        public List<string> CurSectionParams;
        public int CurDepth;
        public bool RunElse;

        // Fields : System Commands
        public CodeCommand OnBuildExit;
        public CodeCommand OnPluginExit;

        public EngineState(DebugLevel debugLevel, Project project, Logger logger, Plugin pluginToRun = null)
        {
            this.DebugLevel = debugLevel;
            this.Project = project;
            this.Plugins = project.GetActivePluginList();
            this.Logger = logger;

            Macro = new Macro(Project, Variables, out List<LogInfo> macroLogs);
            logger.BuildLog_Write(BuildId, macroLogs);

            if (pluginToRun == null) // Run just plugin
            {
                CurrentPlugin = pluginToRun;
                NextPluginIdx = Plugins.IndexOf(pluginToRun);
                RunOnePlugin = true;
            }
            else
            {
                CurrentPlugin = Plugins[0]; // Main Plugin
                NextPluginIdx = 0;
                RunOnePlugin = false;
            }
                
            this.CurSectionParams = new List<string>();
            // this.SectionParams = new Stack<List<string>>();
            this.CurDepth = 0;
            this.RunElse = false;
            this.OnBuildExit = null;
            this.OnPluginExit = null;
        }
    }
}