using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceBackup
{
    static class FilterRules
    {
        public static Dictionary<string, FilterRule> Rules = new Dictionary<string, FilterRule>();
        /// <summary>
        /// erstellt eine bestimmte Art von Regel (pattern oder directory)
        /// </summary>
        static FilterRules()
        {
            Rules.Add("pattern", new PatternFilterRule());
            Rules.Add("directory", new DirectoryFilterRule());
        }
    }
}
