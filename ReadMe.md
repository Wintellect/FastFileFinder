# Fast File Finder #

For some reason, I always seem to be looking for file and directory names. Since desktop search is a GUI and I'm a command line kind of guy, I got tired of waiting on PowerShell to slowly grind through the file system on its single thread. With lots of cores and an SSD, I shouldn't be waiting on simple file finds! On a plane trip I threw together this command line tool to speed up my file finding and give me an extra five seconds each day I used to spend waiting on file searches to finish. This little program scratched my itch and maybe someone will find it useful.

You can search with wild cards as well as regular expressions and for as many patterns as you want. It will also handle directory names if you want. Yes, it’s a full featured fast file finder. Version 2.0 is now about 30% faster and also handles very long filenames.


Here are all the command line flags and usage instructions:

    FF 2.0.0.0
    (c) 2012, John Robbins/Wintellect - john@wintellect.com
    Find file and directory names fast!

    Usage:
    FF [-regex] [-includedir] [-nostats] [-path <directory>] pattern*

    -regex            - Treat the patterns as regular expressions. The default
                        follows DOS wildcard usage. Make sure to use regex values
                        in patterns with this flag. (short: -re)
    -includedir       - Include directory names when searching for matches. The
                        default is only to look at the file name. (short: -i)
    -nostats          - Don't show the search statistics at the end. Useful when
                        you just want the list of matching files. (short: -ns)
    -path <directory> - The directory tree to search. The default is the current
                        directory. Because of command line parsing weakness in
                        Windows, don't end the directories with '\' characters
                        (short: -p)
    pattern*          - The patterns/files to search for. Specify as many patterns
                        as you want separated by spaces. Enclose patterns/files in
                        quotes to use spaces in the pattern or name.

    Examples:
    - Search the current directory tree for all .CMD files
         ff *.cmd
    - Search the Windows directory for all log and txt files
         ff -p c:\windows *.log *.txt
	