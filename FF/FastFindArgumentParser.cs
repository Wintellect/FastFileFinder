// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FastFindArgumentParser.cs" company="John Robbins/Wintellect">
//   (c) 2012 by John Robbins/Wintellect
// </copyright>
// <summary>
//   The fast file finder program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FastFind
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Implements the command line parsing for the Fast Find program.
    /// </summary>
    internal sealed class FastFindArgumentParser : ArgParser
    {
        /// <summary>
        /// The path flag.
        /// </summary>
        private const String PathFlag = "path";

        /// <summary>
        /// The path flag short.
        /// </summary>
        private const String PathFlagShort = "p";

        /// <summary>
        /// The use regular expressions flag.
        /// </summary>
        private const String RegExFlag = "regex";

        /// <summary>
        /// The short use regular expressions flag.
        /// </summary>
        private const String RegExFlagShort = "re";

        /// <summary>
        /// The only files flag.
        /// </summary>
        private const String IncludeDirectoryName = "includedir";

        /// <summary>
        /// The short only files flag short.
        /// </summary>
        private const String IncludeDirectoryNameShort = "i";

        /// <summary>
        /// The no statistics flag.
        /// </summary>
        private const String NoStats = "nostats";

        /// <summary>
        /// The short no stats flag.
        /// </summary>
        private const String NoStatsShort = "ns";

        /// <summary>
        /// The help flag.
        /// </summary>
        private const String HelpFlag = "help";

        /// <summary>
        /// The short help flag.
        /// </summary>
        private const String HelpFlagShort = "?";

        /// <summary>
        /// The raw patterns as they come in from the command line.
        /// </summary>
        private readonly List<String> rawPatterns;

        /// <summary>
        /// The private string to hold more detailed error information.
        /// </summary>
        private String errorMessage;

        /// <summary>
        /// Does the user want to use regular expressions?
        /// </summary>
        private Boolean useRegEx;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastFindArgumentParser"/> class. 
        /// </summary>
        public FastFindArgumentParser()
            : base(
                new[] { RegExFlag, RegExFlagShort, IncludeDirectoryName, IncludeDirectoryNameShort, NoStats, NoStatsShort, HelpFlagShort },
                new[] { PathFlag, PathFlagShort },
                false)
        {
            this.Path = String.Empty;
            this.useRegEx = false;
            this.IncludeDirectories = false;
            this.NoStatistics = false;
            this.Patterns = new List<Regex>();
            this.rawPatterns = new List<String>();
        }

        /// <summary>
        /// Gets the path to search. The default is the current directory.
        /// </summary>
        public String Path { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user only wants to include the
        /// directory name as part of the search.
        /// </summary>
        public Boolean IncludeDirectories { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user wants to see the final
        /// search stats.
        /// </summary>
        public Boolean NoStatistics { get; private set; }

        /// <summary>
        /// Gets the patterns to search for.
        /// </summary>
        public List<Regex> Patterns { get; }

        /// <summary>
        /// Reports correct command line usage.
        /// </summary>
        /// <param name="errorInfo">
        /// The string with the invalid command line option.
        /// </param>
        public override void OnUsage(String errorInfo)
        {
            ProcessModule exe = Process.GetCurrentProcess().Modules[0];
            Console.WriteLine(Constants.UsageString, exe.FileVersionInfo.FileVersion);

            if (false == String.IsNullOrEmpty(errorInfo))
            {
                Program.WriteError(Constants.ErrorSwitch, errorInfo);
            }

            if (false == String.IsNullOrEmpty(this.errorMessage))
            {
                Program.WriteError(this.errorMessage);
            }
        }

        /// <summary>
        /// Called when a switch is parsed out.
        /// </summary>
        /// <param name="switchSymbol">
        /// The switch value parsed out.
        /// </param>
        /// <param name="switchValue">
        /// The value of the switch. For flag switches this is null/Nothing.
        /// </param>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnSwitch(String switchSymbol, String switchValue)
        {
            SwitchStatus ss = SwitchStatus.NoError;

            switch (switchSymbol)
            {
                case PathFlag:
                case PathFlagShort:
                    ss = TestPath(switchValue);
                    break;

                case RegExFlag:
                case RegExFlagShort:
                    this.useRegEx = true;
                    break;

                case IncludeDirectoryName:
                case IncludeDirectoryNameShort:
                    this.IncludeDirectories = true;
                    break;

                case NoStats:
                case NoStatsShort:
                    this.NoStatistics = true;
                    break;

                case HelpFlag:
                case HelpFlagShort:
                    ss = SwitchStatus.ShowUsage;
                    break;

                default:
                    ss = SwitchStatus.Error;
                    this.errorMessage = Constants.UnknownCommandLineOption;
                    break;
            }

            return ss;
        }

        /// <summary>
        /// Called when a non-switch value is parsed out.
        /// </summary>
        /// <param name="value">
        /// The value parsed out.
        /// </param>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnNonSwitch(String value)
        {
            // Just add this to the list of patterns to search for.
            this.rawPatterns.Add(value);
            return SwitchStatus.NoError;
        }

        /// <summary>
        /// Called when parsing is finished so final sanity checking can be
        /// performed.
        /// </summary>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnDoneParse()
        {
            SwitchStatus ss = SwitchStatus.NoError;

            if (String.IsNullOrEmpty(this.Path))
            {
                this.Path = Directory.GetCurrentDirectory();
            }

            // The only error we can have is no patterns.
            if (this.rawPatterns.Count == 0)
            {
                this.errorMessage = Constants.NoPatternsSpecified;
                ss = SwitchStatus.Error;
            }
            else
            {
                // Convert all the raw patterns into regular expressions.
                for (Int32 i = 0; i < this.rawPatterns.Count; i++)
                {
                    String thePattern = this.rawPatterns[i];
                    if (false == this.useRegEx)
                    {
                        thePattern = "^" + Regex.Escape(this.rawPatterns[i]).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    }

                    try
                    {
                        Regex r = new Regex(thePattern,
                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        this.Patterns.Add(r);
                    }
                    catch (ArgumentException e)
                    {
                        // There was an error converting the command line 
                        // parameter into a regular expression. This happens
                        // when the user specified the -regex switch and they
                        // used a DOS wildcard pattern like *..
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat(CultureInfo.CurrentCulture, 
                                        Constants.InvalidRegExFmt, 
                                        thePattern, 
                                        e.Message);
                        this.errorMessage = sb.ToString();
                        ss = SwitchStatus.Error;
                        break;
                    }
                }
            }

            return ss;
        }

        /// <summary>
        /// Isolates the checking for the path parameter.
        /// </summary>
        /// <param name="pathToTest">
        /// The path value to test.
        /// </param>
        /// <returns>
        /// A valid <see cref="SwitchStatus"/> value.
        /// </returns>
        private SwitchStatus TestPath(String pathToTest)
        {
            SwitchStatus ss = SwitchStatus.Error;
            if (false == String.IsNullOrEmpty(this.Path))
            {
                this.errorMessage = Constants.PathMultipleSwitches;
                ss = SwitchStatus.Error;
            }
            else
            {
                if (Directory.Exists(pathToTest))
                {
                    this.Path = pathToTest;
                }
                else
                {
                    this.errorMessage = Constants.PathNotExist;
                    ss = SwitchStatus.Error;
                }
            }

            return ss;
        }

    }
}
