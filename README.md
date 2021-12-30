
# Watching File(s) Changes in .NET 5.0

## This wrapper class :

So, I've forked the work of https://github.com/eranltd/FilesWatcher_CSharp_Core, and did some changes:
 - Updated to .NET 5.0
 - Added Serilog for better exception handling
 - Adequated to run as Windows Service
 - Added the feature to notify a webhook when some file is changed
