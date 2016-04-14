using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceBackup
{
    public abstract class FilterRule
    {
        /// <summary>
        /// Entscheidungseigenschaften einer FilterRule: Include, Exclude
        /// </summary>
        public enum BehaviourType
        {
            Include,
            Exclude
        }
        /// <summary>
        /// gibt oder setzt eine Entscheidungseigenschaft einer FilterRule
        /// </summary>
        public BehaviourType Behaviour { get; set; }
        /// <summary>
        /// gibt oder setzt den Regelnamen einer FilterRule
        /// </summary>
        public string RuleName { get; protected set; }
        /// <summary>
        /// gibt oder setzt die Regeldaten (Parameter) einer FilterRule
        /// </summary>
        public string RuleData { get; set; }
        /// <summary>
        /// aktualisiert die Entscheidungseigenschaft (Include or Exclude) einer FilterRule
        /// </summary>
        /// <param name="isIncluded"></param>
        /// <param name="fileInfo"></param>
        public abstract void UpdateInclusion(ref bool isIncluded, FileInfo fileInfo);
    }
}
