using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceBackup
{
    public static class FindFilesPatternToRegex
    {
        private static Regex HasQuestionMarkRegEx = new Regex(@"\?", RegexOptions.Compiled);
        private static Regex IllegalCharactersRegex = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
        private static Regex CatchExtentionRegex = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
        private static string NonDotCharacters = @"[^.]*";
        /// <summary>
        /// übersetzt den Regelparameter in ein Regex
        /// </summary>
        /// <param name="patternRuleparameter"></param>
        /// <returns></returns>
        public static Regex Convert(string patternRuleparameter)
        {
            if (patternRuleparameter == null)
            {
                throw new ArgumentNullException();
            }
            patternRuleparameter = patternRuleparameter.Trim();
            if (patternRuleparameter.Length == 0)
            {
                throw new ArgumentException("\nDer Pattern-Regelparamter ist leer.");
            }
            if (IllegalCharactersRegex.IsMatch(patternRuleparameter))
            {
                throw new ArgumentException("\nDer Pattern-Regelparameter beinhaltet illegale Zeichen.");
            }
            bool hasExtension = CatchExtentionRegex.IsMatch(patternRuleparameter);
            bool matchExact = false;
            if (HasQuestionMarkRegEx.IsMatch(patternRuleparameter))
            {
                matchExact = true;
            }
            else if (hasExtension)
            {
                matchExact = CatchExtentionRegex.Match(patternRuleparameter).Groups[1].Length != 3;
            }
            string regexString = Regex.Escape(patternRuleparameter);
            regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
            regexString = Regex.Replace(regexString, @"\\\?", ".");
            if (!matchExact && hasExtension)
            {
                regexString += NonDotCharacters;
            }
            regexString += "$";
            Regex regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return regex;
        }
    }
}
