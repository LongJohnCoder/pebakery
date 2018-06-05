﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PEBakery.Core;
using PEBakery.Core.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PEBakery.Tests.Core.Command
{
    [TestClass]
    public class CommandBranchTests
    {
        #region ExistFile
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistFile()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.ExistFile;

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string kernel32 = Path.Combine(winDir, "System32", "kernel32.dll");
            string invalid = Path.GetTempFileName();
            File.Delete(invalid);

            cond = new BranchCondition(type, false, kernel32);
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, invalid);
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, kernel32);
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, invalid);
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, $"If,ExistFile,{kernel32},Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,ExistFile,{invalid},Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,ExistFile,{kernel32},Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,ExistFile,{invalid},Set,%Dest%,T", "T");
        }
        #endregion

        #region ExistDir
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistDir()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.ExistDir;

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string invalid = Path.GetTempFileName();
            File.Delete(invalid);

            cond = new BranchCondition(type, false, winDir);
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, invalid);
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, winDir);
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, invalid);
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, $"If,ExistDir,{winDir},Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,ExistDir,{invalid},Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,ExistDir,{winDir},Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,ExistDir,{invalid},Set,%Dest%,T", "T");
        }
        #endregion

        #region ExistSection
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistSection()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.ExistSection;

            string tempPath = Path.GetTempFileName();
            try
            {
                using (StreamWriter w = new StreamWriter(tempPath, false, Encoding.UTF8))
                {
                    w.WriteLine("[Hello]");
                    w.WriteLine("A=1");
                    w.WriteLine();
                    w.WriteLine("[World]");
                    w.WriteLine("B=2");
                }

                cond = new BranchCondition(type, false, tempPath, "Hello");
                Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
                cond = new BranchCondition(type, false, tempPath, "PEBakery");
                Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

                cond = new BranchCondition(type, true, tempPath, "Hello");
                Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
                cond = new BranchCondition(type, true, tempPath, "PEBakery");
                Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

                BranchCondition_Single_Template(s, $"If,ExistSection,{tempPath},Hello,Set,%Dest%,T", "T");
                BranchCondition_Single_Template(s, $"If,ExistSection,{tempPath},PEBakery,Set,%Dest%,T", "F");
                BranchCondition_Single_Template(s, $"If,Not,ExistSection,{tempPath},Hello,Set,%Dest%,T", "F");
                BranchCondition_Single_Template(s, $"If,Not,ExistSection,{tempPath},PEBakery,Set,%Dest%,T", "T");
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            
        }
        #endregion

        #region ExistRegSubKey
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistRegSubKey()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchConditionType type = BranchConditionType.ExistRegSubKey;
            BranchCondition cond;

            cond = new BranchCondition(type, false, "HKLM", @"SOFTWARE\Microsoft\DirectMusic");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "HKLM", @"SOFTWARE\Microsoft\DirectMusicNotExist");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "HKLM", @"SOFTWARE\Microsoft\DirectMusic");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "HKLM", @"SOFTWARE\Microsoft\DirectMusicNotExist");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, @"If,ExistRegSection,HKLM,SOFTWARE\Microsoft\DirectMusic,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistRegSubKey,HKLM,SOFTWARE\Microsoft\DirectMusicNotExist,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegSection,HKLM,SOFTWARE\Microsoft\DirectMusic,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegSubKey,HKLM,SOFTWARE\Microsoft\DirectMusicNotExist,Set,%Dest%,T", "T");
        }
        #endregion

        #region ExistRegValue
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistRegValue()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchConditionType type = BranchConditionType.ExistRegValue;
            BranchCondition cond;

            cond = new BranchCondition(type, false, "HKLM", @"SOFTWARE\Microsoft\DirectMusic", "GMFilePath");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "HKLM", @"SOFTWARE\Microsoft\DirectNotMusic", "GMFilePath");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "HKLM", @"SOFTWARE\Microsoft\DirectMusic", "NoFilePath");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "HKLM", @"SOFTWARE\Microsoft\DirectMusic", "GMFilePath");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "HKLM", @"SOFTWARE\Microsoft\DirectNotMusic", "GMFilePath");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "HKLM", @"SOFTWARE\Microsoft\DirectMusic", "NoFilePath");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, @"If,ExistRegKey,HKLM,SOFTWARE\Microsoft\DirectMusic,GMFilePath,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistRegValue,HKLM,SOFTWARE\Microsoft\DirectNotMusic,GMFilePath,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,ExistRegValue,HKLM,SOFTWARE\Microsoft\DirectMusic,NoFilePath,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegKey,HKLM,SOFTWARE\Microsoft\DirectMusic,GMFilePath,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegValue,HKLM,SOFTWARE\Microsoft\DirectNotMusic,GMFilePath,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegValue,HKLM,SOFTWARE\Microsoft\DirectMusic,NoFilePath,Set,%Dest%,T", "T");
        }
        #endregion

        #region ExistRegMulti
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistRegMulti()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchConditionType type = BranchConditionType.ExistRegMulti;
            BranchCondition cond;

            cond = new BranchCondition(type, false, "HKLM", @"SYSTEM\ControlSet001\Control\ServiceGroupOrder", "List", "FSFilter Infrastructure");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "HKLM", @"SYSTEM\ControlSet001\Control\ServiceGroupOrder", "List", "DoesNotExist");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "HKLM", @"SYSTEM\ControlSet001\Control\ServiceProvider\Order", "ExcluedProviders", "EMS");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "HKLM", @"SYSTEM\ControlSet001\Control\ServiceGroupOrder", "List", "FSFilter Infrastructure");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "HKLM", @"SYSTEM\ControlSet001\Control\ServiceGroupOrder", "List", "DoesNotExist");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "HKLM", @"SYSTEM\ControlSet001\Control\ServiceProvider\Order", "ExcluedProviders", "EMS");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, @"If,ExistRegMulti,HKLM,SYSTEM\ControlSet001\Control\ServiceGroupOrder,List,FSFilter#$sInfrastructure,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistRegMulti,HKLM,SYSTEM\ControlSet001\Control\ServiceGroupOrder,List,DoesNotExist,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,ExistRegMulti,HKLM,SYSTEM\ControlSet001\Control\ServiceProvider\Order,ExcluedProviders,EMS,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegMulti,HKLM,SYSTEM\ControlSet001\Control\ServiceGroupOrder,List,FSFilter#$sInfrastructure,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegMulti,HKLM,SYSTEM\ControlSet001\Control\ServiceGroupOrder,List,DoesNotExist,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Not,ExistRegMulti,HKLM,SYSTEM\ControlSet001\Control\ServiceProvider\Order,ExcluedProviders,EMS,Set,%Dest%,T", "T");
        }
        #endregion

        #region ExistVar
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistVar()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchConditionType type = BranchConditionType.ExistVar;
            BranchCondition cond;

            s.Variables.SetValue(VarsType.Fixed, "F", "ixed");
            s.Variables.SetValue(VarsType.Fixed, "G", "lobal");
            s.Variables.SetValue(VarsType.Fixed, "L", "ocal");

            cond = new BranchCondition(type, false, "%F%");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "%G%");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "%L%");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "%N%");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "%F%");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "%G%");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "%L%");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "%N%");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, @"If,ExistVar,%F%,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistVar,%G%,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistVar,%L%,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistVar,%N%,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistVar,%F%,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistVar,%G%,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistVar,%L%,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistVar,%N%,Set,%Dest%,T", "T");
        }
        #endregion

        #region ExistMacro
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfExistMacro()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchConditionType type = BranchConditionType.ExistMacro;
            BranchCondition cond;

            // Test if Unicode can be used in macro name
            s.Macro.MacroDict["대한"] = CodeParser.ParseStatement("Echo,054-790-6641", EngineTests.DummySectionAddress());
            s.Macro.LocalDict["Sonic"] = CodeParser.ParseStatement("Echo,Tails", EngineTests.DummySectionAddress());
            s.Variables.SetValue(VarsType.Local, "Tails", "Sonic");

            cond = new BranchCondition(type, false, "대한");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "민국");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "Sonic");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "%Tails%");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "대한");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "민국");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "Sonic");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "%Tails%");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, @"If,ExistMacro,대한,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistMacro,민국,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,ExistMacro,Sonic,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,ExistMacro,%Tails%,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Not,ExistMacro,대한,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistMacro,민국,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Not,ExistMacro,Sonic,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,ExistMacro,%Tails%,Set,%Dest%,T", "F");
        }
        #endregion

        #region WimExistIndex
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfWimExistIndex()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.WimExistIndex;

            string srcWim = Path.Combine("%TestBench%", "CommandWim", "MultiImage.wim");

            cond = new BranchCondition(type, false, srcWim, "0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "1");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "2");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "3");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "4");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, srcWim, "0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "2");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "3");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "4");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, $"If,WimExistIndex,{srcWim},0,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,WimExistIndex,{srcWim},1,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,WimExistIndex,{srcWim},2,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,WimExistIndex,{srcWim},3,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,WimExistIndex,{srcWim},4,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistIndex,{srcWim},0,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,Not,WimExistIndex,{srcWim},1,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistIndex,{srcWim},2,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistIndex,{srcWim},3,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistIndex,{srcWim},4,Set,%Dest%,T", "T");
        }
        #endregion

        #region WimExistFile
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfWimExistFile()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.WimExistFile;

            string srcWim = Path.Combine("%TestBench%", "CommandWim", "MultiImage.wim");

            cond = new BranchCondition(type, false, srcWim, "1", "A.txt");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "1", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, srcWim, "1", "A.txt");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "1", "B");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, $"If,WimExistFile,{srcWim},1,A.txt,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,WimExistFile,{srcWim},1,B,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistFile,{srcWim},1,A.txt,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistFile,{srcWim},1,B,Set,%Dest%,T", "T");
        }
        #endregion

        #region WimExistDir
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfWimExistDir()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.WimExistDir;

            string srcWim = Path.Combine("%TestBench%", "CommandWim", "MultiImage.wim");

            cond = new BranchCondition(type, false, srcWim, "1", "A.txt");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "1", "B");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, srcWim, "1", "A.txt");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "1", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, $"If,WimExistDir,{srcWim},1,A.txt,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,WimExistDir,{srcWim},1,B,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,Not,WimExistDir,{srcWim},1,A.txt,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,Not,WimExistDir,{srcWim},1,B,Set,%Dest%,T", "F");
        }
        #endregion

        #region WimExistImageInfo
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfWimExistImageInfo()
        {
            EngineState s = EngineTests.CreateEngineState();
            const BranchConditionType type = BranchConditionType.WimExistImageInfo;

            string srcWim = Path.Combine("%TestBench%", "CommandWim", "LZX.wim");

            BranchCondition cond = new BranchCondition(type, false, srcWim, "0", "Name");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "1", "Name");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "1", "Dummy");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, srcWim, "2", "Name");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, srcWim, "0", "Name");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "1", "Name");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "1", "Dummy");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, srcWim, "2", "Name");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, $"If,WimExistImageInfo,{srcWim},0,Name,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,WimExistImageInfo,{srcWim},1,Name,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,WimExistImageInfo,{srcWim},1,Dummy,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,WimExistImageInfo,{srcWim},2,Name,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistImageInfo,{srcWim},0,Name,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,Not,WimExistImageInfo,{srcWim},1,Name,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, $"If,Not,WimExistImageInfo,{srcWim},1,Dummy,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, $"If,Not,WimExistImageInfo,{srcWim},2,Name,Set,%Dest%,T", "T");
        }
        #endregion

        #region Equal, NotEqual (!=)
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfEqual()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.Equal;

            // Equal
            cond = new BranchCondition(type, false, "A", "A");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "A", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "a", "A");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "A", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "A", "B");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "11.1", "11.1.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "11.1", "11.1.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "10.9", "11.1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "12");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "11.1.2.9", "11.1.2.3");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "5", "5.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "5", "5.1.2600");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 does not recognize hex integer representation
            // PEBakery support hex integer representation
            cond = new BranchCondition(type, false, "11", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "13", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // Test for a bug reported in http://theoven.org/index.php?topic=2271.msg25381#msg25381
            cond = new BranchCondition(type, false, "-1", "0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,Equal,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,Equal,a,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Equal,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Equal,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Equal,11,Set,%Dest%,T", "F");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,==,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,==,a,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,==,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,==,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,==,11,Set,%Dest%,T", "F");

            BranchCondition_Comparison_Template(s, "A", "If,Not,%Src%,Equal,A,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,NotEqual,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,NotEqual,09,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,Not,%Src%,Equal,10,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,Not,%Src%,Equal,11,Set,%Dest%,T", "T");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,!=,A,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,!=,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,!=,09,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,!=,10,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,!=,11,Set,%Dest%,T", "T");
        }
        #endregion

        #region Smaller
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfSmaller()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.Smaller;

            cond = new BranchCondition(type, false, "11.1", "11.1.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "11.1", "11.1.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "10.9", "11.1");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "12");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "11.1.2.9", "11.1.2.3");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "5", "5.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "5", "5.1.2600");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 will return lexicographic compare result of two strings.
            // PEBakery will ignore them and treat them as just NotEqual
            cond = new BranchCondition(type, false, "A", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "A", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "B", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 does not recognize hex integer representation
            // PEBakery support hex integer representation
            cond = new BranchCondition(type, false, "11", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "13", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,Smaller,A,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,Smaller,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Smaller,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Smaller,10,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Smaller,11,Set,%Dest%,T", "T");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,<,A,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,<,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,<,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,<,10,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,<,11,Set,%Dest%,T", "T");
        }
        #endregion

        #region SmallerEqual
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfSmallerEqual()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.SmallerEqual;

            cond = new BranchCondition(type, false, "11.1", "11.1.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "11.1", "11.1.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "10.9", "11.1");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "12");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "11.1.2.9", "11.1.2.3");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "5", "5.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "5", "5.1.2600");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 will return lexicographic compare result of two strings.
            // PEBakery will ignore them and treat them as just NotEqual
            cond = new BranchCondition(type, false, "A", "A");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "A", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "B", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 does not recognize hex integer representation
            // PEBakery support hex integer representation
            cond = new BranchCondition(type, false, "11", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "13", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,SmallerEqual,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,SmallerEqual,a,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,SmallerEqual,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,SmallerEqual,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,SmallerEqual,11,Set,%Dest%,T", "T");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,<=,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,<=,a,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,<=,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,<=,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,<=,11,Set,%Dest%,T", "T");
        }
        #endregion

        #region Bigger
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfBigger()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.Bigger;

            cond = new BranchCondition(type, false, "11.1", "11.1.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "11.1", "11.1.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "10.9", "11.1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "12");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "11.1.2.9", "11.1.2.3");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "5", "5.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "5", "5.1.2600");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 will return lexicographic compare result of two strings.
            // PEBakery will ignore them and treat them as just NotEqual
            cond = new BranchCondition(type, false, "A", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "A", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "B", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 does not recognize hex integer representation
            // PEBakery support hex integer representation
            cond = new BranchCondition(type, false, "11", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "13", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,Bigger,A,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,Bigger,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Bigger,09,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Bigger,10,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,Bigger,11,Set,%Dest%,T", "F");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,>,A,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,>,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,>,09,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,>,10,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,>,11,Set,%Dest%,T", "F");
        }
        #endregion

        #region BiggerEqual
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfBiggerEqual()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.BiggerEqual;

            cond = new BranchCondition(type, false, "11.1", "11.1.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "11.1", "11.1.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "10.9", "11.1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "12");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "11.1.2.9", "11.1.2.3");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "5", "5.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "5", "5.1.2600");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 will return lexicographic compare result of two strings.
            // PEBakery will ignore them and treat them as just NotEqual
            cond = new BranchCondition(type, false, "A", "A");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "A", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "B", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 does not recognize hex integer representation
            // PEBakery support hex integer representation
            cond = new BranchCondition(type, false, "11", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "13", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,BiggerEqual,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,BiggerEqual,a,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,BiggerEqual,09,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,BiggerEqual,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,BiggerEqual,11,Set,%Dest%,T", "F");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,>=,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,>=,a,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,>=,09,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,>=,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,>=,11,Set,%Dest%,T", "F");
        }
        #endregion

        #region EqualX
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfEqualX()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchCondition cond;
            BranchConditionType type = BranchConditionType.EqualX;

            cond = new BranchCondition(type, false, "A", "A");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "A", "B");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "a", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "A", "A");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "A", "B");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "11.1", "11.1.0");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "11.1", "11.1.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "10.9", "11.1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "12");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "11.1.2.9", "11.1.2.3");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, false, "5", "5.0");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "5", "5.1.2600");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            // WB082 does not recognize hex integer representation
            // PEBakery support hex integer representation
            cond = new BranchCondition(type, false, "11", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "12", "0xC");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "13", "0xC");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,EqualX,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,EqualX,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,EqualX,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,EqualX,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,EqualX,11,Set,%Dest%,T", "F");

            BranchCondition_Comparison_Template(s, "A", "If,%Src%,===,A,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "A", "If,%Src%,===,a,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,===,09,Set,%Dest%,T", "F");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,===,10,Set,%Dest%,T", "T");
            BranchCondition_Comparison_Template(s, "10", "If,%Src%,===,11,Set,%Dest%,T", "F");
        }
        #endregion

        #region Ping
        [TestMethod]
        [TestCategory("Command")]
        [TestCategory("CommandBranch")]
        public void Branch_IfPing()
        {
            EngineState s = EngineTests.CreateEngineState();
            BranchConditionType type = BranchConditionType.Ping;
            BranchCondition cond;

            // According to https://www.iana.org/domains/root/db, root domain .zzz does not exist
            cond = new BranchCondition(type, false, "aaa.zzz");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "localhost");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "127.0.0.1");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, false, "::1");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));

            cond = new BranchCondition(type, true, "aaa.zzz");
            Assert.IsTrue(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "localhost");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "127.0.0.1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));
            cond = new BranchCondition(type, true, "::1");
            Assert.IsFalse(CommandBranch.CheckBranchCondition(s, cond, out _));

            BranchCondition_Single_Template(s, @"If,Ping,aaa.zzz,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Ping,localhost,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Ping,127.0.0.1,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Ping,::1,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Not,Ping,aaa.zzz,Set,%Dest%,T", "T");
            BranchCondition_Single_Template(s, @"If,Not,Ping,localhost,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,Ping,127.0.0.1,Set,%Dest%,T", "F");
            BranchCondition_Single_Template(s, @"If,Not,Ping,::1,Set,%Dest%,T", "F");
        }
        #endregion

        #region Utility
        public void BranchCondition_Single_Template(EngineState s, string rawCode, string comp)
        { // Use EvalLines instead of Eval, because Eval does not fold embedded command of If/Else
            s.Variables["Dest"] = "F";
            EngineTests.EvalLines(s, new List<string> { rawCode }, ErrorCheck.Success);
            Assert.IsTrue(s.Variables["Dest"].Equals(comp, StringComparison.Ordinal));
        }

        public void BranchCondition_Comparison_Template(EngineState s, string src, string rawCode,  string comp)
        { // Use EvalLines instead of Eval, because Eval does not fold embedded command of If/Else
            s.Variables["Src"] = src;
            s.Variables["Dest"] = "F";
            EngineTests.EvalLines(s, new List<string> { rawCode }, ErrorCheck.Success);
            Assert.IsTrue(s.Variables["Dest"].Equals(comp, StringComparison.Ordinal));
        }
        #endregion
    }
}
