using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceBackup
{
    class PatternFilterRule : FilterRule
    {
        public PatternFilterRule()
        {
            RuleName = "Pattern";
        }
        /// <summary>
        /// aktualisiert die Entscheidungseigenschaft (Include or Exclude) einer PatternFilterRule : FilterRule
        /// </summary>
        /// <param name="isIncluded"></param>
        /// <param name="fileInfo"></param>
        public override void UpdateInclusion(ref bool isIncluded, FileInfo fileInfo)
        {
            Regex regex = FindFilesPatternToRegex.Convert(RuleData);
            if (regex.IsMatch(fileInfo.Name))
            {
                isIncluded = Behaviour == BehaviourType.Include;
            }
        }
    }
}
