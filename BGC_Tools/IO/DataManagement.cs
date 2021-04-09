﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BGC.IO
{
    public static class DataManagement
    {
        private static string rootDirectory = null;

        public static string defaultDirectory = "TASagentBotDemo";
        public static bool Initialized => rootDirectory is not null;

        public static void Initialize(string directory)
        {
            if (rootDirectory is not null)
            {
                throw new InvalidOperationException("DataManagement already initialized.");
            }

            rootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), directory);
        }

        /// <summary> Root directory for all users data </summary>
        private static string RootDirectory
        {
            get
            {
                if (rootDirectory == null)
                {
                    Debug.LogError("DataManagement not yet initialized.");
                    rootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), defaultDirectory);
                }

                return rootDirectory;
            }
        }

        /// <summary> Get a list of all data files in sub directory of os data directory </summary>
        public static IEnumerable<string> GetDataFiles(string dataDirectory) =>
            Directory.GetFiles(PathForDataDirectory(dataDirectory));

        /// <summary> Get a list of all data files in sub directory of os data directory </summary>
        public static IEnumerable<string> GetDataFiles(string dataDirectory, string searchPattern) =>
            Directory.GetFiles(PathForDataDirectory(dataDirectory), searchPattern);

        /// <summary> Get a list of all data files in sub directory of os data directory </summary>
        public static IEnumerable<string> GetDataFiles(string dataDirectory, string searchPattern, SearchOption searchOption) =>
            Directory.GetFiles(PathForDataDirectory(dataDirectory), searchPattern, searchOption);

        /// <summary> Returns the full path for specified datafile in a data directory </summary>
        public static string PathForDataFile(string dataDirectory, string fileName, bool create = true) =>
            Path.Combine(PathForDataDirectory(dataDirectory, create), fileName);

        /// <summary> Returns the full path to the <paramref name="dataDirectory"/> directory. </summary>
        public static string PathForDataDirectory(string dataDirectory, bool create = true)
        {
            string path = Path.Combine(RootDirectory, dataDirectory);

            if (create && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static bool DataDirectoryExists(string dataDirectory) =>
            Directory.Exists(Path.Combine(RootDirectory, dataDirectory));

        /// <summary> Returns the full path to the <paramref name="dataDirectories"/> directory. </summary>
        public static string PathForDataSubDirectory(params string[] dataDirectories)
        {
            string[] paths = new string[dataDirectories.Length + 1];
            paths[0] = RootDirectory;
            Array.Copy(
                sourceArray: dataDirectories,
                sourceIndex: 0,
                destinationArray: paths,
                destinationIndex: 1,
                length: dataDirectories.Length);

            string path = Path.Combine(paths);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
