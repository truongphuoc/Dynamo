//Copyright © Autodesk, Inc. 2012. All rights reserved.
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Nodes;
using Dynamo.PackageManager;
using Dynamo.Selection;
using Dynamo.Utilities;

namespace Dynamo.Commands
{
    public class LoginCommand : ICommand
    {
        public void Execute(object parameters) { }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameters)
        {
            return true;
        }
    }

    public class ShowNodePublishInfoCommand : ICommand
    {
        private bool _init;
        private PackageManagerPublishView _view;

        public ShowNodePublishInfoCommand()
        {
            _init = false;
        }

        public void Execute(object funcDef)
        {
            if (dynSettings.Controller.PackageManagerClient.IsLoggedIn == false)
            {
                DynamoCommands.ShowLoginCmd.Execute(null);
                dynSettings.Controller.DynamoViewModel.Log("Must login first to publish a node.");
                return;
            }

            if (!_init)
            {
                _view = new PackageManagerPublishView(dynSettings.Controller.PackageManagerPublishViewModel);

                //MVVM: we now have an event called on the current workspace view model to 
                //add the view to its outer canvas
                //dynSettings.Bench.outerCanvas.Children.Add(_view);
                //Canvas.SetBottom(_view, 0);
                //Canvas.SetRight(_view, 0);

                dynSettings.Controller.DynamoViewModel.CurrentSpaceViewModel.OnRequestAddViewToOuterCanvas(this,
                                                                                                           new ViewEventArgs
                                                                                                               (_view));

                _init = true;
            }

            if (funcDef is FunctionDefinition)
            {
                var f = funcDef as FunctionDefinition;

                dynSettings.Controller.PackageManagerPublishViewModel.FunctionDefinition =
                    f;

                // we're submitting a new version
                if (dynSettings.Controller.PackageManagerClient.LoadedPackageHeaders.ContainsKey(f))
                {
                    dynSettings.Controller.PackageManagerPublishViewModel.PackageHeader =
                        dynSettings.Controller.PackageManagerClient.LoadedPackageHeaders[f];
                }
            }
            else
            {
                dynSettings.Controller.DynamoViewModel.Log("Failed to obtain function definition from node.");
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameters)
        {
            return true;
        }
    }

    public class ShowLoginCommand : ICommand
    {
        private bool _init;

        public void Execute(object parameters)
        {
            if (!_init)
            {
                var loginView = new PackageManagerLoginView(dynSettings.Controller.PackageManagerLoginViewModel);

                //MVVM: event on current workspace model view now adds views to canvas
                //dynSettings.Bench.outerCanvas.Children.Add(loginView);
                //Canvas.SetBottom(loginView, 0);
                //Canvas.SetRight(loginView, 0);

                dynSettings.Controller.DynamoViewModel.CurrentSpaceViewModel.OnRequestAddViewToOuterCanvas(this,
                                                                                                           new ViewEventArgs
                                                                                                               (loginView));

                _init = true;
            }

            dynSettings.Controller.PackageManagerLoginViewModel.Visible = Visibility.Visible;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameters)
        {
            return true;
        }
    }

    public class RefreshRemotePackagesCommand : ICommand
    {
        public void Execute(object parameters)
        {
            dynSettings.Controller.PackageManagerClient.RefreshAvailable();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameters)
        {
            return true;
        }
    }

    public class PublishSelectedNodeCommand : ICommand
    {
        public void Execute(object parameters)
        {
            var nodeList = DynamoSelection.Instance.Selection
                                          .OfType<dynFunction>()
                                          .Select(x => x.Definition.FunctionId)
                                          .ToList();

            if (nodeList.Count != 1)
            {
                MessageBox.Show(
                    "You must select a single user-defined node.  You selected " + nodeList.Count + " nodes.",
                    "Selection Error", MessageBoxButton.OK, MessageBoxImage.Question);
                return;
            }

            if (dynSettings.Controller.CustomNodeLoader.Contains(nodeList[0]))
            {
                DynamoCommands.ShowNodeNodePublishInfoCmd.Execute(
                    dynSettings.Controller.CustomNodeLoader.GetFunctionDefinition(nodeList[0]));
            }
            else
            {
                MessageBox.Show("The selected symbol was not found in the workspace", "Selection Error",
                                MessageBoxButton.OK, MessageBoxImage.Question);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameters)
        {
            // todo: should check authentication state, connected to internet
            return true;
        }
    }

    public class PublishCurrentWorkspaceCommand : ICommand
    {
        public void Execute(object parameters)
        {
            if (dynSettings.Controller.DynamoViewModel.ViewingHomespace)
            {
                MessageBox.Show("You can't publish your the home workspace.", "Workspace Error", MessageBoxButton.OK,
                                MessageBoxImage.Question);
                return;
            }

            var currentFunDef =
                dynSettings.Controller.CustomNodeLoader.GetDefinitionFromWorkspace(
                    dynSettings.Controller.DynamoViewModel.CurrentSpace);

            if (currentFunDef != null)
                DynamoCommands.ShowNodeNodePublishInfoCmd.Execute(currentFunDef);
            else
            {
                MessageBox.Show("The selected symbol was not found in the workspace", "Selection Error",
                                MessageBoxButton.OK, MessageBoxImage.Question);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameters)
        {
            return true;
        }
    }

    public static partial class DynamoCommands
    {
        private static ShowNodePublishInfoCommand _showNodePublishInfoCmd;

        public static ShowNodePublishInfoCommand ShowNodeNodePublishInfoCmd
        {
            get { return _showNodePublishInfoCmd ?? (_showNodePublishInfoCmd = new ShowNodePublishInfoCommand()); }
        }

        private static PublishCurrentWorkspaceCommand _publishCurrentWorkspaceCmd;

        public static PublishCurrentWorkspaceCommand PublishCurrentWorkspaceCmd
        {
            get
            {
                return _publishCurrentWorkspaceCmd ?? (_publishCurrentWorkspaceCmd = new PublishCurrentWorkspaceCommand());
            }
        }

        private static PublishSelectedNodeCommand _publishSelectedNodeCmd;

        public static PublishSelectedNodeCommand PublishSelectedNodeCmd
        {
            get { return _publishSelectedNodeCmd ?? (_publishSelectedNodeCmd = new PublishSelectedNodeCommand()); }
        }

        private static RefreshRemotePackagesCommand _refreshRemotePackagesCmd;

        public static RefreshRemotePackagesCommand RefreshRemotePackagesCmd
        {
            get
            {
                return _refreshRemotePackagesCmd ?? (_refreshRemotePackagesCmd = new RefreshRemotePackagesCommand());
            }
        }

        private static ShowLoginCommand _showLoginCmd;

        public static ShowLoginCommand ShowLoginCmd
        {
            get { return _showLoginCmd ?? (_showLoginCmd = new ShowLoginCommand()); }
        }

        private static LoginCommand _loginCmd;

        public static LoginCommand LoginCmd
        {
            get { return _loginCmd ?? (_loginCmd = new LoginCommand()); }
        }
    }
}