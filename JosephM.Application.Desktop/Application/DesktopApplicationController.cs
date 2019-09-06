﻿using JosephM.Application.Application;
using JosephM.Application.ViewModel.Notification;
using JosephM.Application.ViewModel.TabArea;
using JosephM.Core.AppConfig;
using JosephM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace JosephM.Application.Desktop.Application
{
    /// <summary>
    ///     Implementation Of IApplicationController For The Desktop Application
    /// </summary>
    public class DesktopApplicationController : ApplicationControllerBase, INotifyPropertyChanged
    {
        public DesktopApplicationController(string applicationName, IDependencyResolver container)
            : base(applicationName, container)
        {
            LoadedObjects = new ObservableCollection<object>();
        }

        public ObservableCollection<object> LoadedObjects { get; set; }

        public override void Remove(object item)
        {
            DoOnMainThread(() =>
            {
                if (LoadedObjects.Contains(item))
                    LoadedObjects.Remove(item);
                RaiseAreMultipleTabsChangedEvents();
            });
        }

        private void RaiseAreMultipleTabsChangedEvents()
        {
            foreach (var loadedObject in LoadedObjects)
            {
                if (loadedObject is TabAreaViewModelBase)
                {
                    ((TabAreaViewModelBase)loadedObject).OnPropertyChanged(nameof(TabAreaViewModelBase.AreMultipleTabs));
                }
            }
        }

        public override IEnumerable<object> GetObjects()
        {
            return LoadedObjects.ToArray();
        }

        private object _activeTabItem;
        public override object ActiveTabItem
        {
            get
            {
                return _activeTabItem;
            }
            set
            {
                _activeTabItem = value;
                OnPropertyChanged(nameof(ActiveTabItem));
                foreach (var loadedObject in LoadedObjects)
                {
                    if (loadedObject is TabAreaViewModelBase)
                    {
                        ((TabAreaViewModelBase)loadedObject).OnPropertyChanged(nameof(TabAreaViewModelBase.IsActiveTabItem));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void UserMessage(string message)
        {
            DoOnMainThread(
                () => MessageBox.Show(message));
        }

        public override bool UserConfirmation(string message)
        {
            // Configure the message box to be displayed 
            var messageBoxText = message;
            const string caption = "Confirm";
            const MessageBoxButton button = MessageBoxButton.YesNo;
            const MessageBoxImage icon = MessageBoxImage.Warning;
            var result = MessageBox.Show(messageBoxText, caption, button, icon);
            return result == MessageBoxResult.Yes;
        }

        public override void NavigateTo(Type type, UriQuery uriQuery)
        {
            var resolveIt = Container.ResolveType(type);
            NavigateTo(resolveIt);
        }

        public override void NavigateTo(object item)
        {
            OnNavigatedTo(item);
            LoadedObjects.Add(item);
            ActiveTabItem = item;
            RaiseAreMultipleTabsChangedEvents();
        }

        public override string GetSaveFileName(string initialFileName, string extention)
        {
            using (var selectFolderDialog = new SaveFileDialog() { DefaultExt = extention, FileName = initialFileName, Filter = string.Format("{0} files |*{0}", extention) })
            {
                var result = selectFolderDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    return selectFolderDialog.FileName;
                }
                return null;
            }
        }

        public override string GetSaveFolderName()
        {
            using (var selectFolderDialog = new FolderBrowserDialog { ShowNewFolderButton = true })
            {
                var dialogResult = selectFolderDialog.ShowDialog();
                return dialogResult == DialogResult.OK
                    ? selectFolderDialog.SelectedPath
                    : null;
            }
        }

        public override string Version
        {
            get
            {
                try
                {
                    var rKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");

                    var insApplication = new List<string>();

                    if (rKey != null && rKey.SubKeyCount > 0)
                    {
                        insApplication = rKey.GetSubKeyNames().ToList();
                    }

                    int i = 0;

                    string result = null;

                    foreach (string appName in insApplication)
                    {

                        Microsoft.Win32.RegistryKey finalKey = rKey.OpenSubKey(insApplication[i]);

                        string installedApp = finalKey.GetValue("DisplayName")?.ToString();

                        if (installedApp == ApplicationName)
                        {
                            var thisOne = finalKey.GetValue("DisplayVersion").ToString();
                            if (result == null || VersionHelper.IsNewerVersion(thisOne, result))
                                result = thisOne;
                        }
                        i++;
                    }
                    return result;
                }
                catch(Exception ex)
                {
                    ThrowException(ex);
                }
                return null;
            }
        }

        public override void AddNotification(string id, string notification, bool isLoading = false, Dictionary<string, Action> actions = null)
        {
            var notifications = ResolveType(typeof(NotificationsViewModel)) as NotificationsViewModel;
            if (notifications != null)
            {
                notifications.SetNotification(id, notification, isLoading: isLoading, actions: actions);
            }
        }
    }
}