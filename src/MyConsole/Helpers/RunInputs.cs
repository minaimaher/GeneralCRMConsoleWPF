using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace MyConsole.Helpers
{
    /// <summary>
    /// Optional inputs supplied by a host such as the WPF Function Runner.
    /// When a value is left null, every function keeps its original behaviour,
    /// so the console app works exactly as before.
    /// </summary>
    public static class RunInputs
    {
        /// <summary>Full path of the Excel file to read instead of "Ids.xlsx" next to the exe.</summary>
        public static string ExcelPath { get; set; }

        /// <summary>Overrides the Skip / Take values from App.config.</summary>
        public static int? Skip { get; set; }
        public static int? Take { get; set; }

        /// <summary>Returns the connection to use instead of reading credentials from App.config.</summary>
        public static Func<IOrganizationService> Connect { get; set; }

        public static void Reset()
        {
            ExcelPath = null;
            Skip = null;
            Take = null;
        }

        internal static int SkipOr(string configValue) => Skip ?? int.Parse(configValue);
        internal static int TakeOr(string configValue) => Take ?? int.Parse(configValue);

        internal static List<T> Or<T>(IEnumerable<T> supplied, List<T> fallback)
        {
            if (supplied == null) return fallback;
            var list = new List<T>(supplied);
            return list.Count > 0 ? list : fallback;
        }
    }
}
