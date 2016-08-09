BuildValidator, a simple virtio-win build diff tool
===================================================

= Prerequisites =

A few external tools are required to make BuildValidator work.

* sed and diff for Windows
  http://gnuwin32.sourceforge.net/packages/sed.htm
  http://gnuwin32.sourceforge.net/packages/diffutils.htm

* Microsoft sigcheck tool
  https://technet.microsoft.com/en-us/sysinternals/bb897441.aspx

* Microsoft dumpbin tool (part of Windows SDK)
  https://msdn.microsoft.com/en-us/library/c1h23y6c.aspx

* (optional) ResEdit tool for extracting resources
  http://www.resedit.net/

Make sure that all non-optional tools are installed and the paths
in the config file are correct. The config file is named App.config
and is copied to BuildValidator.exe.config during build.


= Command line =

BuildValidator accepts two or three command line arguments. The two
mandatory ones are paths to the old and new virtio-win build, optionally
preceded by filespec of files to be excluded from the diff.

Examples:

BuildValidator.exe C:\build100 C:\build101

BuildValidator.exe /exclude:*.cat C:\build100 C:\build101

BuildValidator.exe /exclude:*.inf;netkvm.* C:\build100 C:\build101


= Interpreting the output =

BuildValidator produces diff-like annotated output. It shows files missing
in the new build, extra files in the new build, and differences between
corresponding old and new files. Note that if there are no significant
differences between the builds, no output is produced.

Extra files in the new build may be mapped to existing files in the old
build using the 'substitution' mechanism. Two substitutions are currently
hard-coded in DiffProcessor.cs:

Win8.1 -> Win8
Win10 -> Win8

This means that if a file <new_dir>\Win8.1\...\somefile is found in the new
build and there is no <old_dir>\Win8.1\...\somefile, BuildValidator will also
try to use <old_dir>\Win8\...\somefile as the old file.
