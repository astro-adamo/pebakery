﻿/*
    Copyright (C) 2016-2018 Hajin Jang
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PEBakery.IniLib;
using System.Text.RegularExpressions;

namespace PEBakery.Core
{
    public class Macro
    {
        #region Field and Property
        public bool MacroEnabled { get; }
        /// <summary>
        /// %API% of sciprt.project
        /// </summary>
        public Script MacroScript { get; }
        /// <summary>
        /// %APIVAR% of sciprt.project
        /// </summary>
        public ScriptSection MacroSection { get; }
        /// <summary>
        /// [ApiVar] of macro script
        /// </summary>
        public Dictionary<string, CodeCommand> MacroDict { get; }
            = new Dictionary<string, CodeCommand>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Macro defined in current script's [Variables] 
        /// </summary>
        public Dictionary<string, CodeCommand> LocalDict { get; private set; }
            = new Dictionary<string, CodeCommand>(StringComparer.OrdinalIgnoreCase);

        public const string MacroNameRegex = @"^([a-zA-Z0-9_]+)$";
        #endregion

        #region Constructor
        public Macro(Project project, Variables variables, out List<LogInfo> logs)
        {
            logs = new List<LogInfo>();

            MacroEnabled = true;
            if (!project.MainScript.Sections.ContainsKey("Variables"))
            {
                MacroEnabled = false;
                logs.Add(new LogInfo(LogState.Info, "Macro not defined"));
                return;
            }

            Dictionary<string, string> varDict = Ini.ParseIniLinesVarStyle(project.MainScript.Sections["Variables"].GetLines());
            if (!(varDict.ContainsKey("API") && varDict.ContainsKey("APIVAR")))
            {
                MacroEnabled = false;
                logs.Add(new LogInfo(LogState.Info, "Macro not defined"));
                return;
            }

            // Get macroScript
            string rawScriptPath = varDict["API"];
            string macroScriptPath = variables.Expand(varDict["API"]); // Need expansion
            MacroScript = project.AllScripts.Find(x => x.RealPath.Equals(macroScriptPath, StringComparison.OrdinalIgnoreCase));
            if (MacroScript == null)
            {
                MacroEnabled = false;
                logs.Add(new LogInfo(LogState.Error, $"Macro defined but unable to find macro script [{rawScriptPath}"));
                return;
            }

            // Get macroScript
            if (!MacroScript.Sections.ContainsKey(varDict["APIVAR"]))
            {
                MacroEnabled = false;
                logs.Add(new LogInfo(LogState.Error, $"Macro defined but unable to find macro section [{varDict["APIVAR"]}"));
                return;
            }
            MacroSection = MacroScript.Sections[varDict["APIVAR"]];
            variables.SetValue(VarsType.Global, "API", macroScriptPath);
            if (MacroScript.Sections.ContainsKey(Variables.VarSectionName))
                variables.AddVariables(VarsType.Global, MacroScript.Sections[Variables.VarSectionName]);

            // Import Section [APIVAR]'s variables, such as '%Shc_Mode%=0'
            variables.AddVariables(VarsType.Global, MacroSection);

            // Parse Section [APIVAR] into MacroDict
            {
                SectionAddress addr = new SectionAddress(MacroScript, MacroSection);
                Dictionary<string, string> rawDict = Ini.ParseIniLinesIniStyle(MacroSection.GetLines());
                foreach (var kv in rawDict)
                {
                    try
                    {
                        if (Regex.Match(kv.Key, MacroNameRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant).Success) // Macro Name Validation
                            MacroDict[kv.Key] = CodeParser.ParseStatement(kv.Value, addr);
                        else
                            logs.Add(new LogInfo(LogState.Error, $"Invalid macro name [{kv.Key}]"));
                    }
                    catch (Exception e)
                    {
                        logs.Add(new LogInfo(LogState.Error, e));
                    }
                }
            }

            // Parse MainScript's section [Variables] into MacroDict
            // (Written by SetMacro, ... ,PERMANENT
            if (project.MainScript.Sections.ContainsKey(Variables.VarSectionName))
            {
                ScriptSection permaSection = project.MainScript.Sections[Variables.VarSectionName];
                SectionAddress addr = new SectionAddress(project.MainScript, permaSection);
                Dictionary<string, string> rawDict = Ini.ParseIniLinesIniStyle(permaSection.GetLines());
                foreach (var kv in rawDict)
                {
                    try
                    {
                        // Macro Name Validation
                        if (Regex.Match(kv.Key, MacroNameRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant).Success)
                            MacroDict[kv.Key] = CodeParser.ParseStatement(kv.Value, addr);
                        else
                            logs.Add(new LogInfo(LogState.Error, $"Invalid macro name [{kv.Key}]"));
                    }
                    catch (Exception e)
                    {
                        logs.Add(new LogInfo(LogState.Error, e));
                    }
                }
            }
        }
        #endregion

        public List<LogInfo> LoadLocalMacroDict(Script sc, bool append, string sectionName = Variables.VarSectionName)
        {
            if (sc.Sections.ContainsKey(sectionName))
            {
                ScriptSection section = sc.Sections[sectionName];

                // [Variables]'s type is SectionDataType.Lines
                // Pick key-value only if key is not wrapped by %
                SectionAddress addr = new SectionAddress(sc, section);
                Dictionary<string, string> dict = Ini.ParseIniLinesIniStyle(section.GetLines());
                return LoadLocalMacroDict(addr, dict, append);
            }

            return new List<LogInfo>();
        }

        public List<LogInfo> LoadLocalMacroDict(SectionAddress addr, IEnumerable<string> lines, bool append)
        {
            Dictionary<string, string> dict = Ini.ParseIniLinesIniStyle(lines);
            return LoadLocalMacroDict(addr, dict, append);
        }

        private List<LogInfo> LoadLocalMacroDict(SectionAddress addr, Dictionary<string, string> dict, bool append)
        {
            List<LogInfo> logs = new List<LogInfo>();
            if (!append)
                LocalDict.Clear();

            if (0 < dict.Keys.Count)
            {
                int count = 0;
                logs.Add(new LogInfo(LogState.Info, $"Import Local Macro from [{addr.Section.Name}]", 0));
                foreach (var kv in dict)
                {
                    try
                    {
                        // Macro Name Validation
                        if (!Regex.Match(kv.Key, MacroNameRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant).Success)
                        {
                            logs.Add(new LogInfo(LogState.Error, $"Invalid local macro name [{kv.Key}]"));
                            continue;
                        }

                        LocalDict[kv.Key] = CodeParser.ParseStatement(kv.Value, addr);
                        logs.Add(new LogInfo(LogState.Success, $"Local macro [{kv.Key}] set to [{kv.Value}]", 1));
                        count += 1;
                    }
                    catch (Exception e)
                    {
                        logs.Add(new LogInfo(LogState.Error, e));
                    }
                }
                logs.Add(new LogInfo(LogState.Info, $"Imported {count} Local Macro", 0));
                logs.Add(new LogInfo(LogState.None, Logger.LogSeperator, 0));
            }

            return logs;
        }

        public void ResetLocalMacros()
        {
            LocalDict = new Dictionary<string, CodeCommand>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetLocalMacros(Dictionary<string, CodeCommand> newDict)
        { // Local Macro from [Variables]
            LocalDict = new Dictionary<string, CodeCommand>(newDict, StringComparer.OrdinalIgnoreCase);
        }

        public LogInfo SetMacro(string macroName, string macroCommand, SectionAddress addr, bool global, bool permanent)
        {
            // Macro Name Validation
            if (!Regex.Match(macroName, MacroNameRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant).Success)
                return new LogInfo(LogState.Error, $"Invalid macro name [{macroName}]");

            if (macroCommand != null)
            { // Insert
                // Try parsing
                CodeCommand cmd = CodeParser.ParseStatement(macroCommand, addr);
                if (cmd.Type == CodeType.Error)
                {
                    CodeInfo_Error info = cmd.Info.Cast<CodeInfo_Error>();
                    return new LogInfo(LogState.Error, info.ErrorMessage);
                }

                // Put into dictionary
                if (permanent) // MacroDict
                {
                    MacroDict[macroName] = cmd;
                    if (Ini.WriteKey(addr.Project.MainScript.RealPath, Variables.VarSectionName, macroName, cmd.RawCode))
                        return new LogInfo(LogState.Success, $"Permanent Macro [{macroName}] set to [{cmd.RawCode}]");
                    else
                        return new LogInfo(LogState.Error, $"Could not write macro into [{addr.Project.MainScript.RealPath}]");
                }

                if (global) // MacroDict
                {
                    MacroDict[macroName] = cmd;
                    return new LogInfo(LogState.Success, $"Global Macro [{macroName}] set to [{cmd.RawCode}]");
                }

                LocalDict[macroName] = cmd;
                return new LogInfo(LogState.Success, $"Local Macro [{macroName}] set to [{cmd.RawCode}]");
            }
            else
            {
                // Delete
                // Put into dictionary
                if (permanent) // MacroDict
                {
                    if (MacroDict.ContainsKey(macroName))
                    {
                        MacroDict.Remove(macroName);
                        Ini.DeleteKey(addr.Project.MainScript.RealPath, Variables.VarSectionName, macroName);
                        return new LogInfo(LogState.Success, $"Permanent Macro [{macroName}] deleted");
                    }

                    return new LogInfo(LogState.Error, $"Permanent Macro [{macroName}] not found");
                }

                if (global) // MacroDict
                {
                    if (MacroDict.ContainsKey(macroName))
                    {
                        MacroDict.Remove(macroName);
                        return new LogInfo(LogState.Success, $"Global Macro [{macroName}] deleted");
                    }
                    else
                    {
                        return new LogInfo(LogState.Error, $"Global Macro [{macroName}] not found");
                    }
                }

                // LocalDict
                if (LocalDict.ContainsKey(macroName))
                {
                    LocalDict.Remove(macroName);
                    return new LogInfo(LogState.Success, $"Local Macro [{macroName}] deleted");
                }

                return new LogInfo(LogState.Error, $"Local Macro [{macroName}] not found");
            }
        }
    }
}
