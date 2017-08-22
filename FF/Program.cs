// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="John Robbins/Wintellect">
//   (c) 2012-2016 by John Robbins/Wintellect
// </copyright>
// <summary>
//   The fast file finder program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FastFind
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The entry point to the application.
    /// </summary>
    internal sealed class Program
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
        /// The total number of bytes the matching file consume.
        /// </summary>
        private static Int64 totalMatchesSize;

        /// <summary>
        /// The total number of files looked at.
        /// </summary>
        private static Int64 totalFiles;

        /// <summary>
        /// The total number of directories looked at.
        /// </summary>
        private static Int64 totalDirectories;

        /// <summary>
        /// The total number of bytes the files looked at consume.
        /// </summary>
        private static Int64 totalSize;

        /// <summary>
        /// The collection to hold found strings so they can be printed in batch mode.
        /// </summary>
        private static readonly BlockingCollection<String> ResultsQueue = new BlockingCollection<String>();

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
            totalMatchesSize = 0;
            totalFiles = 0;
            totalDirectories = 0;
            totalSize = 0;

            if (parsed)
            {
                var canceller = new CancellationTokenSource();

                // Fire up the searcher and batch output threads.
                var task = Task.Factory.StartNew(() => RecurseFiles(Options.Path));
                var resultsTask = Task.Factory.StartNew(() => WriteResultsBatched(canceller.Token, 200));

                task.Wait();

                // Indicate a cancel so all remaining strings get printed out.
                canceller.Cancel();
                resultsTask.Wait();

                timer.Stop();

                if (false == Options.NoStatistics)
                {
                    Console.WriteLine(Constants.TotalTimeFmt, timer.ElapsedMilliseconds.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalFilesFmt, totalFiles.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalDirectoriesFmt, totalDirectories.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalSizeFmt, totalSize.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalMatchesFmt, totalMatches.ToString("N0", CultureInfo.CurrentCulture));
                    Console.WriteLine(Constants.TotalMatchesSizeFmt, totalMatchesSize.ToString("N0", CultureInfo.CurrentCulture));
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
        /// Takes care of writing out results found in a batch manner so slow calls to 
        /// Console.WriteLine are minimized.
        /// </summary>
        /// <param name="canceller">
        /// The cancellation token.
        /// </param>
        /// <param name="batchSize">
        /// The batch size for the number of lines to write.
        /// </param>
        private static void WriteResultsBatched(CancellationToken canceller, Int32 batchSize = 10)
        {
            var sb = new StringBuilder(batchSize * 260);
            var lineCount = 0;

            try
            {
                foreach (var line in ResultsQueue.GetConsumingEnumerable(canceller))
                {
                    sb.AppendLine(line);
                    lineCount++;

                    if (lineCount > batchSize)
                    {
                        Console.Write(sb);
                        sb.Clear();
                        lineCount = 0;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Not much to do here...  
            }
            finally
            {
                if (sb.Length > 0)
                {
                    Console.Write(sb);
                }
            }
        }

        /// <summary>
        /// The method to call when a matching file/directory is found.
        /// </summary>
        /// <param name="line">
        /// The matching item to add to the output queue.
        /// </param>
        private static void QueueConsoleWriteLine(String line)
        {
            ResultsQueue.Add(line);
        }


        /// <summary>
        /// The main method that does the recursive file matching. 
        /// </summary>
        /// <param name="directory">
        /// The file directory to search.
        /// </param>
        /// <remarks>
        /// This method calls the low level Windows API because the built in .NET APIs do not
        /// support long file names. (Those greater than 260 characters).
        /// </remarks>
        static private void RecurseFiles(String directory)
        {
            String lookUpdirectory = String.Empty;
            if (directory.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                lookUpdirectory += directory.Replace(@"\\", @"\\?\UNC\") + "\\*";
            }
            else
            {
                lookUpdirectory = "\\\\?\\" + directory + "\\*";
            }
            NativeMethods.WIN32_FIND_DATA w32FindData;

            using (SafeFindFileHandle fileHandle = NativeMethods.FindFirstFileEx(lookUpdirectory,
                                                                                 NativeMethods.FINDEX_INFO_LEVELS.Basic,
                                                                                 out w32FindData,
                                                                                 NativeMethods.FINDEX_SEARCH_OPS.SearchNameMatch,
                                                                                 IntPtr.Zero,
                                                                                 NativeMethods.FindExAdditionalFlags.LargeFetch))
            {
                if (!fileHandle.IsInvalid)
                {
                    do
                    {
                        // Does this match "." or ".."? If so get out.
                        if ((w32FindData.cFileName.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                            (w32FindData.cFileName.Equals("..", StringComparison.OrdinalIgnoreCase))))
                        {
                            continue;
                        }

                        // Is this a directory? If so, queue up another task.
                        if ((w32FindData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            Interlocked.Increment(ref totalDirectories);

                            String subDirectory = Path.Combine(directory, w32FindData.cFileName);
                            if (Options.IncludeDirectories)
                            {
                                if (IsNameMatch(w32FindData.cFileName))
                                {
                                    Interlocked.Increment(ref totalMatches);
                                    QueueConsoleWriteLine(subDirectory);
                                }
                            }

                            // Recurse our way to happiness....
                            Task.Factory.StartNew(() => RecurseFiles(subDirectory), TaskCreationOptions.AttachedToParent);
                        }
                        else
                        {
                            // It's a file so look at it.
                            Interlocked.Increment(ref totalFiles);

                            Int64 fileSize = w32FindData.nFileSizeLow + ((Int64)w32FindData.nFileSizeHigh << 32);
                            Interlocked.Add(ref totalSize, fileSize);

                            String fullFile = directory;
                            if (!directory.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
                            {
                                fullFile += "\\";
                            }
                            fullFile += w32FindData.cFileName;

                            String matchName = fullFile;

                            if (false == Options.IncludeDirectories)
                            {
                                matchName = w32FindData.cFileName;
                            }

                            if (IsNameMatch(matchName))
                            {
                                Interlocked.Increment(ref totalMatches);
                                Interlocked.Add(ref totalMatchesSize, fileSize);
                                QueueConsoleWriteLine(fullFile);
                            }
                        }
                    } while (NativeMethods.FindNextFile(fileHandle, out w32FindData));
                }
            }
        }
    }
}
