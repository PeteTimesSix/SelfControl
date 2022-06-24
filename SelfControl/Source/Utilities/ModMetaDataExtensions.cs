using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PeteTimesSix.SelfControl.Utilities
{
    public static class ModMetaDataExtensions
    {

        public static bool IsIgnoredBySelfControl(this ModMetaData mod)
        {
            return mod.Official || mod.PackageId.Contains("PeteTimesSix.SelfControl", StringComparison.OrdinalIgnoreCase);
        }
        public static long GetTotalFileSize(this ModMetaData mod)
        {
            var root = mod.RootDir;
            var total = root.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            return total;
        }
    }
}
