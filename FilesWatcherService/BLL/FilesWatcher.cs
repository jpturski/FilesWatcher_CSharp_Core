using FilesWatcherService.Models;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FilesWatcherService.BLL
{
    public sealed class FilesWatcher
    {
        #region config

        static int initialTimerInterval = 5;
        static int delayedTimerIntervalAddition = 1;
        static int permittedIntervalBetweenFiles = 60;

        static bool LogFSWEvents = false;
        static bool LogFileReadyEvents = false;
        static bool FSWUseRegex = false;

        static string FSWRegex = null;

        #endregion

        #region private vars

        private List<string> _filteredFileTypes;
        private FileSystemWatcher _watcher;

        #endregion

        #region Singletone

        private static readonly FilesWatcher instance = new FilesWatcher();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static FilesWatcher()
        {
        }

        private FilesWatcher()
        {
        }

        public static FilesWatcher Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        #region events

        public static event EventHandler<FileChangeEventArgs> OnFileReady;
        public static event EventHandler<string> OnNewMessage;

        #endregion

        public static ConcurrentDictionary<string, FileChangeItem> filesEvents = new ConcurrentDictionary<string, FileChangeItem>();

        public void Watch(string folderSource, List<string> fileTypes)
        {
            Instance._filteredFileTypes = fileTypes;
            Instance._Watch(folderSource);
        }

        private void _Watch(string folderSource)
        {
            //read all settings from app.config
            ReadAllSettings();

            // If a directory is not specified, exit program.
            if (string.IsNullOrWhiteSpace(folderSource) || _filteredFileTypes == null || !_filteredFileTypes.Any())
            {
                Log.Error("Cannot Proceed without FSWSource || fileTypes");
                return;
            }

            // Create a new FileSystemWatcher and set its properties.
            _watcher = new FileSystemWatcher();

            //watcher.Path = folder;
            _watcher.EnableRaisingEvents = false;
            _watcher.IncludeSubdirectories = true;
            _watcher.InternalBufferSize = 32768; //32KB

            _watcher.Path = folderSource;

            Log.Information($"Watching Folder: {_watcher.Path}");

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            _watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.LastAccess;
            _watcher.Filter = "*.*";

            // Add event handlers.
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;

            // Begin watching.
            _watcher.EnableRaisingEvents = true;
            Log.Information("FileSystemWatcher Ready.");
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (FSWUseRegex && !Regex.IsMatch(e.Name, FSWRegex))
                return;

            try
            {
                FileInfo f = new FileInfo(e.FullPath);

                //discard event to block other file extentions...

                if (_filteredFileTypes.Any(str => f.Extension.Equals(str)))
                {
                    DateTime eventTime = DateTime.Now;
                    string fileName = e.Name;

                    if (LogFSWEvents)
                        Log.Information($"Time: {eventTime.TimeOfDay}\t ChangeType: {e.ChangeType,-14} FileName: {fileName,-50} Path: {e.FullPath} ");

                    if (filesEvents.TryGetValue(fileName, out FileChangeItem r2NetWatchItem))
                    {
                        // in update process
                        if (r2NetWatchItem.State == FileChangeItem.WatchItemState.Updating)
                        {
                            r2NetWatchItem.ResetTimer(e, eventTime);
                        }
                        // new / already reported file ready.
                        else if (r2NetWatchItem.State == FileChangeItem.WatchItemState.Idle)
                        {
                            if (!r2NetWatchItem.WaitingForNextFile(eventTime))
                            {
                                // increase timer
                                r2NetWatchItem.UpdateTimeForFileToBeReady(eventTime); //reset + interval
                            }
                            else
                            {
                                Log.Information($"FileName: {fileName} restarting count again.");
                                r2NetWatchItem.ResetTimer(e, eventTime);
                            }
                        }
                    }
                    else // new supplier file
                    {
                        var watchItem = new FileChangeItem(e, initialTimerInterval);

                        watchItem.OnFileReadyEvent += WatchItem_OnFileReady;
                        watchItem.OnNewMessageEvent += WatchItem_OnNewMessage;
                        watchItem.permittedIntervalBetweenFiles = permittedIntervalBetweenFiles;
                        filesEvents.TryAdd(watchItem.FileName, watchItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("OnFileChanged", ex);
            }
        }

        private void WatchItem_OnNewMessage(object sender, string msg)
        {
            OnNewMessage?.Invoke(this, msg);
        }

        private void WatchItem_OnFileReady(object sender, FileChangeEventArgs e)
        {
            if (LogFileReadyEvents)
                Log.Information($"Time: {DateTime.Now.TimeOfDay}\t File: {e.FileName,-50} ready.");

            OnFileReady?.Invoke(this, e);
        }
        void ReadAllSettings()
        {
            try
            {
                initialTimerInterval = int.Parse(ConfigValueProvider.Get("FSW:initialTimerInterval"));
                delayedTimerIntervalAddition = int.Parse(ConfigValueProvider.Get("FSW:delayedTimerAddition"));
                permittedIntervalBetweenFiles = int.Parse(ConfigValueProvider.Get("FSW:permittedSecondsBetweenReadyEvents"));
                LogFileReadyEvents = bool.Parse(ConfigValueProvider.Get("FSW:LogFileReadyEvents"));
                LogFSWEvents = bool.Parse(ConfigValueProvider.Get("FSW:LogFSWEvents"));
                FSWUseRegex = bool.Parse(ConfigValueProvider.Get("FSW:FSWUseRegex"));
                FSWRegex = ConfigValueProvider.Get("FSW:FSWRegex");

                Log.Information($"initialTimerInterval:[{initialTimerInterval}], delayedTimerIntervalAddition:[{delayedTimerIntervalAddition}], permittedIntervalBetweenEvents:[{permittedIntervalBetweenFiles}]");
                Log.Information($"LogFileReadyEvents:[{LogFileReadyEvents}], LogFSWEvents:[{LogFSWEvents}]");
            }
            catch (Exception e)
            {
                Log.Error($"Error reading R2NETFSWSettings settings(setting defaults...)", e);
            }
        }
    }
}