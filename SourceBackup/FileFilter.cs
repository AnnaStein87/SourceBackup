using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SourceBackup
{
    abstract class FileFilter
    {
        /// <summary>
        /// Dateifilter mit den Eigenschaften: excludeOrInclude, Regelname, Regelparameter
        /// </summary>
        /// <param name="excludeOrInclude"></param>
        /// <param name="rulename"></param>
        /// <param name="ruleparameter"></param>
        public FileFilter(string excludeOrInclude, string rulename, string ruleparameter)
        {
            this.excludeOrInclude = excludeOrInclude;
            this.rulename = rulename;
            this.ruleparameter = ruleparameter;
        }
        /// <summary>
        /// Entscheidungsmethode eines Dateifilters; ist true, wenn eine Datei die Kriterien/Regeln erfüllt
        /// </summary>
        /// <param name="directoryName"></param>
        /// <param name="fileNameWithoutExtension"></param>
        /// <param name="includeFile"></param>
        /// <returns></returns>
        abstract public bool IncludeFile(string directoryName, string fileNameWithoutExtension, bool includeFile);

        public string excludeOrInclude;
        public string rulename;
        public string ruleparameter;
    }
}
