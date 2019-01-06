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

using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PEBakery.Core
{
    public class LogExporter
    {
        #region Fields and Constructors
        private readonly LogDatabase _db;
        private readonly LogExportType _exportType;
        private readonly TextWriter _w;

        public LogExporter(LogDatabase db, LogExportType type, TextWriter writer)
        {
            // The responsibility of closing _db and _w goes to the caller of LogExporter
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _w = writer ?? throw new ArgumentNullException(nameof(writer));
            _exportType = type;
        }
        #endregion

        #region ExportSystemLog
        public void ExportSystemLog()
        {
            switch (_exportType)
            {
                case LogExportType.Text:
                    {
                        _w.WriteLine("- PEBakery System Log -");
                        var logs = _db.Table<LogModel.SystemLog>().OrderBy(x => x.Time);
                        foreach (LogModel.SystemLog log in logs)
                        {
                            if (log.State == LogState.None)
                                _w.WriteLine($"[{log.TimeStr}] {log.Message}");
                            else
                                _w.WriteLine($"[{log.TimeStr}] [{log.State}] {log.Message}");
                        }
                    }
                    break;
                case LogExportType.Html:
                    {
                        ExportSystemLogHtmlModel m = new ExportSystemLogHtmlModel
                        {
                            PEBakeryVersion = Global.Const.StringVersionFull,
                            SysLogs = new List<SystemLogHtmlModel>(),
                        };

                        var logs = _db.Table<LogModel.SystemLog>().OrderBy(x => x.Time);
                        foreach (LogModel.SystemLog log in logs)
                        {
                            m.SysLogs.Add(new SystemLogHtmlModel
                            {
                                TimeStr = log.TimeStr,
                                State = log.State,
                                Message = log.Message
                            });
                        }

                        string html = RazorEngine.Engine.Razor.RunCompile(Properties.Resources.SystemLogHtmlTemplate, "SystemLogHtmlTemplateKey", null, m);
                        _w.WriteLine(html);
                    }
                    break;
            }
        }
        #endregion

        #region ExportBuildLog
        public struct BuildLogOptions
        {
            public bool IncludeComments;
            public bool IncludeMacros;
        }

        public void ExportBuildLog(int buildId, BuildLogOptions opts)
        {
            switch (_exportType)
            {
                #region Text
                case LogExportType.Text:
                    {
                        LogModel.BuildInfo dbBuild = _db.Table<LogModel.BuildInfo>().First(x => x.Id == buildId);
                        if (dbBuild.EndTime == DateTime.MinValue)
                            dbBuild.EndTime = DateTime.UtcNow;

                        _w.WriteLine($"- PEBakery Build <{dbBuild.Name}> -");
                        _w.WriteLine($"Started  at {dbBuild.StartTime.ToLocalTime().ToString("yyyy-MM-dd hh:mm:ss tt K", CultureInfo.InvariantCulture)}");
                        _w.WriteLine($"Finished at {dbBuild.EndTime.ToLocalTime().ToString("yyyy-MM-dd hh:mm:ss tt K", CultureInfo.InvariantCulture)}");
                        TimeSpan t = dbBuild.EndTime - dbBuild.StartTime;
                        _w.WriteLine($"Took {t:h\\:mm\\:ss}");
                        _w.WriteLine();
                        _w.WriteLine();

                        // Log Statistics
                        _w.WriteLine("<Log Statistics>");
                        var states = ((LogState[])Enum.GetValues(typeof(LogState))).Where(x => x != LogState.None && x != LogState.CriticalError);
                        foreach (LogState state in states)
                        {
                            int count = _db.Table<LogModel.BuildLog>().Count(x => x.BuildId == buildId && x.State == state);
                            _w.WriteLine($"{state.ToString().PadRight(9)}: {count}");
                        }
                        _w.WriteLine();
                        _w.WriteLine();

                        // Show ErrorLogs
                        LogModel.BuildLog[] errors = _db.Table<LogModel.BuildLog>().Where(x => x.BuildId == buildId && x.State == LogState.Error).ToArray();
                        if (0 < errors.Length)
                        {
                            _w.WriteLine("<Errors>");

                            int[] scLogIds = errors.Select(x => x.ScriptId).OrderBy(x => x).Distinct().ToArray();
                            LogModel.Script[] scLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && scLogIds.Contains(x.Id)).ToArray();

                            int[] refScLogIds = errors.Select(x => x.RefScriptId).OrderBy(x => x).Distinct().ToArray();
                            LogModel.Script[] refScLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && refScLogIds.Contains(x.Id)).ToArray();
                            foreach (LogModel.Script scLog in scLogs)
                            {
                                LogModel.BuildLog[] eLogs = errors.Where(x => x.ScriptId == scLog.Id).ToArray();
                                if (eLogs.Length == 1)
                                    _w.WriteLine($"- [{eLogs.Length}] Error in script [{scLog.Name}] ({scLog.TreePath})");
                                else
                                    _w.WriteLine($"- [{eLogs.Length}] Errors in script [{scLog.Name}] ({scLog.TreePath})");

                                foreach (LogModel.BuildLog eLog in eLogs)
                                {
                                    _w.WriteLine(eLog.Export(LogExportType.Text, false));
                                    if (eLog.RefScriptId != 0 && eLog.RefScriptId != eLog.ScriptId)
                                    {
                                        _w.Write("  ");
                                        _w.WriteLine(ExportRefScriptText(eLog, refScLogs));
                                    }
                                }
                                _w.WriteLine();
                            }

                            _w.WriteLine();
                        }

                        // Show WarnLogs
                        LogModel.BuildLog[] warns = _db.Table<LogModel.BuildLog>().Where(x => x.BuildId == buildId && x.State == LogState.Warning).ToArray();
                        if (0 < warns.Length)
                        {
                            _w.WriteLine("<Warnings>");

                            int[] scLogIds = warns.Select(x => x.ScriptId).OrderBy(x => x).Distinct().ToArray();
                            LogModel.Script[] scLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && scLogIds.Contains(x.Id)).ToArray();

                            int[] refScLogIds = warns.Select(x => x.RefScriptId).OrderBy(x => x).Distinct().ToArray();
                            LogModel.Script[] refScLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && refScLogIds.Contains(x.Id)).ToArray();

                            foreach (LogModel.Script scLog in scLogs)
                            {
                                LogModel.BuildLog[] wLogs = warns.Where(x => x.ScriptId == scLog.Id).ToArray();
                                Debug.Assert(0 < wLogs.Length);

                                if (wLogs.Length == 1)
                                    _w.WriteLine($"- [{wLogs.Length}] Warning in script [{scLog.Name}] ({scLog.TreePath})");
                                else
                                    _w.WriteLine($"- [{wLogs.Length}] Warnings in script [{scLog.Name}] ({scLog.TreePath})");

                                foreach (LogModel.BuildLog wLog in wLogs)
                                {
                                    _w.WriteLine(wLog.Export(LogExportType.Text, false));
                                    if (wLog.RefScriptId != 0 && wLog.RefScriptId != wLog.ScriptId)
                                    {
                                        _w.Write("  ");
                                        _w.WriteLine(ExportRefScriptText(wLog, refScLogs));
                                    }
                                }
                                _w.WriteLine();
                            }

                            _w.WriteLine();
                        }

                        // Script
                        LogModel.Script[] scripts = _db.Table<LogModel.Script>()
                            .Where(x => x.BuildId == buildId)
                            .ToArray();

                        LogModel.Script[] processedScripts = scripts
                            .Where(x => 0 < x.Order)
                            .OrderBy(x => x.Order)
                            .ToArray();
                        _w.WriteLine("<Scripts>");
                        {
                            int count = processedScripts.Length;
                            int idx = 1;
                            foreach (LogModel.Script sc in processedScripts)
                            {
                                _w.WriteLine($"[{idx}/{count}] {sc.Name} v{sc.Version} ({sc.ElapsedMilliSec / 1000.0:0.000}s)");
                                idx++;
                            }

                            _w.WriteLine($"Total {count} Scripts");
                            _w.WriteLine();
                            _w.WriteLine();
                        }

                        // Variables
                        _w.WriteLine("<Variables>");
                        VarsType[] typeList = { VarsType.Fixed, VarsType.Global };
                        foreach (VarsType varsType in typeList)
                        {
                            _w.WriteLine($"- {varsType} Variables");
                            var vars = _db.Table<LogModel.Variable>()
                                .Where(x => x.BuildId == buildId && x.Type == varsType)
                                .OrderBy(x => x.Key);
                            foreach (LogModel.Variable log in vars)
                                _w.WriteLine($"%{log.Key}% = {log.Value}");
                            _w.WriteLine();
                        }
                        _w.WriteLine();

                        // Code Logs
                        _w.WriteLine("<Code Logs>");
                        {
                            foreach (LogModel.Script scLog in processedScripts)
                            {
                                // Log codes
                                var cLogs = _db.Table<LogModel.BuildLog>()
                                    .Where(x => x.BuildId == buildId && x.ScriptId == scLog.Id);
                                if (!opts.IncludeComments)
                                    cLogs = cLogs.Where(x => (x.Flags & LogModel.BuildLogFlag.Comment) != LogModel.BuildLogFlag.Comment);
                                if (!opts.IncludeMacros)
                                    cLogs = cLogs.Where(x => (x.Flags & LogModel.BuildLogFlag.Macro) != LogModel.BuildLogFlag.Macro);
                                cLogs = cLogs.OrderBy(x => x.Id);
                                foreach (LogModel.BuildLog log in cLogs)
                                    _w.WriteLine(log.Export(LogExportType.Text, true));

                                // Log local variables
                                var vLogs = _db.Table<LogModel.Variable>()
                                    .Where(x => x.BuildId == buildId && x.ScriptId == scLog.Id && x.Type == VarsType.Local)
                                    .OrderBy(x => x.Key);
                                if (vLogs.Any())
                                {
                                    _w.WriteLine($"- Local Variables of Script [{scLog.Name}]");
                                    foreach (LogModel.Variable vLog in vLogs)
                                        _w.WriteLine($"%{vLog.Key}% = {vLog.Value}");
                                    _w.WriteLine(Logger.LogSeparator);
                                }

                                _w.WriteLine();
                            }
                        }
                    }
                    break;
                #endregion
                #region HTML
                case LogExportType.Html:
                    {
                        LogModel.BuildInfo dbBuild = _db.Table<LogModel.BuildInfo>().First(x => x.Id == buildId);
                        if (dbBuild.EndTime == DateTime.MinValue)
                            dbBuild.EndTime = DateTime.UtcNow;
                        ExportBuildLogHtmlModel m = new ExportBuildLogHtmlModel
                        {
                            PEBakeryVersion = Global.Const.StringVersionFull,
                            BuildName = dbBuild.Name,
                            BuildStartTimeStr = dbBuild.StartTime.ToLocalTime().ToString("yyyy-MM-dd h:mm:ss tt K", CultureInfo.InvariantCulture),
                            BuildEndTimeStr = dbBuild.EndTime.ToLocalTime().ToString("yyyy-MM-dd h:mm:ss tt K", CultureInfo.InvariantCulture),
                            BuildTookTimeStr = $"{dbBuild.EndTime - dbBuild.StartTime:h\\:mm\\:ss}",
                            LogStats = new List<LogStatHtmlModel>(),
                        };

                        // Log Statistics
                        var states = ((LogState[])Enum.GetValues(typeof(LogState))).Where(x => x != LogState.None && x != LogState.CriticalError);
                        foreach (LogState state in states)
                        {
                            int count = _db.Table<LogModel.BuildLog>().Count(x => x.BuildId == buildId && x.State == state);

                            m.LogStats.Add(new LogStatHtmlModel
                            {
                                State = state,
                                Count = count,
                            });
                        }

                        // Show ErrorLogs
                        m.ErrorCodeDicts = new Dictionary<ScriptHtmlModel, Tuple<CodeLogHtmlModel, string>[]>();
                        {
                            int errIdx = 0;
                            LogModel.BuildLog[] errors = _db.Table<LogModel.BuildLog>().Where(x => x.BuildId == buildId && x.State == LogState.Error).ToArray();
                            if (0 < errors.Length)
                            {
                                int[] pLogIds = errors.Select(x => x.ScriptId).Distinct().ToArray();
                                LogModel.Script[] scLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && pLogIds.Contains(x.Id)).ToArray();

                                int[] refScLogIds = errors.Select(x => x.RefScriptId).OrderBy(x => x).Distinct().ToArray();
                                LogModel.Script[] refScLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && refScLogIds.Contains(x.Id)).ToArray();

                                foreach (LogModel.Script scLog in scLogs)
                                {
                                    ScriptHtmlModel scModel = new ScriptHtmlModel
                                    {
                                        Name = scLog.Name,
                                        Path = scLog.TreePath,
                                    };

                                    m.ErrorCodeDicts[scModel] = errors
                                        .Where(x => x.ScriptId == scLog.Id)
                                        .Select(x => new Tuple<CodeLogHtmlModel, string>(
                                            new CodeLogHtmlModel
                                            {
                                                State = x.State,
                                                Message = x.Export(LogExportType.Html, false),
                                                Href = errIdx++,
                                            }, ExportRefScriptText(x, refScLogs))).ToArray();
                                }
                            }
                        }

                        // Show WarnLogs
                        m.WarnCodeDicts = new Dictionary<ScriptHtmlModel, Tuple<CodeLogHtmlModel, string>[]>();
                        {
                            int warnIdx = 0;
                            LogModel.BuildLog[] warns = _db.Table<LogModel.BuildLog>().Where(x => x.BuildId == buildId && x.State == LogState.Warning).ToArray();
                            if (0 < warns.Length)
                            {
                                int[] pLogIds = warns.Select(x => x.ScriptId).Distinct().ToArray();
                                LogModel.Script[] scLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && pLogIds.Contains(x.Id)).ToArray();

                                int[] refScLogIds = warns.Select(x => x.RefScriptId).OrderBy(x => x).Distinct().ToArray();
                                LogModel.Script[] refScLogs = _db.Table<LogModel.Script>().Where(x => x.BuildId == buildId && refScLogIds.Contains(x.Id)).ToArray();

                                foreach (LogModel.Script scLog in scLogs)
                                {
                                    ScriptHtmlModel pModel = new ScriptHtmlModel
                                    {
                                        Name = scLog.Name,
                                        Path = scLog.TreePath,
                                    };
                                    m.WarnCodeDicts[pModel] = warns
                                        .Where(x => x.ScriptId == scLog.Id)
                                        .Select(x => new Tuple<CodeLogHtmlModel, string>(
                                            new CodeLogHtmlModel
                                            {
                                                State = x.State,
                                                Message = x.Export(LogExportType.Html, false),
                                                Href = warnIdx++,
                                            }, ExportRefScriptText(x, refScLogs))).ToArray();
                                }
                            }
                        }

                        // Scripts
                        var scripts = _db.Table<LogModel.Script>()
                            .Where(x => x.BuildId == buildId && 0 < x.Order)
                            .OrderBy(x => x.Order);
                        m.Scripts = new List<ScriptHtmlModel>();
                        {
                            int idx = 1;
                            foreach (LogModel.Script scLog in scripts)
                            {
                                m.Scripts.Add(new ScriptHtmlModel
                                {
                                    Index = idx,
                                    Name = scLog.Name,
                                    Path = scLog.TreePath,
                                    Version = $"v{scLog.Version}",
                                    TimeStr = $"{scLog.ElapsedMilliSec / 1000.0:0.000}s",
                                });
                                idx++;
                            }
                        }

                        // Variables
                        m.Vars = new List<VarHtmlModel>();
                        {
                            var vars = _db.Table<LogModel.Variable>()
                                        .Where(x => x.BuildId == buildId && (x.Type == VarsType.Fixed || x.Type == VarsType.Global))
                                        .OrderBy(x => x.Type)
                                        .ThenBy(x => x.Key);
                            foreach (LogModel.Variable vLog in vars)
                            {
                                m.Vars.Add(new VarHtmlModel
                                {
                                    Type = vLog.Type,
                                    Key = vLog.Key,
                                    Value = vLog.Value,
                                });
                            }
                        }

                        // CodeLogs
                        m.CodeLogs = new List<Tuple<ScriptHtmlModel, CodeLogHtmlModel[], VarHtmlModel[]>>();
                        {
                            int pIdx = 0;
                            int errIdx = 0;
                            int warnIdx = 0;

                            foreach (LogModel.Script scLog in scripts)
                            {
                                pIdx += 1;

                                // Log codes
                                var cLogs = _db.Table<LogModel.BuildLog>().Where(x => x.BuildId == buildId && x.ScriptId == scLog.Id);
                                if (!opts.IncludeComments)
                                    cLogs = cLogs.Where(x => (x.Flags & LogModel.BuildLogFlag.Comment) != LogModel.BuildLogFlag.Comment);
                                if (!opts.IncludeMacros)
                                    cLogs = cLogs.Where(x => (x.Flags & LogModel.BuildLogFlag.Macro) != LogModel.BuildLogFlag.Macro);
                                LogModel.BuildLog[] codeLogs = cLogs.OrderBy(x => x.Id).OrderBy(x => x.Id).ToArray();

                                ScriptHtmlModel pModel = new ScriptHtmlModel
                                {
                                    Index = pIdx,
                                    Name = scLog.Name,
                                    Path = scLog.TreePath,
                                };

                                List<CodeLogHtmlModel> logModel = new List<CodeLogHtmlModel>(codeLogs.Length);
                                foreach (LogModel.BuildLog log in codeLogs)
                                {
                                    CodeLogHtmlModel item = new CodeLogHtmlModel
                                    {
                                        State = log.State,
                                        Message = log.Export(LogExportType.Html, true),
                                    };

                                    if (log.State == LogState.Error)
                                        item.Href = errIdx++;
                                    else if (log.State == LogState.Warning)
                                        item.Href = warnIdx++;

                                    logModel.Add(item);
                                }

                                // Log local variables
                                VarHtmlModel[] localVarModel = _db.Table<LogModel.Variable>()
                                    .Where(x => x.BuildId == buildId && x.ScriptId == scLog.Id && x.Type == VarsType.Local)
                                    .OrderBy(x => x.Key)
                                    .Select(x => new VarHtmlModel
                                    {
                                        Type = x.Type,
                                        Key = x.Key,
                                        Value = x.Value,
                                    }).ToArray();

                                m.CodeLogs.Add(new Tuple<ScriptHtmlModel, CodeLogHtmlModel[], VarHtmlModel[]>(pModel, logModel.ToArray(), localVarModel));
                            }
                        }

                        string html = RazorEngine.Engine.Razor.RunCompile(Properties.Resources.BuildLogHtmlTemplate, "BuildLogHtmlTemplateKey", null, m);
                        _w.WriteLine(html);
                    }
                    break;
                    #endregion
            }
        }
        #endregion

        #region ExportRefScriptText
        private static string ExportRefScriptText(LogModel.BuildLog bLog, LogModel.Script[] refScLogs)
        {
            if (bLog.RefScriptId == 0 || bLog.RefScriptId == bLog.ScriptId)
                return null;

            LogModel.Script refScLog = refScLogs.FirstOrDefault(x => x.Id == bLog.RefScriptId);
            if (refScLog == null)
                return "|-> Referenced unknown script";

            string path = refScLog.TreePath;
            if (path.Length == 0)
                path = refScLog.RealPath;
            return $"|-> Referenced script [{refScLog.Name}] ({path})";
        }
        #endregion

        #region HtmlModel
        public class ExportSystemLogHtmlModel
        {
            // ReSharper disable once InconsistentNaming
            public string PEBakeryVersion { get; set; }
            public List<SystemLogHtmlModel> SysLogs { get; set; }
        }

        public class SystemLogHtmlModel
        {
            public string TimeStr { get; set; }
            public LogState State { get; set; }
            public string Message { get; set; }
        }

        public class ExportBuildLogHtmlModel
        {
            // ReSharper disable once InconsistentNaming
            public string PEBakeryVersion { get; set; }
            public string BuildName { get; set; }
            public string BuildStartTimeStr { get; set; }
            public string BuildEndTimeStr { get; set; }
            public string BuildTookTimeStr { get; set; }
            public List<LogStatHtmlModel> LogStats { get; set; }
            public List<ScriptHtmlModel> Scripts { get; set; }
            public List<VarHtmlModel> Vars { get; set; }
            public Dictionary<ScriptHtmlModel, Tuple<CodeLogHtmlModel, string>[]> ErrorCodeDicts { get; set; }
            public Dictionary<ScriptHtmlModel, Tuple<CodeLogHtmlModel, string>[]> WarnCodeDicts { get; set; }
            public List<Tuple<ScriptHtmlModel, CodeLogHtmlModel[], VarHtmlModel[]>> CodeLogs { get; set; }
        }

        public class LogStatHtmlModel
        {
            public LogState State { get; set; }
            public int Count { get; set; }
        }

        public class ScriptHtmlModel
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public string Version { get; set; }
            public string TimeStr { get; set; }
        }

        public class VarHtmlModel
        {
            public VarsType Type { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public class CodeLogHtmlModel
        {
            public LogState State { get; set; }
            public string Message { get; set; }
            public int Href { get; set; } // Optional
        }
        #endregion
    }
}