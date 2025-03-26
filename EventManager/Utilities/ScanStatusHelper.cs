using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Utilities
{
    public static class ScanStatusHelper
    {
        public static class StatusString
        {
            public const string AlreadyScanned = "ALREADY SCANNED";
            public const string NotFound = "NOT FOUND";
            public const string Success = "SUCCESS";
        }
        public static class FrameColor
        {
            public static readonly Color Error = Color.FromArgb("#D50000");
            public static readonly Color Success = Color.FromArgb("#00C853");
            public static readonly Color Default = Color.FromArgb("#009AFE");
        }
    }
}
