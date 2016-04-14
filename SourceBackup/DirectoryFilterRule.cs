using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceBackup
{
    class DirectoryFilterRule : FilterRule
    {
        public DirectoryFilterRule()
        {
            RuleName = "Directory";
        }
        /// <summary>
        /// aktualisiert die Entscheidungseigenschaft (Include or Exclude) einer DirectoryFilterRule : FilterRule
        /// </summary>
        /// <param name="isIncluded"></param>
        /// <param name="fileInfo"></param>
        public override void UpdateInclusion(ref bool isIncluded, FileInfo fileInfo)
        {
            if (fileInfo.FullName.IndexOf(RuleData) >= 0)
            {
                isIncluded = Behaviour == BehaviourType.Include;
            }
        }
    }
}
