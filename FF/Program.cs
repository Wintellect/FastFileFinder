// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="John Robbins/Wintellect">
//   (c) 2012 by John Robbins/Wintellect
// </copyright>
// <summary>
//   The fast file finder program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FastFind
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The entry point to the application.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Holds the command line options the user wanted.
        /// </summary>
        private static readonly FastFindArgumentParser Options = new FastFindArgumentParser();

        /// <summary>
        /// The total number of matching files and directories.
        /// </summary>
        private static Int64 totalMatches;

        /// <summary>
        /// The total number of files looked at.
        /// </summary>
        private static Int64 totalFiles;

        /// <summary>
        /// The total number of directories looked at.
        /// </summary>
        private static Int64 totalDirectories;

        /// <summary>
        /// The entry point function for the program.
        /// </summary>
        /// <param name="args">
        /// The command line arguments for the program.
        /// </param>
        /// <returns>
        /// 0 - Proper execution
        /// 1 - Invalid command line.
        /// </returns>
        internal static Int32 Main(String[] args)
        {
            Int32 returnValue = 0;

            Stopwatch timer = new Stopwatch();

            // Have to include the time for parsing and creating the regular
            // expressions.
            timer.Start();

            Boolean parsed = Options.Parse(args);

            totalMatches = 0;
            totalFiles = 0;
            totalDirectories = 0;

            if (parsed)
            {
                var task = Task.Factory.StartNew(() => RecurseFiles(Options.Path));
                task.Wait();
                timer.Stop();

                if (false == Options.NoStatistics)
                {
                    Console.WriteLine(Constants.TotalTimeFmt, timer.ElapsedMilliseconds.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalFilesFmt, totalFiles.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalDirectoriesFmt, totalDirectories.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalMatchesFmt, totalMatches.ToString("N0", CultureInfo.CurrentCulture));
                }
            }
            else
            {
                returnValue = 1;
            }

            return returnValue;
        }

        /// <summary>
        /// Writes a error message to the screen.
        /// </summary>
        /// <param name="message">
        /// The message to report.
        /// </param>
        /// <param name="args">
        /// Any additional items to include in the output.
        /// </param>
        internal static void WriteError(String message, params Object[] args)
        {
            ColorWriteLine(ConsoleColor.Red, message, args);
        }

        /// <summary>
        /// Writes an error message to the screen.
        /// </summary>
        /// <param name="message">
        /// The message to write.
        /// </param>
        internal static void WriteError(String message)
        {
            ColorWriteLine(ConsoleColor.Red, message, null);
        }

        /// <summary>
        /// Writes the text out in the specified foreground color.
        /// </summary>
        /// <param name="color">
        /// The foreground color to use.
        /// </param>
        /// <param name="message">
        /// The message to display.
        /// </param>
        /// <param name="args">
        /// Optional insertion arguments.
        /// </param>
        private static void ColorWriteLine(ConsoleColor color,
                                           String message,
                                           params Object[] args)
        {
            ConsoleColor currForeground = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                if (null != args)
                {
                    Console.WriteLine(message, args);
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
            finally
            {
                Console.ForegroundColor = currForeground;
            }
        }

        /// <summary>
        /// Checks to see if the name matches and of the patterns.
        /// </summary>
        /// <param name="name">
        /// The name to match.
        /// </param>
        /// <returns>
        /// True if yes, false otherwise.
        /// </returns>
        private static Boolean IsNameMatch(String name)
        {
            for (Int32 i = 0; i < Options.Patterns.Count; i++)
            {
                if (Options.Patterns[i].IsMatch(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reports all matches in a directory.
        /// </summary>
        /// <param name="directory">
        /// The directory to look at.
        /// </param>
        private static void RecurseFiles(String directory)
        {
            try
            {
                String[] files = Directory.GetFiles(directory);

                Interlocked.Add(ref totalFiles, files.Length);

                for (Int32 i = 0; i < files.Length; i++)
                {
                    String currFile = files[i];
                    if (false == Options.IncludeDirectories)
                    {
                        currFile = Path.GetFileName(currFile);
                    }

                    if (IsNameMatch(currFile))
                    {
                        Interlocked.Increment(ref totalMatches);
                        Console.WriteLine(files[i]);
                    }
                }

                // Lets look for the directories.
                String[] dirs = Directory.GetDirectories(directory);
                Interlocked.Add(ref totalDirectories, dirs.Length);

                for (Int32 i = 0; i < dirs.Length; i++)
                {
                    String currDir = dirs[i];
                    if (Options.IncludeDirectories)
                    {
                        if (IsNameMatch(currDir))
                        {
                            Interlocked.Increment(ref totalMatches);
                            Console.WriteLine(currDir);
                        }
                    }

                    // Recurse our way to happiness....
                    Task.Factory.StartNew(() => RecurseFiles(currDir), TaskCreationOptions.AttachedToParent);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // I guess I could dump out the fact that there was a directory
                // the caller doesn't have access to but it seems like overkill
                // and noise.
            }
            catch (PathTooLongException)
            {
                // Not much I can do here. The BCL methods do not support the \\?\c:\ format
                // like the raw Win32 API. I guess I could thunk down and do this on my own by
                // pInvoke.
            }
        }
    }
}
