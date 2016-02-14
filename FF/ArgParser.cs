//------------------------------------------------------------------------------
// <copyright file="ArgParser.cs" company="Wintellect">
//    Copyright (c) 2002-2012 John Robbins/Wintellect -- All rights reserved.
// </copyright>
// <Project>
//    Wintellect Debugging .NET Code
// </Project>
//------------------------------------------------------------------------------

namespace FastFind
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// A command line argument parsing class.
    /// </summary>
    /// <remarks>
    /// This class is based on the WordCount version from the Framework SDK
    /// samples.  Any errors are mine.
    /// <para>
    /// There are two arrays of flags you'll pass to the constructors.  The
    /// flagSymbols are supposed to be standalone switches that toggle an option
    /// on.  The dataSymbols are for switches that take data values.  For 
    /// example, if your application needs a switch, -c, to set the count, 
    /// you'd put "c" in the dataSymbols.  This code will allow both "-c100" and 
    /// the usual "-c" "100" both to be passed on the command line.  Note that
    /// you can pass null/Nothing for dataSymbols if you don't need them.
    /// </para>
    /// </remarks>
    internal abstract class ArgParser
    {
        /// <summary>
        /// For example: "/", "-" 
        /// </summary>
        private readonly String[] switchChars;

        /// <summary>
        /// Switch character(s) that are simple flags
        /// </summary>
        private readonly String[] flagSymbols;

        /// <summary>
        /// Switch characters(s) that take parameters.  For example: -f file.
        /// This can be null if not needed.
        /// </summary>
        private readonly String[] dataSymbols;

        /// <summary>
        /// Are switches case-sensitive?
        /// </summary>
        private readonly Boolean caseSensitiveSwitches;

        /// <summary>
        /// Initializes a new instance of the ArgParser class and defaults to 
        /// "/" and "-" as the only valid switch characters
        /// </summary>
        /// <param name="flagSymbols">
        /// The array of simple flags to toggle options on or off. 
        /// </param>
        /// <param name="dataSymbols">
        /// The array of options that need data either in the next parameter or
        /// after the switch itself.  This value can be null/Nothing.
        /// </param>
        /// <param name="caseSensitiveSwitches">
        /// True if case sensitive switches are supposed to be used.
        /// </param>
        [SuppressMessage("Microsoft.Naming",
                         "CA1726:UsePreferredTerms",
                         MessageId = "flag",
            Justification = "Flag is appropriate term when dealing with command line arguments.")]
        protected ArgParser(String[] flagSymbols, String[] dataSymbols, Boolean caseSensitiveSwitches)
            : this(flagSymbols,
                dataSymbols,
                caseSensitiveSwitches,
                     new[] { "/", "-" })
        {
        }

        /// <summary>
        /// Initializes a new instance of the ArgParser class with all options
        /// specified by the caller.
        /// </summary>
        /// <param name="flagSymbols">
        /// The array of simple flags to toggle options on or off. 
        /// </param>
        /// <param name="dataSymbols">
        /// The array of options that need data either in the next parameter or
        /// after the switch itself.  This value can be null/Nothing.
        /// </param>
        /// <param name="caseSensitiveSwitches">
        /// True if case sensitive switches are supposed to be used.
        /// </param>
        /// <param name="switchChars">
        /// The array of switch characters to use.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="flagSymbols"/> or 
        /// <paramref name="switchChars"/> are invalid.
        /// </exception>
        [SuppressMessage("Microsoft.Naming",
                         "CA1726:UsePreferredTerms",
                         MessageId = "flag",
                         Justification = "Flag is appropriate term when dealing with command line arguments.")]
        protected ArgParser(String[] flagSymbols,
                            String[] dataSymbols,
                            Boolean caseSensitiveSwitches,
                            String[] switchChars)
        {
            Debug.Assert(null != flagSymbols, "null != flagSymbols");

            // Avoid assertion side effects in debug builds.
#if DEBUG
            if (null != flagSymbols)
            {
                Debug.Assert(flagSymbols.Length > 0, "flagSymbols.Length > 0");
            }
#endif
            if ((null == flagSymbols) || (0 == flagSymbols.Length))
            {
                throw new ArgumentException(Constants.ArrayMustBeValid, nameof(flagSymbols));
            }

            Debug.Assert(null != switchChars, "null != switchChars");

            // Avoid assertion side effects in debug builds.
#if DEBUG
            if (null != switchChars)
            {
                Debug.Assert(switchChars.Length > 0, "switchChars.Length > 0");
            }
#endif
            if ((null == switchChars) || (0 == switchChars.Length))
            {
                throw new ArgumentException(Constants.ArrayMustBeValid, nameof(switchChars));
            }

            this.flagSymbols = flagSymbols;
            this.dataSymbols = dataSymbols;
            this.caseSensitiveSwitches = caseSensitiveSwitches;
            this.switchChars = switchChars;
        }

        /// <summary>
        /// The status values for various internal methods.
        /// </summary>
        protected enum SwitchStatus
        {
            /// <summary>
            /// Indicates all parsing was correct.
            /// </summary>
            NoError,

            /// <summary>
            /// There was a problem.
            /// </summary>
            Error,

            /// <summary>
            /// Show the usage help.
            /// </summary>
            ShowUsage
        }

        /// <summary>
        /// Reports correct command line usage.
        /// </summary>
        /// <param name="errorInfo">
        /// The string with the invalid command line option.
        /// </param>
        public abstract void OnUsage(String errorInfo);

        /// <summary>
        /// Parses an arbitrary set of arguments.
        /// </summary>
        /// <param name="args">
        /// The string array to parse through.
        /// </param>
        /// <returns>
        /// True if parsing was correct.  
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="args"/> is null.
        /// </exception>
        public Boolean Parse(String[] args)
        {
            // Assume parsing is successful.
            SwitchStatus ss = SwitchStatus.NoError;

            Debug.Assert(null != args, "null != args");
            if (null == args)
            {
                throw new ArgumentException(Constants.InvalidParameter);
            }

            // Handle the easy case of no arguments.
            if (0 == args.Length)
            {
                ss = SwitchStatus.ShowUsage;
            }
            Int32 errorArg = -1;
            Int32 currArg;
            for (currArg = 0;
                 (ss == SwitchStatus.NoError) && (currArg < args.Length);
                 currArg++)
            {
                errorArg = currArg;

                // Determine if this argument starts with a valid switch 
                // character
                Boolean isSwitch = this.StartsWithSwitchChar(args[currArg]);
                if (isSwitch)
                {
                    // Indicates the symbol is a data symbol.
                    Boolean useDataSymbols = false;

                    // Get the argument itself.
                    String processedArg = args[currArg].Substring(1);
                    Int32 n = this.IsSwitchInArray(this.flagSymbols, processedArg);

                    // If it's not in the flags array, try the data array if 
                    // that array is not null.
                    if ((-1 == n) && (null != this.dataSymbols))
                    {
                        n = this.IsSwitchInArray(this.dataSymbols, processedArg);
                        useDataSymbols = true;
                    }

                    if (-1 != n)
                    {
                        String theSwitch;
                        String dataValue = null;

                        // If it's a flag switch.
                        if (false == useDataSymbols)
                        {
                            // This is a legal switch, notified the derived 
                            // class of this switch and its value.
                            theSwitch = this.flagSymbols[n];
                        }
                        else
                        {
                            theSwitch = this.dataSymbols[n];

                            // Look at the next parameter if it's there.
                            if (currArg + 1 < args.Length)
                            {
                                currArg++;
                                dataValue = args[currArg];

                                // Take a look at dataValue to see if it starts
                                // with a switch character. If it does, that
                                // means this data argument is empty.
                                if (this.StartsWithSwitchChar(dataValue))
                                {
                                    ss = SwitchStatus.Error;
                                    break;
                                }
                            }
                            else
                            {
                                ss = SwitchStatus.Error;
                                break;
                            }
                        }

                        ss = this.OnSwitch(theSwitch, dataValue);
                    }
                    else
                    {
                        ss = SwitchStatus.Error;
                        break;
                    }
                }
                else
                {
                    // This is not a switch, notified the derived class of this 
                    // "non-switch value"
                    ss = this.OnNonSwitch(args[currArg]);
                }
            }

            // Finished parsing arguments
            if (ss == SwitchStatus.NoError)
            {
                // No error occurred while parsing, let derived class perform a 
                // sanity check and return an appropriate status
                ss = this.OnDoneParse();
            }

            if (ss == SwitchStatus.ShowUsage)
            {
                // Status indicates that usage should be shown, show it
                this.OnUsage(null);
            }

            if (ss == SwitchStatus.Error)
            {
                String errorValue = null;
                if ((errorArg != -1) && (errorArg != args.Length))
                {
                    errorValue = args[errorArg];
                }

                // Status indicates that an error occurred, show it and the 
                // proper usage
                this.OnUsage(errorValue);
            }

            // Return whether all parsing was successful.
            return ss == SwitchStatus.NoError;
        }

        /// <summary>
        /// Called when a switch is parsed out.
        /// </summary>
        /// <param name="switchSymbol">
        /// The switch value parsed out.
        /// </param>
        /// <param name="switchValue">
        /// The value of the switch.  For flag switches this is null/Nothing.
        /// </param>
        /// <returns>
        /// One of the <see cref="SwitchStatus"/> values.
        /// </returns>
        /// <remarks>
        /// Every derived class must implement an OnSwitch method or a switch 
        /// is considered an error.
        /// </remarks>
        protected virtual SwitchStatus OnSwitch(String switchSymbol, String switchValue) => SwitchStatus.Error;

        /// <summary>
        /// Called when a non-switch value is parsed out.
        /// </summary>
        /// <param name="value">
        /// The value parsed out.
        /// </param>
        /// <returns>
        /// One of the <see cref="SwitchStatus"/> values.
        /// </returns>
        protected virtual SwitchStatus OnNonSwitch(String value) => SwitchStatus.Error;

        /// <summary>
        /// Called when parsing is finished so final sanity checking can be 
        /// performed.
        /// </summary>
        /// <returns>
        /// One of the <see cref="SwitchStatus"/> values.
        /// </returns>
        // By default, we'll assume that all parsing was an error.
        protected virtual SwitchStatus OnDoneParse() => SwitchStatus.Error;

        /// <summary>
        /// Looks to see if the switch is in the array.
        /// </summary>
        /// <param name="switchArray">
        /// The switch array.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The index of the switch.
        /// </returns>
        private Int32 IsSwitchInArray(String[] switchArray, String value)
        {
            String valueCompare = value;
            if (this.caseSensitiveSwitches)
            {
                valueCompare = value.ToUpperInvariant();
            }
            Int32 retValue = -1;

            for (Int32 n = 0; n < switchArray.Length; n++)
            {
                String currSwitch = switchArray[n];
                if (this.caseSensitiveSwitches)
                {
                    currSwitch = currSwitch.ToUpperInvariant();
                }

                if (0 == String.CompareOrdinal(valueCompare, currSwitch))
                {
                    retValue = n;
                    break;
                }
            }

            return retValue;
        }

        /// <summary>
        /// Looks to see if this string starts with a switch character.
        /// </summary>
        /// <param name="value">
        /// The string to check.
        /// </param>
        /// <returns>
        /// True if the string starts with a switch character.
        /// </returns>
        private Boolean StartsWithSwitchChar(String value)
        {
            Boolean isSwitch = false;
            for (Int32 n = 0; !isSwitch && (n < this.switchChars.Length); n++)
            {
                if (0 != String.CompareOrdinal(value, 0, this.switchChars[n], 0, 1))
                {
                    continue;
                }

                isSwitch = true;
                break;
            }

            return isSwitch;
        }
    }
}