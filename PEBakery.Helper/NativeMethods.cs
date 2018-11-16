﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PEBakery.Helper
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class NativeMethods
    {
        #region FileHelper
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, SHFILEINFO psfi, uint cbFileInfo, uint uFalgs);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
        #endregion

        #region StringHelper
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogical(string psz1, string psz2);
        #endregion

        #region EncodingHelper
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetACP();
        #endregion
    }
}
