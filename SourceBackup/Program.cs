using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.IO.Compression; //für das Zip-Archiv
using System.Security.Cryptography; //für die Verschlüsselung mit AES
using System.Text.RegularExpressions;

namespace SourceBackup
{
    class Program
    {
        /// <summary>
        /// festegelegte Endung für die verschlüsselte Datei
        /// </summary>
        private const string EncryptEnding = ".psenc";
        /// <summary>
        /// festgelegte Schlüssellänge
        /// </summary>
        private const int KeyLength = 32;
        /// <summary>
        /// festgelegter Salt für den Schlüssel
        /// </summary>
        private static readonly byte[] Salt = new byte[] { 53, 31, 61, 83, 23, 71, 41, 11 };
        /// <summary>
        /// festgelegte Iteration für den Schlüssel
        /// </summary>
        private const int Iterations = 300;
        /// <summary>
        /// festgelegter Initialisierungsvektor für den Schlüssel
        /// </summary>
        private static readonly byte[] IV = new byte[] { 30, 3, 31, 83, 53, 18, 61, 8, 23, 71, 1, 13, 36, 11, 41, 24 };

        //cd Documents\Visual Studio 2015\Projects\SourceBackup\SourceBackup\bin\Debug\   
        //SourceBackup.exe "C:\Users\i.smemann\Documents\Visual Studio 2015\Projects\SourceBackup\TestOrdner" /destination C:\Sourcesbackup\ /passphrasefile "C:\Users\i.smemann\Documents\Visual Studio 2015\Projects\SourceBackup\TestOrdner\passphraseTest.txt" 
        //Fehler: SourceBackup.exe "C:\Users\i.smemann\Documents\Visual Studio 2015\Projects\SourceBackup\TestOrdner" /destination "C:\Sourcesbackup\" /passphrasefile "C:\Users\i.smemann\Documents\Visual Studio 2015\Projects\SourceBackup\TestOrdner\passphraseTest.txt" 
        //SourceBackup.exe "C:\Users\i.smemann\Documents\Visual Studio 2015\Projects\SourceBackup\TestOrdner" /destination C:\Sourcesbackup\ /passphrase "Ich arbeite gerne bei Plansysteme!" 
        //Fehler: SourceBackup.exe "C:\Users\i.smemann\Documents\Visual Studio 2015\Projects\SourceBackup\TestOrdner" /destination "C:\Sourcesbackup\" /passphrase "Ich arbeite gerne bei Plansysteme!" 


