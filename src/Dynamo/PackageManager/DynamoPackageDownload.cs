﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dynamo.Utilities;
using Greg.Responses;
using Microsoft.Practices.Prism.ViewModel;

namespace Dynamo.PackageManager
{
    public class DynamoPackageDownload : NotificationObject
    {
        public enum State
        {
            Uninitialized, Downloading, Downloaded, Installing, Installed, Error
        }

        private string _errorString = "";
        public string ErrorString { get { return _errorString; } set { _errorString = value; RaisePropertyChanged("ErrorString"); } }

        private State _downloadState = State.Uninitialized;

        public State DownloadState
        {
            get { return _downloadState; }
            set
            {
                _downloadState = value;
                RaisePropertyChanged("DownloadState");
            }
        }

        public PackageHeader Header { get; private set; }
        public string Name { get { return Header.name; } }

        private string _downloadPath;
        public string DownloadPath { get { return _downloadPath; } set { _downloadPath = value; RaisePropertyChanged("DownloadPath"); } }

        private string _versionName;
        public string VersionName { get { return _versionName; } set { _versionName = value; RaisePropertyChanged("VersionName"); } }

        private DynamoPackageDownload()
        {
            
        }

        public DynamoPackageDownload(PackageHeader header, string version)
        {
            this.Header = header;
            this.DownloadPath = "";
        }

        public void Start()
        {
            dynSettings.Controller.PackageManagerClient.DownloadAndInstall(this);
        }

        public void Error(string errorString)
        {
            this.DownloadState = State.Error;
            this.ErrorString = errorString;
        }
        
        public void Done( string filePath )
        {
            this.DownloadState = State.Downloaded;
            this.DownloadPath = filePath;
        }

        private string BuildInstallDirectoryString()
        {
            // assembly_path/dynamo_packages/package_name

            Assembly dynamoAssembly = Assembly.GetExecutingAssembly();
            string location = Path.GetDirectoryName(dynamoAssembly.Location);
            return Path.Combine(location, "dynamo_packages", this.Name);

        }

        public bool Extract( out DynamoInstalledPackage pkg )
        {
            if (this.DownloadState != State.Downloaded)
            {
                pkg = null;
                return false;
            }

            this.DownloadState = State.Installing;

            // unzip, place files
            var unzipPath = Greg.Utility.FileUtilities.UnZip(DownloadPath);
            
            var installedPath = BuildInstallDirectoryString();

            Directory.CreateDirectory(installedPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(unzipPath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(unzipPath, installedPath));

            //Copy all the files
            foreach (string newPath in Directory.GetFiles(unzipPath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(unzipPath, installedPath));

            // provide handle to installed package 
            pkg = new DynamoInstalledPackage(installedPath, Header.name, VersionName);

            return true;
        }

        // cancel, install, redownload

    }

    public class DynamoInstalledPackage : NotificationObject
    {
        public string Name { get; set; }

        private string _directory;
        public string Directory { get { return _directory; } set { _directory = value; RaisePropertyChanged("Directory"); } }

        private string _versionName;
        public string VersionName { get { return _versionName; } set { _versionName = value; RaisePropertyChanged("VersionName"); } }

        public DynamoInstalledPackage(string directory, string name, string versionName )
        {
            this.Directory = directory;
            this.Name = name;
            this.VersionName = versionName;
        }

        // do everything necessary to make the host aware of the package
        // may require a restart
        public bool RegisterWithHost()
        {
            dynSettings.PackageLoader.AppendBinarySearchPath();
            DynamoLoader.LoadBuiltinTypes();

            dynSettings.PackageLoader.AppendCustomNodeSearchPaths(dynSettings.CustomNodeLoader);
            DynamoLoader.LoadCustomNodes();
            
            return false;
        }

        public DynamoInstalledPackage FromXML()
        {
            // open a
            return null;
        }

        // location of all files
        public void Uninstall()
        {
            // remove this package completely
        }

        public bool ToXML()
        {
            // open and deserialize a package
            // 
            return true;
        }
    }



}