        static void Main(string[] args)
        {
            Console.WriteLine("\nSource Backup 1.0");
            Console.WriteLine("\nDieses Programm macht ein verschluesseltes Backup als zip-Datei. \nDie zu sicherenden Dateien koennen in einer Filter-Datei im jeweiligen Ursprungsordner festgelegt werden.");
            Console.WriteLine("\nEingaben: Genau einen Passphrase ODER eine Passphrasedatei (/passphrase oder /passphrasefile)\nund genau einen Speicherpfad(/destination) sowie mindestens einen Backupverzeichnispfad.");
            Console.WriteLine("\nFolgende Eingaben von Ihnen werden nun verarbeitet:");
            foreach (var arg in args)
                Console.WriteLine(arg);

            // in dieser Liste werden nachher alle gültigen directoriesToBackup aufgelistet
            List<string> directoriesToBackup = new List<string>();
            
            // zählt die erfolgreichen Kopien
            int copyCount = 0;
            // zählt die Fehler beim Kopieren
            int errorCount = 0;
            // ist true, solange alles in Ordnung ist
            bool success = true;
            string passphrase = "";
            string destinationDirectory = "";
            int passphraseCount = 0;
            int backupDirectoryCount = 0;
            int destinationDirectoryCount = 0;

            // prepare command line switches
            int i = 0;
            while (success && i < args.Length)
            {
                success = AssignAndFilterArgs(directoriesToBackup, ref passphrase, ref passphraseCount, ref destinationDirectory, ref destinationDirectoryCount, ref backupDirectoryCount, args, i);
                i++;
            }

            // check if all precondition apply and create destinationDirectory
            success = success && CheckPreconditions(passphraseCount, destinationDirectoryCount, backupDirectoryCount) && CreateDirectory(destinationDirectory);

            // process backups
            if (success)
            {
                foreach (string directoryToBackup in directoriesToBackup)
                {
                    Console.WriteLine("\n\n\nFolgendes Verzeichnis wird nun bearbeitet:\n" + directoryToBackup);
                    DoBackup(directoryToBackup, ref copyCount, ref errorCount, passphrase, destinationDirectory);
                }
            }
            else
            {
                Console.WriteLine("\nDieser fatale Fehler fuehrte zum Programm-Abbruch.");
            }

            //// Ende des Programms
            //Console.WriteLine("\nVielen Dank für Ihren Auftrag! Schoenen Tag noch!");
            //Console.ReadLine();
        }
        /// <summary>
        /// ordnet die "sinnvollen" Eingaben entsprechen zu (Passphrase, Destination-Verzeichnis und Backup-Verzeichnisse)
        /// </summary>
        /// <param name="directoriesToBackup"></param>
        /// <param name="passphrase"></param>
        /// <param name="passphraseCount"></param>
        /// <param name="backupDestinationDirectory"></param>
        /// <param name="backupDestinationDirectoryCount"></param>
        /// <param name="backupDirectoryCount"></param>
        /// <param name="args"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool AssignAndFilterArgs(List<string> directoriesToBackup, ref string passphrase, ref int passphraseCount, ref string backupDestinationDirectory, ref int backupDestinationDirectoryCount, ref int backupDirectoryCount, string[] args, int i)
        {
            bool result = true;
            if (args[i] == "/passphrase" || args[i] == "/passphrasefile" || args[i] == "/destination")
            {
                result = CheckIfArgAfterProgramkeyword(args, i) && CheckProgramkeyword(ref passphrase, ref passphraseCount, ref backupDestinationDirectory, ref backupDestinationDirectoryCount, args, i);
            }
            else
            {
                if ((i == 0 || !(args[i - 1] == "/passphrase" || args[i - 1] == "/passphrasefile" || args[i - 1] == "/destination")) && (args[i] != null))
                {
                    string directory = args[i].Trim();
                    DirectoriesToBackupFilter(directoriesToBackup, directory, ref backupDirectoryCount);
                }
            }
            return result;
        }
        /// <summary>
        /// überprüft, ob hinter dem Programmschlüsselwort ("/passphrase", "/passphrasefile" oder "/destination") eine Angabe ist
        /// </summary>
        /// <param name="args"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool CheckIfArgAfterProgramkeyword(string[] args, int i)
        {
            bool result = true;
            if (i == args.Length - 1)
            {
                result = false;
                Console.WriteLine("\nHinter dem Programmschluesselwort \"{0}\" ist keine Angabe.", args[i]);
            }
            return result;
        }
        /// <summary>
        /// überprüft ein Programmschlüsselwort ("/passphrase", "/passphrasefile" oder "/destination") und deren Angaben, ordnet wenn möglich zu
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="passphraseCount"></param>
        /// <param name="backupDestinationDirectory"></param>
        /// <param name="backupDestinationDirectoryCount"></param>
        /// <param name="args"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool CheckProgramkeyword(ref string passphrase, ref int passphraseCount, ref string backupDestinationDirectory, ref int backupDestinationDirectoryCount, string[] args, int i)
        {
            bool result = true;
            switch (args[i])
            {
                case "/passphrase":
                    result = CheckPassphrase(ref passphrase, ref passphraseCount, args, i);
                    break;
                case "/passphrasefile":
                    result = ReadPassphraseFile(ref passphrase, args, i) && CheckPassphraseEmpty(passphrase, ref passphraseCount);
                    break;
                case "/destination":
                    backupDestinationDirectory = args[i + 1];
                    CountDestinationDirectory(ref backupDestinationDirectoryCount, ref backupDestinationDirectory);
                    break;
            }
            return result;
        }
        /// <summary>
        /// überprüft den Passphrase
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="passphraseCount"></param>
        /// <param name="args"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool CheckPassphrase(ref string passphrase, ref int passphraseCount, string[] args, int i)
        {
            bool result;
            if (args[i + 1] != "/passphrase" && args[i + 1] != "/passphrasefile" && args[i + 1] != "/destination" && !Regex.IsMatch(args[i + 1], @"^[a-zA-Z]:\.*"))
            {
                passphrase = args[i + 1];
                result = CheckPassphraseEmpty(passphrase, ref passphraseCount);
            }
            else
            {
                result = false;
                Console.WriteLine("\nDer mit dem Programmschluesselwort \"/passphrase\" eingegebene Passphrase darf nicht \neinem Programmschluesselwort wie \"/passphrase\" selbst, \"/passphrasefile\" oder \"/destination\" entsprechen \nund nicht mit der Kombination - ein Buchstaben und \"{0}\" - beginnen.", @":\");
            }

            return result;
        }
        /// <summary>
        /// liest den Passphrase aus und ordnet diesen zu
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="args"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool ReadPassphraseFile(ref string passphrase, string[] args, int i)
        {
            bool result = true;
            string passphrasefile = args[i + 1];
            try
            {
                passphrase = File.ReadAllText(passphrasefile);
            }
            catch (Exception e)
            {
                result = false;
                Console.WriteLine("\nFehler beim Lesen der Passphrase-Datei: \n{0}", e.ToString());
            }
            return result;
        }
        /// <summary>
        /// überprüft, ob der Passphrase nicht leer ist bzw. nur aus Leerzeichen besteht, erhöht ggf. den Passphrase-Zähler um eins
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="passphraseCount"></param>
        /// <returns></returns>
        private static bool CheckPassphraseEmpty(string passphrase, ref int passphraseCount)
        {
            bool result = true;
            if (passphrase.Trim() == "")
            {
                Console.WriteLine("\nDer Passphrase ist leer oder besteht nur aus Leerzeichen (\"\").");
                result = false;
            }
            else
                passphraseCount++;
            return result;
        }
        /// <summary>
        /// zählt die backupDestinationDirectorys und entfernt alle \ am Ende
        /// </summary>
        /// <param name="backupDestinationDirectoryCount"></param>
        /// <param name="backupDestinationDirectory"></param>
        private static void CountDestinationDirectory(ref int backupDestinationDirectoryCount, ref string backupDestinationDirectory)
        {
            if (backupDestinationDirectory.EndsWith(@"\\"))
            {
                backupDestinationDirectory = backupDestinationDirectory.TrimEnd(backupDestinationDirectory.Last());
            }
            if (!backupDestinationDirectory.EndsWith(@"\"))
            {
                backupDestinationDirectory = backupDestinationDirectory + @"\";
            }
            backupDestinationDirectoryCount++;
        }
        /// <summary>
        /// schreibt einen gültigen Pfad in List<string> directoriesToBackup
        /// </summary>
        /// <param name="directoriesToBackup"></param>
        /// <param name="directory"></param>
        /// <param name="backupDirectoryCount"></param>
        private static void DirectoriesToBackupFilter(List<string> directoriesToBackup, string directory, ref int backupDirectoryCount)
        {
            //entfernt ALLE \ am Ende, sofern es welche gibt
            if (directory != null && directory.EndsWith(@"\"))
            {
                directory = directory.TrimEnd(directory.Last());
            }
            if (directory != null && Directory.Exists(directory))
            {
                //schreibt einen gültigen Pfad in die Liste der zu sicherenden Verzeichnisse
                directoriesToBackup.Add(directory);
                backupDirectoryCount++;
            }
            else
            {
                Console.WriteLine(string.Format("\nDer folgende Backupverzeichnispfad bleibt unbearbeitet, da er nicht existiert: \n{0}", directory));
            }
        }
        /// <summary>
        /// prüft, ob es genau einen Passphrase, genau ein Destination-Verzeichnis und mindestens ein Backupverzeichnis gibt, inkl. ggf. Fehlermeldungen
        /// </summary>
        /// <param name="passphraseCount"></param>
        /// <param name="backupDestinationDirectoryCount"></param>
        /// <param name="backupDirectoryCount"></param>
        /// <returns></returns>
        private static bool CheckPreconditions(int passphraseCount, int backupDestinationDirectoryCount, int backupDirectoryCount)
        {
            bool result = true;
            if (!((passphraseCount == 1) && (backupDestinationDirectoryCount == 1) && (backupDirectoryCount > 0)))
            {
                result = false;
                if (passphraseCount != 1)
                    Console.WriteLine("\nEs wurden {0} Passphrases gefunden (/passphrase oder /passphrasefile),\nABER es muss GENAU EIN Passphrase sein.", passphraseCount);
                if (backupDestinationDirectoryCount != 1)
                    Console.WriteLine("\nEs wurden {0} Verzeichnisse für den/die zukuenftigen Backup-Zips gefunden (/destination),\nABER es muss GENAU EIN Verzeichnis für alle Zips sein.", backupDestinationDirectoryCount);
                if (backupDirectoryCount == 0)
                    Console.WriteLine("\nEs wurde kein gueltiges Backupverzeichnis gefunden.");
            }
            return result;
        }
        /// <summary>
        /// erstellt ein neues (Ziel-)Verzeichnis inkl. Pfad-Verzeichnisse
        /// </summary>
        /// <param name="backupPath"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        private static bool CreateDirectory(string backupPath)
        {
            bool result = true;
            //Hier brauche ich einen Tipp, wie man das verbessern kann. ... der erstellt Verzeichnissepfade wie "\Sourcesbackup\"	und zwar als C:\Users\i.smemann\Documents\Visual Studio 2015\Backup Files\Sourcesbackup\
            //string[] prefixes = { @"a:\", @"b:\", @"c:\", @"d:\", @"e:\", @"f:\", @"g:\", @"h:\", @"i:\", @"j:\", @"k:\", @"l:\", @"m:\", @"n:\", @"o:\", @"p:\", @"q:\", @"r:\", @"s:\", @"t:\", @"u:\", @"v:\", @"w:\", @"x:\", @"y:\", @"z:\" };
            //if (prefixes.Any(prefix => backupPath.ToLower().StartsWith(prefix)))
            if (Regex.IsMatch(backupPath, @"^[a-zA-Z]:\.*"))
            {
                try
                {
                    Directory.CreateDirectory(backupPath);
                }
                catch (Exception e)
                {
                    result = false;
                    Console.WriteLine("\nFehler beim Erstellen des Verzeichnisses:\n{0}\n{1}", backupPath, e.ToString());
                }
            }
            else
            {
                result = false;
                Console.WriteLine("\nDas Verzeichnis: {0} \nbeginnt nicht mit der Kombination: EIN Buchstabe und dann \"{1}\"", backupPath, @":\");
            }
            return result;
        }
        /// <summary>
        /// macht ein (verschlüsseltes Zip-)Backup von dem jeweiligen Verzeichnis (anhand der Filter-Datei von speziellen Dateien, ansonsten von alle)
        /// </summary>
        /// <param name="sourceRootDirectory"></param>
        /// <param name="copyCount"></param>
        /// <param name="errorCount"></param>
        /// <param name="passphrase"></param>
        /// <param name="destinationDirectory"></param>
        /// <returns></returns>
        private static bool DoBackup(string sourceRootDirectory, ref int copyCount, ref int errorCount, string passphrase, string destinationDirectory)
        {
            bool result = true;
            string backupName = ExtractBackupName(sourceRootDirectory);
            List<FilterRule> rules = GetFilterRules(sourceRootDirectory, ref result);
            if (result && rules != null)
            {
                CopySourceFilesToBackupTemp(sourceRootDirectory, backupName, rules, ref copyCount, ref errorCount, destinationDirectory);
                if (copyCount > 0)
                {
                    string backupDestinationRoot = Path.Combine(destinationDirectory, backupName);
                    string zipFileName = CreateZipArchive(backupDestinationRoot);
                    DeleteBackupTemp(backupDestinationRoot);
                    CreateEncryptZipFile(zipFileName, passphrase);
                    Console.WriteLine("\nAus dem Verzeichnis \"{0}\" wurden \n{1} Dateien kopiert und {2} Kopierversuche sind fehlgeschlagen.", sourceRootDirectory, copyCount, errorCount);
                    Console.WriteLine("\nSpeicherort des verschluesselten Backups: {0}.\n\n", destinationDirectory);
                }
                else
                {
                    result = false;
                    if (errorCount == 0)
                        Console.WriteLine("\nKeine Datei wurde kopiert und kein Kopierversuch ist fehlgeschlagen.\nTipp: Bitte ueberpruefen Sie ihre Filter-Datei.");
                    else
                        Console.WriteLine("\nAlle Kopierversuche sind fehlgeschlagen.");
                }
            }
            else
            {
                Console.WriteLine("\nDaher konnte leider kein Backup von diesem Verzeichnis gemacht werden.");
                result = false;
            }
            return result;
        }
        /// <summary>
        /// extrahiert den BackupNamen
        /// </summary>
        /// <param name="sourceRootDirectory"></param>
        /// <returns></returns>
        public static string ExtractBackupName(string sourceRootDirectory)
        {
            while ((sourceRootDirectory.IndexOf(@"\") != sourceRootDirectory.Length - 1) && (sourceRootDirectory.IndexOf(@"\") != -1))
            {
                sourceRootDirectory = sourceRootDirectory.Substring(sourceRootDirectory.IndexOf(@"\") + 1);
            }
            string backupName = sourceRootDirectory.Replace(@"\", "");
            return backupName;
        }
        /// <summary>
        /// gibt die Backup-Regeln zurück
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<FilterRule> GetFilterRules(string path, ref bool success)
        {
            List<FilterRule> result = new List<FilterRule>();
            string filterFileName = Path.Combine(path, "sourcebackup.settings");

            if (File.Exists(filterFileName))
            {
                //Datei einlesen
                success = true;
                string[] filterRules = ReadFilterFile(filterFileName, ref success);
                //Daten als FilterRule speichern
                int i = 0;
                while (success && i < filterRules.Length)
                {
                    success = ReadFilterRules(ref result, filterRules, i);
                    i++;
                }
            }
            else
            {
                // Standardregel hinzufügen, falls keine Filter-Datei gefunden wurde
                result.Add(new PatternFilterRule() { RuleData = "*.*", Behaviour = FilterRule.BehaviourType.Include });
                Console.WriteLine("\nEs wurde keine Filter-Datei gefunden, daher werden alle Dateien kopiert.");
            }
            return result;
        }
        /// <summary>
        /// öffnet die Filter-Datei und liest die Daten ein
        /// </summary>
        /// <param name="filterFileName"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        private static string[] ReadFilterFile(string filterFileName, ref bool success)
        {
            string[] filterRules = null;
            try
            {
                filterRules = File.ReadAllLines(filterFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nFehler beim Oeffnen der Filter-Datei:\n{0}\n{1}", filterFileName, e.ToString());
                success = false;
            }
            return filterRules;
        }
        /// <summary>
        /// liest die einzelnen Regeln ins Programm ein (als FilterRule)
        /// </summary>
        /// <param name="filterRuleList"></param>
        /// <param name="filterRules"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static bool ReadFilterRules(ref List<FilterRule> filterRuleList, string[] filterRules, int i)
        {
            bool result = true;
            // Großbuchstaben werden an dieser Stelle durchgelassen // dass hier die Leerzeichen an bestimmten Stellen mit überprüft werden, ist für das Extrahieren der Einzelteile der Regel entscheidend (s.u.)
            if ((filterRules[i].ToLowerInvariant().Contains("include ") ^ filterRules[i].ToLowerInvariant().Contains("exclude "))
                && (filterRules[i].ToLowerInvariant().Contains(" pattern ") ^ filterRules[i].ToLowerInvariant().Contains(" directory ")))
            {
                //extrahiert die Einzelteile der Regel und entfernt dabei alle unnötigen Leerzeichen (behält aber die wichtigen) und schreibt alles klein
                string line = filterRules[i];
                string firstPart = SplitLineInFilterRules(ref line);
                string secondPart = SplitLineInFilterRules(ref line);
                string lastPart = line.Trim();

                string ruleName = secondPart.ToLowerInvariant();
                FilterRule rule;
// Anmerkung 1: so etwas wie "include pattern e" wird nicht als Fehler erkannt! ... setzt aber vermutlich auch keine Datei true
// Anmerkung 2: so etwas wie "include pattern /nd" wird hier nicht als Fehler erkannt! ... löst aber in FindFilesPatternToRegex die Exception"Der Regelparameter beinhaltet illegale Zeichen." aus...
// ... da dann keine Kopien erstellt werden, wird der Main() (an der zweiten Möglichkeit) ein fataler Fehler ausgelöst (das Programm hat dann nichts Neues erstellt)
                if (FilterRules.Rules.TryGetValue(ruleName, out rule) && !string.IsNullOrWhiteSpace(lastPart))
                {
                    rule.Behaviour = firstPart.ToLowerInvariant() == "include" ? FilterRule.BehaviourType.Include : FilterRule.BehaviourType.Exclude;
                    rule.RuleData = lastPart;
                    if (ruleName == "pattern")
                    {
                        filterRuleList.Add(new PatternFilterRule() { RuleData = rule.RuleData, Behaviour = rule.Behaviour });
                    }
                    else
                    {
                        filterRuleList.Add(new DirectoryFilterRule() { RuleData = rule.RuleData, Behaviour = rule.Behaviour });
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("\nUnbekannte Regel: Regelname \"{0}\" ist unbekannt und/oder es gibt keinen Regelparameter.", ruleName));
                    result = false;
                }
            }
            else
            {
                Console.WriteLine(string.Format("\nUnbekannte Regel: include/exclude und/oder pattern/directory fehlen in mindestens einer Regelzeile."));
                result = false;
            }
            return result;
        }
        /// <summary>
        /// zerlegt die Regel (line) in ihre drei Bestandteile
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static string SplitLineInFilterRules(ref string line)
        {
            while (line.IndexOf(' ') == 0)
            {
                line = line.TrimStart(' ');
            }
            string firstPartOfLine = line.Remove(line.IndexOf(' '));
            string restOfLine = line.Substring(line.IndexOf(' '));
            line = restOfLine;
            return firstPartOfLine;
        }
        /// <summary>
        /// verwendet FindFilesToCopy und DoTempCopy, um die richtigen Dateien zu finden und dann zu kopieren
        /// </summary>
        /// <param name="sourceRootDirectory"></param>
        /// <param name="backupName"></param>
        /// <param name="rules"></param>
        /// <param name="copyCount"></param>
        /// <param name="errorCount"></param>
        /// <param name="backupDestinationDirectory"></param>
        private static void CopySourceFilesToBackupTemp(string sourceRootDirectory, string backupName, List<FilterRule> rules, ref int copyCount, ref int errorCount, string backupDestinationDirectory)
        {
            // hier Verzeichnis durchlaufen (inkl. Unterverzeichnisse) und s.u. was damit machen (-> Prüfen, ob Kopie machen oder nicht)...
            DirectoryInfo dir = new DirectoryInfo(sourceRootDirectory);
            List<string> filesToCopy = new List<string>();
            //... gibt zu kopierenden Dateien inkl. vollem Pfad zurück
            FindFilesToCopy(dir, rules, filesToCopy);
            foreach (string fileToCopy in filesToCopy)
            {
                // Pfad ohne Dateiname
                string sourcePath = Path.GetDirectoryName(fileToCopy);
                // Verzeichnisname relativ zum Ausgangsverzeichnis des Backups 
                string relativeSourcePath = sourcePath.Substring(sourceRootDirectory.Length);
                // Vollständiger Zielpfad 
                string backupDestinationPath = backupDestinationDirectory + backupName + relativeSourcePath;
                //string backupDestinationPath = Path.Combine(backupDestinationDirectory, backupName + @"\", relativeSourcePath); 
// keine Ahnung, warum hier Combine nicht funktioniert, jedenfalls macht er backupDestinationPath = relativeSourcePath :(
                DoCopyOfFile(fileToCopy, backupDestinationPath, ref copyCount, ref errorCount);
            }
        }
        /// <summary>
        /// durchläuft ein Verzeichnis mit allen Unterverzeichnissen und listet die Dateien, die zum Backup gehören, in das List<string> filesToCopy
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="rules"></param>
        /// <param name="filesToCopy"></param>
        private static void FindFilesToCopy(DirectoryInfo dir, List<FilterRule> rules, List<string> filesToCopy)
        {
            try
            {
                // Alle Verzeichnisse rekursiv durchlaufen ...
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                {
                    FindFilesToCopy(subdir, rules, filesToCopy);
                }
                // und dort alle Dateien durchlaufen
                foreach (FileInfo fi in dir.GetFiles())
                {
                    bool isIncluded = false;
                    foreach (FilterRule rule in rules)
                    {
                        rule.UpdateInclusion(ref isIncluded, fi);
                    }
                    //erst nach dem Durchlauf aller Regeln für die eine Datei (s.o.) wird hier entschieden, ob sie endgültig zum Backup dazu gehört oder nicht:
                    if (isIncluded)
                    {
                        filesToCopy.Add(fi.FullName);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nFehler beim Durchlaufen des Verzeichnisbaums: \n{0}", e.ToString());
            }
        }
        /// <summary>
        /// ertellt eine Kopie einer Datei im Backupverzeichnis
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="backupPath"></param>
        /// <param name="copyCount"></param>
        /// <param name="errorCount"></param>
        private static void DoCopyOfFile(string fileName, string backupPath, ref int copyCount, ref int errorCount)
        {
            if(CreateDirectory(backupPath))
                CopyFile(fileName, backupPath, ref copyCount, ref errorCount);
        }
        /// <summary>
        /// kopiert von A nach B und überschreibt ggf., zählt die erfolgreichen und nicht erfolgreichen Kopien
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="backupPath"></param>
        /// <param name="copyCount"></param>
        /// <param name="errorCount"></param>
        private static void CopyFile(string fileName, string backupPath, ref int copyCount, ref int errorCount)
        {
            try
            {
                File.Copy(fileName, Path.Combine(backupPath, Path.GetFileName(fileName)), overwrite: true);
                copyCount++;
            }
            catch (Exception e)
            {
                errorCount++;
                Console.WriteLine("\nDie Datei {0}\nkonnte nicht kopiert werden: \n{1}", fileName, e.ToString());
            }
        }
        /// <summary>
        /// erstellt ein Zip-Archiv im Backupverzeichnis
        /// </summary>
        /// <param name="backupDestinationRoot"></param>
        /// <returns></returns>
        private static string CreateZipArchive(string backupDestinationRoot)
        {
            // Zip-Archiv erstellen (Startverzeichnis, Zielverzeichnis)
            DateTime today = DateTime.Today;
            string zipFileName = string.Format("{0} {1}-{2}-{3}.zip", backupDestinationRoot, today.Year, today.Month, today.Day);
            int index = 1;
            // ein Zip oder ein verschlüsseltes Zip mit dem Namen existiert bereits... 
            string fileNameOfEncryptedZip = zipFileName + EncryptEnding;

            while ((File.Exists(zipFileName)) || (File.Exists(fileNameOfEncryptedZip)))
            {
                zipFileName = string.Format("{0} {1}-{2}-{3} ({4}).zip", backupDestinationRoot, today.Year, today.Month, today.Day, index++);
                fileNameOfEncryptedZip = zipFileName + EncryptEnding;
            }
            try
            {
                ZipFile.CreateFromDirectory(backupDestinationRoot, zipFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine("\nDas Zip-Archiv \n{0}\nkonnte nicht erstellt werden: \n{1}", zipFileName, e.ToString());
            }
            return zipFileName;
        }
        /// <summary>
        /// löscht die angegebene temporäre Backup-Kopie im Backupverzeichnis
        /// </summary>
        /// <param name="backupDestinationRoot"></param>
        private static void DeleteBackupTemp(string backupDestinationRoot)
        {
            // löscht das Zielverzeichnis
            try
            {
                // true zum Entfernen von Verzeichnissen, Unterverzeichnissen und Dateien in path
                Directory.Delete(backupDestinationRoot, recursive: true);  
            }
            catch (Exception e)
            {
                Console.WriteLine("\nDas Verzeichnis \n{0}\nkonnte nicht geloescht werden: \n{1}", backupDestinationRoot, e.ToString());
            }
        }
        /// <summary>
        /// erstellt ein verschlüsseltes Zip-Archiv und löscht das ursprüngliche Zip-Archiv
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="passphrase"></param>
        private static void CreateEncryptZipFile(string zipFileName, string passphrase)
        {
            if (File.Exists(zipFileName))
            {
                string fileNameOfEncryptedZip = zipFileName + EncryptEnding;
                //Schlüssel mit Passphrase erstellen und s.o. IV immer festgelegt
                byte[] key = new byte[KeyLength];
                try
                {
                    key = CreateKey(passphrase, KeyLength);
                }
                catch (Exception e){
                    Console.WriteLine("\nFehler beim Generieren des Schluessels: \n{0}", e.ToString()); }
                try
                {
                    EncryptAes(zipFileName, fileNameOfEncryptedZip, key);
                }
                catch (Exception e){
                    Console.WriteLine("\nProgrammfehler beim Schluessel oder beim IV: \n{0}", e.ToString()); }
         
                //Ursprungs-Zip 20,3 KB(20.812 Bytes) // Encrypt-Zip 20,3 KB (20.816 Bytes) // Decrypt-Zip 20,3 KB (20.812 Bytes)
                // müssen die Zip-Archive exakt gleich groß sein? das Encrypt- ist mininal größer und dann das Decrypt- wieder so groß wie das Ursprungs-Zip.
                
                // altes Zip löschen
                try
                {
                    File.Delete(zipFileName);
                }
                catch (Exception e) {
                    Console.WriteLine("\nDas Zip-Verzeichnis \n{0}\nkonnte nicht geloescht werden: \n{1}", zipFileName, e.ToString()); }
            }
        }
        /// <summary>
        /// erstellt anhand eines Passphrase einen Schlüssel
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="keyBytes"></param>
        /// <returns></returns>
        private static byte[] CreateKey(string passphrase, int keyBytes = KeyLength)
        {
            var keyGenerator = new Rfc2898DeriveBytes(passphrase, Salt, Iterations);
            return keyGenerator.GetBytes(keyBytes);
        }
        /// <summary>
        /// verschlüsselt eine (Zip-)Datei
        /// </summary>
        /// <param name="sInput"></param>
        /// <param name="sOutput"></param>
        /// <param name="key"></param>
        private static void EncryptAes(string sInput, string sOutput, byte[] key)
        {
            string zipFileName = sInput;
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            // oder
            //if (key == null || key.Length <= 0)
            //{
            //    result = false;
            //    Console.WriteLine("\nProgrammfehler beim Schluessel.");
            //}
            ////throw new ArgumentNullException("Key");
            //if (IV == null || IV.Length <= 0)
            //{
            //    result = false;
            //    Console.WriteLine("\nProgrammfehler beim IV");
            //}
            ////throw new ArgumentNullException("Key");
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = IV;
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV); 
                    // "Werkzeug" erstellen
                    FileStream fsIn = new FileStream(sInput, FileMode.Open, FileAccess.Read);
                    FileStream fsOut = new FileStream(sOutput, FileMode.OpenOrCreate, FileAccess.Write);
                    CryptoStream decryptStream = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);
                    // "Werkzeug" benutzen
                    // ein Byte[] mit der richtigen Größe für die (Zip-)Datei erstellen
                    byte[] fileData = new byte[fsIn.Length];
                    // Informationen einlesen
                    fsIn.Read(fileData, 0, fileData.Length);
                    //schreibt die verschlüsselten Informationen
                    decryptStream.Write(fileData, 0, fileData.Length);
                    // "Werzeug" schließen
                    decryptStream.Close();
                    fsIn.Close();
                    fsOut.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nFehler beim Verschluesseln: \n{0}", e.ToString());
            }
        }
    }
}
