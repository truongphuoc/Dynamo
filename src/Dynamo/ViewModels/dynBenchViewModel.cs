﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Dynamo.Commands;
using Dynamo.Connectors;
using Dynamo.Controls;
using Dynamo.FSchemeInterop;
using Dynamo.Nodes;
using Dynamo.PackageManager;
using Dynamo.Search;
using Dynamo.Selection;
using Dynamo.Utilities;
using Microsoft.Practices.Prism.Commands;

namespace Dynamo.Controls
{
    class dynBenchViewModel:dynViewModelBase
    {
        private DynamoModel _model;

        private string logText;
        private ConnectorType connectorType;
        private Point transformOrigin;
        private bool consoleShowing;
        private dynConnector activeConnector;
        private DynamoController controller;
        public StringWriter sw;
        private bool runEnabled = true;
        protected bool canRunDynamically = true;
        protected bool debug = false;
        protected bool dynamicRun = false;
        
        /// <summary>
        /// An observable collection of workspace view models which tracks the model
        /// </summary>
        private ObservableCollection<dynWorkspaceViewModel> _workspaces = new ObservableCollection<dynWorkspaceViewModel>();

        public DelegateCommand GoToWikiCommand { get; set; }
        public DelegateCommand GoToSourceCodeCommand { get; set; }
        public DelegateCommand ExitCommand { get; set; }
        public DelegateCommand ShowSaveImageDialogueAndSaveResultCommand { get; set; }
        public DelegateCommand ShowOpenDialogAndOpenResultCommand { get; set; }
        public DelegateCommand ShowSaveDialogIfNeededAndSaveResultCommand { get; set; }
        public DelegateCommand ShowSaveDialogAndSaveResultCommand { get; set; }
        public DelegateCommand ShowNewFunctionDialogCommand { get; set; }
        public DelegateCommand<object> OpenCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand SaveAsCommand { get; set; }
        public DelegateCommand ClearCommand { get; set; }
        public DelegateCommand HomeCommand { get; set; }
        public DelegateCommand LayoutAllCommand { get; set; }
        public DelegateCommand<object> CopyCommand { get; set; }
        public DelegateCommand<object> PasteCommand { get; set; }
        public DelegateCommand ToggleConsoleShowingCommand { get; set; }
        public DelegateCommand CancelRunCommand { get; set; }
        public DelegateCommand<object> SaveImageCommand { get; set; }
        public DelegateCommand ClearLogCommand { get; set; }
        public DelegateCommand RunExpressionCommand { get; set; }
        public DelegateCommand ShowPackageManagerCommand { get; set; }
        public DelegateCommand<object> GoToWorkspaceCommand { get; set; }
        public DelegateCommand<object> DisplayFunctionCommand { get; set; }
        public DelegateCommand<object> SetConnectorTypeCommand { get; set; }
        
        public ObservableCollection<dynWorkspaceViewModel> Workspaces
        {
            get { return _workspaces; }
            set
            {
                _workspaces = value;
                RaisePropertyChanged("Workspaces");
            }
        }

        public string LogText
        {
            get { return logText; }
            set
            {
                logText = value;
                RaisePropertyChanged("LogText");
            }
        }

        public ConnectorType ConnectorType
        {
            get { return connectorType; }
            set
            {
                connectorType = value;
                RaisePropertyChanged("ConnectorType");
            }
        }

        public Point TransformOrigin
        {
            get { return transformOrigin; }
            set
            {
                transformOrigin = value;
                RaisePropertyChanged("TransformOrigin");
            }
        }

        public bool ConsoleShowing
        {
            get { return consoleShowing; }
            set
            {
                consoleShowing = value;
                RaisePropertyChanged("ConsoleShowing");
            }
        }

        public dynConnector ActiveConnector
        {
            get { return activeConnector; }
            set
            {
                activeConnector = value;
                RaisePropertyChanged("ActiveConnector");
            }
        }

        public DynamoController Controller
        {
            get { return controller; }
            set
            {
                controller = value;
                RaisePropertyChanged("ViewModel");
            }
        }

        public bool RunEnabled
        {
            get { return runEnabled; }
            set
            {
                runEnabled = value;
                RaisePropertyChanged("RunEnabled");
            }
        }

        public virtual bool CanRunDynamically
        {
            get
            {
                //we don't want to be able to run
                //dynamically if we're in debug mode
                return !debug;
            }
            set
            {
                canRunDynamically = value;
                RaisePropertyChanged("CanRunDynamically");
            }
        }
        
        public Point CurrentOffset
        {
            get { return zoomBorder.GetTranslateTransformOrigin(); }
            set
            {
                if (zoomBorder != null)
                {
                    zoomBorder.SetTranslateTransformOrigin(value);
                }
                RaisePropertyChanged("CurrentOffset");
            }
        }

        public bool ViewingHomespace
        {
            get { return _model.CurrentSpace == _model.HomeSpace; }
        }

        public dynBenchViewModel(DynamoController controller)
        {
            //MVVM: Instantiate the model
            _model = new DynamoModel();
            _model.Workspaces.CollectionChanged += Workspaces_CollectionChanged;
            _model.PropertyChanged += _model_PropertyChanged;

            Controller = controller;
            sw = new StringWriter();
            ConnectorType = ConnectorType.BEZIER;

            GoToWikiCommand = new DelegateCommand(GoToWiki, CanGoToWiki);
            GoToSourceCodeCommand = new DelegateCommand(GoToSourceCode,  CanGoToSourceCode);
            ExitCommand = new DelegateCommand(Exit, CanExit);
            ShowSaveImageDialogueAndSaveResultCommand = new DelegateCommand(ShowSaveImageDialogueAndSaveResult, CanShowSaveImageDialogueAndSaveResult);
            ShowOpenDialogAndOpenResultCommand = new DelegateCommand(ShowOpenDialogAndOpenResult, CanShowOpenDialogAndOpenResultCommand);
            ShowSaveDialogIfNeededAndSaveResultCommand = new DelegateCommand(ShowSaveDialogIfNeededAndSaveResult, CanShowSaveDialogIfNeededAndSaveResultCommand);
            ShowSaveDialogAndSaveResultCommand = new DelegateCommand(ShowSaveDialogAndSaveResult, CanShowSaveDialogAndSaveResultCommand);
            ShowNewFunctionDialogCommand = new DelegateCommand(ShowNewFunctionDialog, CanShowNewFunctionDialogCommand);
            SaveCommand = new DelegateCommand(Save, CanSave);
            OpenCommand = new DelegateCommand<object>(Open, CanOpen);
            SaveAsCommand = new DelegateCommand<string>(SaveAs, CanSaveAs);
            ClearCommand = new DelegateCommand(Clear, CanClear);
            HomeCommand = new DelegateCommand(Home, CanGoHome);
            LayoutAllCommand = new DelegateCommand(LayoutAll, CanLayoutAll);
            CopyCommand = new DelegateCommand<object>(Copy, CanCopy);
            PasteCommand = new DelegateCommand<object>(Paste, CanPaste);
            ToggleConsoleShowingCommand = new DelegateCommand(ToggleConsoleShowing, CanToggleConsoleShowing);
            CancelRunCommand = new DelegateCommand(CancelRun, CanCancelRun);
            SaveImageCommand = new DelegateCommand<object>(SaveImage, CanSaveImage);
            ClearLogCommand = new DelegateCommand(ClearLog, CanClearLog);
            RunExpressionCommand = new DelegateCommand(RunExpression,CanRunExpression);
            ShowPackageManagerCommand = new DelegateCommand(ShowPackageManager,CanShowPackageManager);
            GoToWorkspaceCommand = new DelegateCommand<object>(GoToWorkspace, CanGoToWorkspace);
            DisplayFunctionCommand = new DelegateCommand<object>(DisplayFunction, CanDisplayFunction);
            SetConnectorTypeCommand = new DelegateCommand<object>(SetConnectorType, CanSetConnectorType);
        }

        void _model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSpace")
                RaisePropertyChanged("CanGoHome");
        }

        /// <summary>
        /// Responds to change in the workspaces collection, creating or deleting workspace model views.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Workspaces_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                        _workspaces.Add(new dynWorkspaceViewModel(item as dynWorkspace));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                        _workspaces.Remove(_workspaces.ToList().Where(x => x.Workspace == item));
                    break;
            }
        }

        private bool CanSave()
        {
            return true;
        }

        private void Open(object parameters)
        {
            string xmlPath = parameters as string;

            if (!string.IsNullOrEmpty(xmlPath))
            {
                if (dynSettings.Bench.UILocked)
                {
                    dynSettings.Controller.QueueLoad(xmlPath);
                    return;
                }

                dynSettings.Bench.LockUI();

                if (!_model.OpenDefinition(xmlPath))
                {
                    //MessageBox.Show("Workbench could not be opened.");
                    dynSettings.Bench.Log("Workbench could not be opened.");

                    //dynSettings.Writer.WriteLine("Workbench could not be opened.");
                    //dynSettings.Writer.WriteLine(xmlPath);

                    if (DynamoCommands.WriteToLogCmd.CanExecute(null))
                    {
                        DynamoCommands.WriteToLogCmd.Execute("Workbench could not be opened.");
                        DynamoCommands.WriteToLogCmd.Execute(xmlPath);
                    }
                }
                dynSettings.Bench.UnlockUI();
            }

            //clear the clipboard to avoid copying between dyns
            dynSettings.Controller.ClipBoard.Clear();
        }

        private bool CanOpen(object parameters)
        {
            return true;
        }
        
        private void ShowSaveImageDialogueAndSaveResult()
        {
            FileDialog _fileDialog;

            if (_fileDialog == null)
            {
                _fileDialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = ".png",
                    FileName = "Capture.png",
                    Filter = "PNG Image|*.png",
                    Title = "Save your Workbench to an Image",
                };
            }

            // if you've got the current space path, use it as the inital dir
            if (!string.IsNullOrEmpty(DynamoModel.Instance.CurrentSpace.FilePath))
            {
                var fi = new FileInfo(DynamoModel.Instance.CurrentSpace.FilePath);
                _fileDialog.InitialDirectory = fi.DirectoryName;
            }

            if (_fileDialog.ShowDialog() == DialogResult.OK)
            {
                DynamoCommands.SaveImageCmd.Execute(_fileDialog.FileName);
            }
        
        }

        private bool CanShowSaveImageDialogueAndSaveResult()
        {
            return true;
        }

        private void ShowOpenDialogAndOpenResult()
        {
            FileDialog _fileDialog;

            if (_fileDialog == null)
            {
                _fileDialog = new OpenFileDialog()
                {
                    Filter = "Dynamo Definitions (*.dyn; *.dyf)|*.dyn;*.dyf|All files (*.*)|*.*",
                    Title = "Open Dynamo Definition..."
                };
            }

            // if you've got the current space path, use it as the inital dir
            if (!string.IsNullOrEmpty(DynamoModel.Instance.CurrentSpace.FilePath))
            {
                var fi = new FileInfo(DynamoModel.Instance.CurrentSpace.FilePath);
                _fileDialog.InitialDirectory = fi.DirectoryName;
            }

            if (_fileDialog.ShowDialog() == DialogResult.OK)
            {
                DynamoCommands.OpenCmd.Execute(_fileDialog.FileName);
            }
        }

        private bool CanShowOpenDialogAndOpenResultCommand()
        {
            return true;
        }

        private void ShowSaveDialogIfNeededAndSaveResult()
        {
            if (DynamoModel.Instance.CurrentSpace.FilePath != null)
            {
                DynamoCommands.SaveCmd.Execute(null);
            }
            else
            {
                DynamoCommands.ShowSaveDialogAndSaveResultCmd.Execute(null);
            }
        }

        private bool CanShowSaveDialogIfNeededAndSaveResultCommand()
        {
            return true;
        }

        private void ShowSaveDialogAndSaveResult()
        {
            FileDialog _fileDialog;

            if (_fileDialog == null)
            {
                _fileDialog = new SaveFileDialog
                {
                    AddExtension = true,
                };
            }

            string ext, fltr;
            if (DynamoModel.Instance.ViewingHomespace)
            {
                ext = ".dyn";
                fltr = "Dynamo Workspace (*.dyn)|*.dyn";
            }
            else
            {
                ext = ".dyf";
                fltr = "Dynamo Function (*.dyf)|*.dyf";
            }
            fltr += "|All files (*.*)|*.*";

            _fileDialog.FileName = DynamoModel.Instance.CurrentSpace.Name + ext;
            _fileDialog.AddExtension = true;
            _fileDialog.DefaultExt = ext;
            _fileDialog.Filter = fltr;

            //if the xmlPath is not empty set the default directory
            if (!string.IsNullOrEmpty(DynamoModel.Instance.CurrentSpace.FilePath))
            {
                var fi = new FileInfo(DynamoModel.Instance.CurrentSpace.FilePath);
                _fileDialog.InitialDirectory = fi.DirectoryName;
            }

            if (_fileDialog.ShowDialog() == DialogResult.OK)
            {
                DynamoModel.Instance.SaveAs(_fileDialog.FileName);
            }
        }

        private bool CanShowSaveDialogAndSaveResultCommand()
        {
            return true;
        }

        private void ShowNewFunctionDialog()
        {
            //First, prompt the user to enter a name
            string name, category;
            string error = "";

            do
            {
                var dialog = new FunctionNamePrompt(dynSettings.Controller.SearchViewModel.Categories, error);
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                name = dialog.Text;
                category = dialog.Category;

                if (dynSettings.FunctionDict.Values.Any(x => x.Workspace.Name == name))
                {
                    error = "A function with this name already exists.";
                }
                else if (category.Equals(""))
                {
                    error = "Please enter a valid category.";
                }
                else
                {
                    error = "";
                }
            } while (!error.Equals(""));

            dynSettings.Controller.NewFunction(Guid.NewGuid(), name, category, true);
        }

        private bool CanShowNewFunctionDialogCommand()
        {
            return true;
        }

        public virtual bool DynamicRunEnabled
        {
            get
            {
                return dynamicRun; //selecting debug now toggles this on/off
            }
            set
            {
                dynamicRun = value;
                RaisePropertyChanged("DynamicRunEnabled");
            }
        }

        public virtual bool RunInDebug
        {
            get { return debug; }
            set
            {
                debug = value;

                //toggle off dynamic run
                CanRunDynamically = !debug;

                if (debug)
                    DynamicRunEnabled = false;

                RaisePropertyChanged("RunInDebug");
            }
        }

        private void GoToWiki()
        {
            System.Diagnostics.Process.Start("https://github.com/ikeough/Dynamo/wiki");
        }

        private bool CanGoToWiki()
        {
            return true;
        }

        private void GoToSourceCode()
        {
            System.Diagnostics.Process.Start("https://github.com/ikeough/Dynamo");
        }

        private bool CanGoToSourceCode()
        {
            return true;
        }

        private void Exit()
        {
            dynSettings.Bench.Close();
        }

        private bool CanExit()
        {
            return true;
        }

        private void SaveAs(object parameters)
        {
            SaveAs(parameters.ToString());
        }

        private bool CanSaveAs()
        {
            return true;
        }

        private void Clear()
        {
            dynSettings.Bench.LockUI();

            DynamoModel.Instance.CleanWorkbench();

            //don't save the file path
            DynamoModel.Instance.CurrentSpace.FilePath = "";

            dynSettings.Bench.UnlockUI();
        }

        private bool CanClear()
        {
            return true;
        }

        private void Home()
        {
            DynamoModel.Instance.ViewHomeWorkspace();
        }

        private bool CanGoHome()
        {
            return DynamoModel.Instance.CurrentSpace != DynamoModel.Instance.HomeSpace;
        }

        private void LayoutAll()
        {
            dynSettings.Bench.LockUI();
            dynSettings.Controller.CleanWorkbench();

            double x = 0;
            double y = 0;
            double maxWidth = 0;    //track max width of current column
            double colGutter = 40;     //the space between columns
            double rowGutter = 40;
            int colCount = 0;

            Hashtable typeHash = new Hashtable();

            foreach (KeyValuePair<string, TypeLoadData> kvp in dynSettings.Controller.BuiltInTypesByNickname)
            {
                Type t = kvp.Value.Type;

                object[] attribs = t.GetCustomAttributes(typeof(NodeCategoryAttribute), false);

                if (t.Namespace == "Dynamo.Nodes" &&
                    !t.IsAbstract &&
                    attribs.Length > 0 &&
                    t.IsSubclassOf(typeof(dynNode)))
                {
                    NodeCategoryAttribute elCatAttrib = attribs[0] as NodeCategoryAttribute;

                    List<Type> catTypes = null;

                    if (typeHash.ContainsKey(elCatAttrib.ElementCategory))
                    {
                        catTypes = typeHash[elCatAttrib.ElementCategory] as List<Type>;
                    }
                    else
                    {
                        catTypes = new List<Type>();
                        typeHash.Add(elCatAttrib.ElementCategory, catTypes);
                    }

                    catTypes.Add(t);
                }
            }

            foreach (DictionaryEntry de in typeHash)
            {
                List<Type> catTypes = de.Value as List<Type>;

                //add the name of the category here
                //AddNote(de.Key.ToString(), x, y, ViewModel.CurrentSpace);
                Dictionary<string, object> paramDict = new Dictionary<string, object>();
                paramDict.Add("x", x);
                paramDict.Add("y", y);
                paramDict.Add("text", de.Key.ToString());
                paramDict.Add("workspace", DynamoModel.Instance.CurrentSpace);
                DynamoCommands.AddNoteCmd.Execute(paramDict);

                y += 60;

                foreach (Type t in catTypes)
                {
                    object[] attribs = t.GetCustomAttributes(typeof(NodeNameAttribute), false);

                    NodeNameAttribute elNameAttrib = attribs[0] as NodeNameAttribute;
                    dynNode el = dynSettings.Controller.CreateInstanceAndAddNodeToWorkspace(
                           t, elNameAttrib.Name, Guid.NewGuid(), x, y,
                           DynamoModel.Instance.CurrentSpace
                        );

                    if (el == null) continue;

                    el.DisableReporting();

                    maxWidth = Math.Max(el.NodeUI.Width, maxWidth);

                    colCount++;

                    y += el.NodeUI.Height + rowGutter;

                    if (colCount > 20)
                    {
                        y = 60;
                        colCount = 0;
                        x += maxWidth + colGutter;
                        maxWidth = 0;
                    }
                }

                y = 0;
                colCount = 0;
                x += maxWidth + colGutter;
                maxWidth = 0;

            }

            dynSettings.Bench.UnlockUI();
        }

        private bool CanLayoutAll()
        {
            return true;
        }
    
        private void Copy(object parameters)
        {
            dynSettings.Controller.ClipBoard.Clear();

            foreach (ISelectable sel in DynamoSelection.Instance.Selection)
            {
                UIElement el = sel as UIElement;
                if (el != null)
                {
                    if (!dynSettings.Controller.ClipBoard.Contains(el))
                    {
                        dynSettings.Controller.ClipBoard.Add(el);

                        dynNodeUI n = el as dynNodeUI;
                        if (n != null)
                        {
                            var connectors = n.InPorts.SelectMany(x => x.Connectors)
                                .Concat(n.OutPorts.SelectMany(x => x.Connectors))
                                .Where(x => x.End != null &&
                                    x.End.Owner.IsSelected &&
                                    !dynSettings.Controller.ClipBoard.Contains(x));

                            dynSettings.Controller.ClipBoard.AddRange(connectors);
                        }
                    }
                }
            }
        }

        private bool CanCopy(object parameters)
        {
            if (DynamoSelection.Instance.Selection.Count == 0)
            {
                return false;
            }
            return true;
        }

        private void Paste(object parameters)
        {
            //make a lookup table to store the guids of the
            //old nodes and the guids of their pasted versions
            Hashtable nodeLookup = new Hashtable();

            //clear the selection so we can put the
            //paste contents in
            DynamoSelection.Instance.Selection.RemoveAll();

            var nodes = dynSettings.Controller.ClipBoard.Select(x => x).Where(x => x is dynNodeViewModel);

            var connectors = dynSettings.Controller.ClipBoard.Select(x => x).Where(x => x is dynConnector);

            foreach (dynNodeViewModel node in nodes)
            {
                //create a new guid for us to use
                Guid newGuid = Guid.NewGuid();
                nodeLookup.Add(node.GUID, newGuid);

                Dictionary<string, object> nodeData = new Dictionary<string, object>();
                nodeData.Add("x", Canvas.GetLeft(node));
                nodeData.Add("y", Canvas.GetTop(node) + 100);
                nodeData.Add("name", node.NickName);
                nodeData.Add("guid", newGuid);

                if (typeof(dynBasicInteractive<double>).IsAssignableFrom(node.NodeLogic.GetType()))
                {
                    nodeData.Add("value", (node.NodeLogic as dynBasicInteractive<double>).Value);
                }
                else if (typeof(dynBasicInteractive<string>).IsAssignableFrom(node.NodeLogic.GetType()))
                {
                    nodeData.Add("value", (node.NodeLogic as dynBasicInteractive<string>).Value);
                }
                else if (typeof(dynBasicInteractive<bool>).IsAssignableFrom(node.NodeLogic.GetType()))
                {
                    nodeData.Add("value", (node.NodeLogic as dynBasicInteractive<bool>).Value);
                }
                else if (typeof(dynVariableInput).IsAssignableFrom(node.NodeLogic.GetType()))
                {
                    //for list type nodes send the number of ports
                    //as the value - so we can setup the new node with
                    //the right number of ports
                    nodeData.Add("value", node.InPorts.Count);
                }

                dynSettings.Controller.CommandQueue.Enqueue(Tuple.Create<object, object>(DynamoCommands.CreateNodeCmd, nodeData));
            }

            //process the command queue so we have 
            //nodes to connect to
            dynSettings.Controller.ProcessCommandQueue();

            //update the layout to ensure that the visuals
            //are present in the tree to connect to
            dynSettings.Bench.UpdateLayout();

            foreach (dynConnector c in connectors)
            {
                Dictionary<string, object> connectionData = new Dictionary<string, object>();

                dynNodeUI startNode = null;

                try
                {
                    startNode = dynSettings.Controller.CurrentSpace.Nodes
                        .Select(x => x.NodeUI)
                        .Where(x => x.GUID == (Guid)nodeLookup[c.Start.Owner.GUID]).FirstOrDefault();
                }
                catch
                {
                    //don't let users paste connectors between workspaces
                    if (c.Start.Owner.NodeLogic.WorkSpace == dynSettings.Controller.CurrentSpace)
                    {
                        startNode = c.Start.Owner;
                    }
                    else
                    {
                        continue;
                    }

                }

                connectionData.Add("start", startNode);

                connectionData.Add("end", dynSettings.Controller.CurrentSpace.Nodes
                    .Select(x => x.NodeUI)
                    .Where(x => x.GUID == (Guid)nodeLookup[c.End.Owner.GUID]).FirstOrDefault());

                connectionData.Add("port_start", c.Start.Index);
                connectionData.Add("port_end", c.End.Index);

                dynSettings.Controller.CommandQueue.Enqueue(Tuple.Create<object, object>(DynamoCommands.CreateConnectionCmd, connectionData));
            }

            //process the queue again to create the connectors
            dynSettings.Controller.ProcessCommandQueue();

            foreach (DictionaryEntry de in nodeLookup)
            {
                dynSettings.Controller.CommandQueue.Enqueue(Tuple.Create<object, object>(DynamoCommands.AddToSelectionCmd,
                    dynSettings.Controller.CurrentSpace.Nodes
                    .Select(x => x.NodeUI)
                    .Where(x => x.GUID == (Guid)de.Value).FirstOrDefault()));
            }

            dynSettings.Controller.ProcessCommandQueue();

            //dynSettings.ViewModel.ClipBoard.Clear();
        }

        private bool CanPaste(object parameters)
        {
            if (dynSettings.Controller.ClipBoard.Count == 0)
            {
                return false;
            }

            return true;
        }

        private void ToggleConsoleShowing()
        {
            if (dynSettings.Bench.ConsoleShowing)
            {
                dynSettings.Bench.consoleRow.Height = new GridLength(0.0);
                dynSettings.Bench.ConsoleShowing = false;
            }
            else
            {
                dynSettings.Bench.consoleRow.Height = new GridLength(100.0);
                dynSettings.Bench.ConsoleShowing = true;
            }
        }

        private bool CanToggleConsoleShowing()
        {
            return true;
        }

        private void CancelRun()
        {
            dynSettings.Controller.RunCancelled = true;
        }

        private bool CanCancelRun()
        {
            return true;
        }

        private void SaveImage(object parameters)
        {
            string imagePath = parameters as string;

            if (!string.IsNullOrEmpty(imagePath))
            {
                Transform trans = dynSettings.Workbench.LayoutTransform;
                dynSettings.Workbench.LayoutTransform = null;
                Size size = new Size(dynSettings.Workbench.Width, dynSettings.Workbench.Height);
                dynSettings.Workbench.Measure(size);
                dynSettings.Workbench.Arrange(new Rect(size));

                //calculate the necessary width and height
                double width = 0;
                double height = 0;
                foreach (dynNodeUI n in dynSettings.Controller.Nodes.Select(x => x.NodeUI))
                {
                    Point relativePoint = n.TransformToAncestor(dynSettings.Workbench)
                          .Transform(new Point(0, 0));

                    width = Math.Max(relativePoint.X + n.Width, width);
                    height = Math.Max(relativePoint.Y + n.Height, height);
                }

                Rect rect = VisualTreeHelper.GetDescendantBounds(dynSettings.Bench.border);

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)rect.Right + 50,
                  (int)rect.Bottom + 50, 96, 96, System.Windows.Media.PixelFormats.Default);
                rtb.Render(dynSettings.Workbench);
                //endcode as PNG
                BitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var stm = File.Create(imagePath))
                {
                    pngEncoder.Save(stm);
                }
            }
        }

        private bool CanSaveImage(object parameters)
        {
            return true;
        }

        private void ClearLog()
        {
            dynSettings.Bench.sw.Flush();
            dynSettings.Bench.sw.Close();
            dynSettings.Bench.sw = new StringWriter();
            dynSettings.Bench.LogText = dynSettings.Bench.sw.ToString();
        }

        private bool CanClearLog()
        {
            return true;
        }

        private void RunExpression()
        {
            dynSettings.Controller.RunExpression(Convert.ToBoolean(parameters));
        }

        private bool CanRunExpression()
        {
            if (dynSettings.Controller == null)
            {
                return false;
            }
            return true;
        }

        private void ShowPackageManager()
        {
            dynSettings.Bench.PackageManagerLoginStateContainer.Visibility = Visibility.Visible;
            dynSettings.Bench.PackageManagerMenu.Visibility = Visibility.Visible;
        }

        private bool CanShowPackageManager()
        {
            return true;
        }

        private void GoToWorkspace(object parameter)
        {
            if (parameter is Guid && dynSettings.FunctionDict.ContainsKey((Guid)parameter))
            {
                _model.ViewCustomNodeWorkspace(dynSettings.FunctionDict[(Guid)parameter]);
            }
        }

        private bool CanGoToWorkspace(object parameter)
        {
            return true;
        }

        private void DisplayFunction(object parameters)
        {
            _model.ViewCustomNodeWorkspace((parameters as FunctionDefinition));
        }

        private bool CanDisplayFunction(object parameters)
        {
            FunctionDefinition fd = parameters as FunctionDefinition;
            if (fd == null)
            {
                return false;
            }

            return true;
        }

        private void SetConnectorType(object parameters)
        {
            if (parameters.ToString() == "BEZIER")
            {
                _model.CurrentSpace.Connectors.ForEach(x => x.ConnectorType = ConnectorType.BEZIER);
            }
            else
            {
                _model.CurrentSpace.Connectors.ForEach(x => x.ConnectorType = ConnectorType.POLYLINE);
            }
        }

        private bool CanSetConnectorType(object parameters)
        {
            //parameter object will be BEZIER or POLYLINE
            if (string.IsNullOrEmpty(parameters.ToString()))
            {
                return false;
            }
            return true;
        }

        public void Log(Exception e)
        {
            Log(e.GetType() + ":");
            Log(e.Message);
            Log(e.StackTrace);
        }

        public void Log(string message)
        {
            sw.WriteLine(message);
            LogText = sw.ToString();

            if (DynamoCommands.WriteToLogCmd.CanExecute(null))
            {
                DynamoCommands.WriteToLogCmd.Execute(message);
            }

            //MVVM: Replaced with event handler on source changed
            //if (LogScroller != null)
            //    LogScroller.ScrollToBottom();
        }

        /// <summary>
        ///     Generate an xml doc and write the workspace to the given path
        /// </summary>
        /// <param name="xmlPath">The path to save to</param>
        /// <param name="workSpace">The workspace</param>
        /// <returns>Whether the operation was successful</returns>
        private bool SaveWorkspace(string xmlPath, dynWorkspace workSpace)
        {
            Log("Saving " + xmlPath + "...");
            try
            {
                var xmlDoc = GetXmlDocFromWorkspace(workSpace, workSpace == HomeSpace);
                xmlDoc.Save(xmlPath);

                //cache the file path for future save operations
                workSpace.FilePath = xmlPath;
            }
            catch (Exception ex)
            {
                Log(ex);
                Debug.WriteLine(ex.Message + " : " + ex.StackTrace);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Generate the xml doc of the workspace from memory
        /// </summary>
        /// <param name="workSpace">The workspace</param>
        /// <returns>The generated xmldoc</returns>
        public static XmlDocument GetXmlDocFromWorkspace(dynWorkspace workSpace, bool savingHomespace)
        {
            try
            {
                //create the xml document
                var xmlDoc = new XmlDocument();
                xmlDoc.CreateXmlDeclaration("1.0", null, null);

                XmlElement root = xmlDoc.CreateElement("dynWorkspace"); //write the root element
                root.SetAttribute("X", workSpace.PositionX.ToString());
                root.SetAttribute("Y", workSpace.PositionY.ToString());

                if (!savingHomespace) //If we are not saving the home space
                {
                    root.SetAttribute("Name", workSpace.Name);
                    root.SetAttribute("Category", ((FuncWorkspace)workSpace).Category);
                    root.SetAttribute(
                            "ID",
                            dynSettings.FunctionDict.Values
                                       .First(x => x.Workspace == workSpace).FunctionId.ToString());
                }

                xmlDoc.AppendChild(root);

                XmlElement elementList = xmlDoc.CreateElement("dynElements"); //write the root element
                root.AppendChild(elementList);

                foreach (dynNode el in workSpace.Nodes)
                {
                    XmlElement dynEl = xmlDoc.CreateElement(el.GetType().ToString());
                    elementList.AppendChild(dynEl);

                    //set the type attribute
                    dynEl.SetAttribute("type", el.GetType().ToString());
                    dynEl.SetAttribute("guid", el.NodeUI.GUID.ToString());
                    dynEl.SetAttribute("nickname", el.NodeUI.NickName);
                    dynEl.SetAttribute("x", Canvas.GetLeft(el.NodeUI).ToString());
                    dynEl.SetAttribute("y", Canvas.GetTop(el.NodeUI).ToString());

                    el.SaveElement(xmlDoc, dynEl);
                }

                //write only the output connectors
                XmlElement connectorList = xmlDoc.CreateElement("dynConnectors"); //write the root element
                root.AppendChild(connectorList);

                foreach (dynNode el in workSpace.Nodes)
                {
                    foreach (dynPort port in el.NodeUI.OutPorts)
                    {
                        foreach (dynConnector c in port.Connectors.Where(c => c.Start != null && c.End != null))
                        {
                            XmlElement connector = xmlDoc.CreateElement(c.GetType().ToString());
                            connectorList.AppendChild(connector);
                            connector.SetAttribute("start", c.Start.Owner.GUID.ToString());
                            connector.SetAttribute("start_index", c.Start.Index.ToString());
                            connector.SetAttribute("end", c.End.Owner.GUID.ToString());
                            connector.SetAttribute("end_index", c.End.Index.ToString());

                            if (c.End.PortType == PortType.INPUT)
                                connector.SetAttribute("portType", "0");
                        }
                    }
                }

                //save the notes
                XmlElement noteList = xmlDoc.CreateElement("dynNotes"); //write the root element
                root.AppendChild(noteList);
                foreach (dynNote n in workSpace.Notes)
                {
                    XmlElement note = xmlDoc.CreateElement(n.GetType().ToString());
                    noteList.AppendChild(note);
                    note.SetAttribute("text", n.noteText.Text);
                    note.SetAttribute("x", Canvas.GetLeft(n).ToString());
                    note.SetAttribute("y", Canvas.GetTop(n).ToString());
                }

                return xmlDoc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + " : " + ex.StackTrace);
                return null;
            }
        }

        internal bool OpenDefinition(string xmlPath)
        {
            return OpenDefinition(
                xmlPath,
                new Dictionary<Guid, HashSet<FunctionDefinition>>(),
                new Dictionary<Guid, HashSet<Guid>>());
        }

        internal bool OpenDefinition(
            string xmlPath,
            Dictionary<Guid, HashSet<FunctionDefinition>> children,
            Dictionary<Guid, HashSet<Guid>> parents)
        {
            try
            {
                #region read xml file

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);

                string funName = null;
                string category = "";
                double cx = dynBench.CANVAS_OFFSET_X;
                double cy = dynBench.CANVAS_OFFSET_Y;
                string id = "";

                // load the header
                foreach (XmlNode node in xmlDoc.GetElementsByTagName("dynWorkspace"))
                {
                    foreach (XmlAttribute att in node.Attributes)
                    {
                        if (att.Name.Equals("X"))
                            cx = Convert.ToDouble(att.Value);
                        else if (att.Name.Equals("Y"))
                            cy = Convert.ToDouble(att.Value);
                        else if (att.Name.Equals("Name"))
                            funName = att.Value;
                        else if (att.Name.Equals("Category"))
                            category = att.Value;
                        else if (att.Name.Equals("ID"))
                        {
                            id = att.Value;
                        }
                    }
                }

                // we have a dyf and it lacks an ID field, we need to assign it
                // a deterministic guid based on its name.  By doing it deterministically,
                // files remain compatible
                if (string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(funName))
                {
                    id = GuidUtility.Create(GuidUtility.UrlNamespace, funName).ToString();
                }

                #endregion

                //If there is no function name, then we are opening a home definition
                if (funName == null)
                {
                    //View the home workspace, then open the bench file
                    if (!ViewingHomespace)
                        ViewHomeWorkspace(); //TODO: Refactor
                    return OpenWorkbench(xmlPath);
                }
                else if (dynSettings.FunctionDict.Values.Any(x => x.Workspace.Name == funName))
                {
                    Log("ERROR: Could not load definition for \"" + funName +
                              "\", a node with this name already exists.");
                    return false;
                }

                Log("Loading node definition for \"" + funName + "\" from: " + xmlPath);

                FunctionDefinition def = NewFunction(
                    Guid.Parse(id),
                    funName,
                    category.Length > 0
                        ? category
                        : BuiltinNodeCategories.MISC,
                    false, cx, cy
                    );

                dynWorkspace ws = def.Workspace;

                //this.Log("Opening definition " + xmlPath + "...");

                XmlNodeList elNodes = xmlDoc.GetElementsByTagName("dynElements");
                XmlNodeList cNodes = xmlDoc.GetElementsByTagName("dynConnectors");
                XmlNodeList nNodes = xmlDoc.GetElementsByTagName("dynNotes");

                XmlNode elNodesList = elNodes[0];
                XmlNode cNodesList = cNodes[0];
                XmlNode nNodesList = nNodes[0];

                var dependencies = new Stack<Guid>();

                #region instantiate nodes

                foreach (XmlNode elNode in elNodesList.ChildNodes)
                {
                    XmlAttribute typeAttrib = elNode.Attributes[0];
                    XmlAttribute guidAttrib = elNode.Attributes[1];
                    XmlAttribute nicknameAttrib = elNode.Attributes[2];
                    XmlAttribute xAttrib = elNode.Attributes[3];
                    XmlAttribute yAttrib = elNode.Attributes[4];

                    string typeName = typeAttrib.Value;

                    string oldNamespace = "Dynamo.Elements.";
                    if (typeName.StartsWith(oldNamespace))
                        typeName = "Dynamo.Nodes." + typeName.Remove(0, oldNamespace.Length);

                    //test the GUID to confirm that it is non-zero
                    //if it is zero, then we have to fix it
                    //this will break the connectors, but it won't keep
                    //propagating bad GUIDs
                    var guid = new Guid(guidAttrib.Value);
                    if (guid == Guid.Empty)
                    {
                        guid = Guid.NewGuid();
                    }

                    string nickname = nicknameAttrib.Value;

                    double x = Convert.ToDouble(xAttrib.Value);
                    double y = Convert.ToDouble(yAttrib.Value);

                    //Type t = Type.GetType(typeName);
                    TypeLoadData tData;
                    Type t;

                    if (!builtinTypesByTypeName.TryGetValue(typeName, out tData))
                    {
                        t = Type.GetType(typeName);
                        if (t == null)
                        {
                            Log("Error loading definition. Could not load node of type: " + typeName);
                            return false;
                        }
                    }
                    else
                        t = tData.Type;

                    dynNode el = CreateInstanceAndAddNodeToWorkspace(t, nickname, guid, x, y, ws, Visibility.Hidden);

                    if (el == null)
                        return false;

                    el.DisableReporting();
                    el.LoadElement(elNode);

                    if (el is dynFunction)
                    {
                        var fun = el as dynFunction;

                        // we've found a custom node, we need to attempt to load its guid.  
                        // if it doesn't exist (i.e. its a legacy node), we need to assign it one,
                        // deterministically
                        Guid funId;
                        try
                        {
                            funId = Guid.Parse(fun.Symbol);
                        }
                        catch
                        {
                            funId = GuidUtility.Create(GuidUtility.UrlNamespace, nicknameAttrib.Value);
                            fun.Symbol = funId.ToString();
                        }

                        FunctionDefinition funcDef;
                        if (dynSettings.FunctionDict.TryGetValue(funId, out funcDef))
                            fun.Definition = funcDef;
                        else
                            dependencies.Push(funId);
                    }
                }

                #endregion

                Bench.WorkBench.UpdateLayout();

                #region instantiate connectors

                foreach (XmlNode connector in cNodesList.ChildNodes)
                {
                    XmlAttribute guidStartAttrib = connector.Attributes[0];
                    XmlAttribute intStartAttrib = connector.Attributes[1];
                    XmlAttribute guidEndAttrib = connector.Attributes[2];
                    XmlAttribute intEndAttrib = connector.Attributes[3];
                    XmlAttribute portTypeAttrib = connector.Attributes[4];

                    var guidStart = new Guid(guidStartAttrib.Value);
                    var guidEnd = new Guid(guidEndAttrib.Value);
                    int startIndex = Convert.ToInt16(intStartAttrib.Value);
                    int endIndex = Convert.ToInt16(intEndAttrib.Value);
                    int portType = Convert.ToInt16(portTypeAttrib.Value);

                    //find the elements to connect
                    dynNode start = null;
                    dynNode end = null;

                    foreach (dynNode e in ws.Nodes)
                    {
                        if (e.NodeUI.GUID == guidStart)
                        {
                            start = e;
                        }
                        else if (e.NodeUI.GUID == guidEnd)
                        {
                            end = e;
                        }
                        if (start != null && end != null)
                        {
                            break;
                        }
                    }

                    //don't connect if the end element is an instance map
                    //those have a morphing set of inputs
                    //dynInstanceParameterMap endTest = end as dynInstanceParameterMap;

                    //if (endTest != null)
                    //{
                    //    continue;
                    //}

                    try
                    {
                        if (start != null && end != null && start != end)
                        {
                            var newConnector = new dynConnector(
                                start.NodeUI, end.NodeUI,
                                startIndex, endIndex,
                                portType, false
                                );

                            ws.Connectors.Add(newConnector);
                        }
                    }
                    catch
                    {
                        Bench.Log(string.Format("ERROR : Could not create connector between {0} and {1}.", start.NodeUI.GUID, end.NodeUI.GUID));
                    }
                }

                #endregion

                #region instantiate notes

                if (nNodesList != null)
                {
                    foreach (XmlNode note in nNodesList.ChildNodes)
                    {
                        XmlAttribute textAttrib = note.Attributes[0];
                        XmlAttribute xAttrib = note.Attributes[1];
                        XmlAttribute yAttrib = note.Attributes[2];

                        string text = textAttrib.Value;
                        double x = Convert.ToDouble(xAttrib.Value);
                        double y = Convert.ToDouble(yAttrib.Value);

                        //dynNote n = Bench.AddNote(text, x, y, ws);
                        //Bench.AddNote(text, x, y, ws);

                        var paramDict = new Dictionary<string, object>();
                        paramDict.Add("x", x);
                        paramDict.Add("y", y);
                        paramDict.Add("text", text);
                        paramDict.Add("workspace", ws);
                        DynamoCommands.AddNoteCmd.Execute(paramDict);
                    }
                }

                #endregion

                foreach (dynNode e in ws.Nodes)
                    e.EnableReporting();

                hideWorkspace(ws);

                ws.FilePath = xmlPath;

                bool canLoad = true;

                //For each node this workspace depends on...
                foreach (Guid dep in dependencies)
                {
                    canLoad = false;
                    //Dep -> Ws
                    if (children.ContainsKey(dep))
                        children[dep].Add(def);
                    else
                        children[dep] = new HashSet<FunctionDefinition> { def };

                    //Ws -> Deps
                    if (parents.ContainsKey(def.FunctionId))
                        parents[def.FunctionId].Add(dep);
                    else
                        parents[def.FunctionId] = new HashSet<Guid> { dep };
                }

                if (canLoad)
                    SaveFunction(def, false);

                PackageManagerClient.LoadPackageHeader(def, funName);
                nodeWorkspaceWasLoaded(def, children, parents);

            }
            catch (Exception ex)
            {
                Bench.Log("There was an error opening the workbench.");
                Bench.Log(ex);
                Debug.WriteLine(ex.Message + ":" + ex.StackTrace);
                CleanWorkbench();
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Save to a specific file path, if the path is null or empty, does nothing.
        ///     If successful, the CurrentSpace.FilePath field is updated as a side effect
        /// </summary>
        /// <param name="path">The path to save to</param>
        internal void SaveAs(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                if (!SaveWorkspace(path, _model.CurrentSpace))
                {
                    Log("Workbench could not be saved.");
                }
                else
                {
                    _model.CurrentSpace.FilePath = path;
                }
            }
        }

        /// <summary>
        ///     Attempts to save an element, assuming that the CurrentSpace.FilePath 
        ///     field is already  populated with a path has a filename associated with it. 
        /// </summary>
        internal void Save()
        {
            if (!String.IsNullOrEmpty(_model.CurrentSpace.FilePath))
                SaveAs(_model.CurrentSpace.FilePath);
        }

        /// <summary>
        ///     Collapse a set of nodes in the current workspace.  Has the side effects of prompting the user
        ///     first in order to obtain the name and category for the new node, 
        ///     writes the function to a dyf file, adds it to the FunctionDict, adds it to search, and compiles and 
        ///     places the newly created symbol (defining a lambda) in the Controller's FScheme Environment.  
        /// </summary>
        /// <param name="selectedNodes"> The function definition for the user-defined node </param>
        internal void CollapseNodes(IEnumerable<dynNode> selectedNodes)
        {
            Dynamo.Utilities.NodeCollapser.Collapse(selectedNodes, _model.CurrentSpace);
        }

        /// <summary>
        ///     Update a custom node after refactoring.  Updates search and all instances of the node.
        /// </summary>
        /// <param name="selectedNodes"> The function definition for the user-defined node </param>
        internal void RefactorCustomNode()
        {
            string newName = Bench.editNameBox.Text;

            if (dynSettings.FunctionDict.Values.Any(x => x.Workspace.Name == newName))
            {
                Log("ERROR: Cannot rename to \"" + newName + "\", node with same name already exists.");
                return;
            }

            Bench.workspaceLabel.Content = Bench.editNameBox.Text;
            SearchViewModel.Refactor(CurrentSpace, newName);

            //Update existing function nodes
            foreach (dynNode el in AllNodes)
            {
                if (el is dynFunction)
                {
                    var node = (dynFunction)el;

                    if (node.Definition == null)
                    {
                        node.Definition = dynSettings.FunctionDict[Guid.Parse(node.Symbol)];
                    }

                    if (!node.Definition.Workspace.Name.Equals(CurrentSpace.Name))
                        continue;

                    //Rename nickname only if it's still referring to the old name
                    if (node.NodeUI.NickName.Equals(CurrentSpace.Name))
                        node.NodeUI.NickName = newName;
                }
            }

            FSchemeEnvironment.RemoveSymbol(CurrentSpace.Name);

            //TODO: Delete old stored definition
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pluginsPath = Path.Combine(directory, "definitions");

            if (Directory.Exists(pluginsPath))
            {
                string oldpath = Path.Combine(pluginsPath, CurrentSpace.Name + ".dyf");
                if (File.Exists(oldpath))
                {
                    string newpath = FormatFileName(
                        Path.Combine(pluginsPath, newName + ".dyf")
                        );

                    File.Move(oldpath, newpath);
                }
            }

            (_model.CurrentSpace).Name = newName;

            SaveFunction(dynSettings.FunctionDict.Values.First(x => x.Workspace == CurrentSpace));
        }

        public IEnumerable<dynNode> AllNodes
        {
            get
            {
                return _model.HomeSpace.Nodes.Concat(
                    dynSettings.FunctionDict.Values.Aggregate(
                        (IEnumerable<dynNode>)new List<dynNode>(),
                        (a, x) => a.Concat(x.Workspace.Nodes)
                        )
                    );
            }
        }

        /// <summary>
        ///     Save a function.  This includes writing to a file and compiling the 
        ///     function and saving it to the FSchemeEnvironment
        /// </summary>
        /// <param name="definition">The definition to saveo</param>
        /// <param name="bool">Whether to write the function to file</param>
        /// <returns>Whether the operation was successful</returns>
        public void SaveFunction(FunctionDefinition definition, bool writeDefinition = true)
        {
            if (definition == null)
                return;

            // Get the internal nodes for the function
            dynWorkspace functionWorkspace = definition.Workspace;

            // If asked to, write the definition to file
            if (writeDefinition)
            {
                string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string pluginsPath = Path.Combine(directory, "definitions");

                try
                {
                    if (!Directory.Exists(pluginsPath))
                        Directory.CreateDirectory(pluginsPath);

                    string path = Path.Combine(pluginsPath, FormatFileName(functionWorkspace.Name) + ".dyf");
                    SaveWorkspace(path, functionWorkspace);
                    SearchViewModel.Add(definition.Workspace);
                }
                catch (Exception e)
                {
                    Log("Error saving:" + e.GetType());
                    Log(e);
                }
            }

            try
            {
                #region Find outputs

                // Find output elements for the node
                IEnumerable<dynNode> outputs = functionWorkspace.Nodes.Where(x => x is dynOutput);

                var topMost = new List<Tuple<int, dynNode>>();

                IEnumerable<string> outputNames;

                // if we found output nodes, add select their inputs
                // these will serve as the function output
                if (outputs.Any())
                {
                    topMost.AddRange(
                        outputs.Where(x => x.HasInput(0)).Select(x => x.Inputs[0]));

                    outputNames = outputs.Select(x => (x as dynOutput).Symbol);
                }
                else
                {
                    // if there are no explicitly defined output nodes
                    // get the top most nodes and set THEM as tht output
                    IEnumerable<dynNode> topMostNodes = functionWorkspace.GetTopMostNodes();

                    var outNames = new List<string>();

                    foreach (dynNode topNode in topMostNodes)
                    {
                        foreach (int output in Enumerable.Range(0, topNode.OutPortData.Count))
                        {
                            if (!topNode.HasOutput(output))
                            {
                                topMost.Add(Tuple.Create(output, topNode));
                                outNames.Add(topNode.OutPortData[output].NickName);
                            }
                        }
                    }

                    outputNames = outNames;
                }

                #endregion

                // color the node to define its connectivity
                foreach (var ele in topMost)
                {
                    ele.Item2.NodeUI.ValidateConnections();
                }

                //Find function entry point, and then compile the function and add it to our environment
                IEnumerable<dynNode> variables = functionWorkspace.Nodes.Where(x => x is dynSymbol);
                IEnumerable<string> inputNames = variables.Select(x => (x as dynSymbol).Symbol);

                INode top;
                var buildDict = new Dictionary<dynNode, Dictionary<int, INode>>();

                if (topMost.Count > 1)
                {
                    InputNode node = new ExternalFunctionNode(
                        FScheme.Value.NewList,
                        Enumerable.Range(0, topMost.Count).Select(x => x.ToString()));

                    int i = 0;
                    foreach (var topNode in topMost)
                    {
                        string inputName = i.ToString();
                        node.ConnectInput(inputName, topNode.Item2.Build(buildDict, topNode.Item1));
                        i++;
                    }

                    top = node;
                }
                else
                    top = topMost[0].Item2.BuildExpression(buildDict);

                // if the node has any outputs, we create a BeginNode in order to evaluate all of them
                // sequentially (begin evaluates a list of expressions)
                if (outputs.Any())
                {
                    var beginNode = new BeginNode();
                    List<dynNode> hangingNodes = functionWorkspace.GetTopMostNodes().ToList();
                    foreach (var tNode in hangingNodes.Select((x, index) => new { Index = index, Node = x }))
                    {
                        beginNode.AddInput(tNode.Index.ToString());
                        beginNode.ConnectInput(tNode.Index.ToString(), tNode.Node.Build(buildDict, 0));
                    }
                    beginNode.AddInput(hangingNodes.Count.ToString());
                    beginNode.ConnectInput(hangingNodes.Count.ToString(), top);

                    top = beginNode;
                }

                // make the anonymous function
                FScheme.Expression expression = Utils.MakeAnon(variables.Select(x => x.NodeUI.GUID.ToString()),
                                                               top.Compile());

                // make it accessible in the FScheme environment
                FSchemeEnvironment.DefineSymbol(definition.FunctionId.ToString(), expression);

                //Update existing function nodes which point to this function to match its changes
                foreach (dynNode el in AllNodes)
                {
                    if (el is dynFunction)
                    {
                        var node = (dynFunction)el;

                        if (node.Definition != definition)
                            continue;

                        node.SetInputs(inputNames);
                        node.SetOutputs(outputNames);
                        el.RegisterAllPorts();
                    }
                }

                //Call OnSave for all saved elements
                foreach (dynNode el in functionWorkspace.Nodes)
                    el.onSave();

            }
            catch (Exception ex)
            {
                Log(ex.GetType() + ": " + ex.Message);
            }

        }

        /// <summary>
        ///     Save a function.  This includes writing to a file and compiling the 
        ///     function and saving it to the FSchemeEnvironment
        /// </summary>
        /// <param name="definition">The definition to saveo</param>
        /// <param name="bool">Whether to write the function to file</param>
        /// <returns>Whether the operation was successful</returns>
        public string SaveFunctionOnly(FunctionDefinition definition)
        {
            if (definition == null)
                return "";

            // Get the internal nodes for the function
            dynWorkspace functionWorkspace = definition.Workspace;

            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string pluginsPath = Path.Combine(directory, "definitions");

            try
            {
                if (!Directory.Exists(pluginsPath))
                    Directory.CreateDirectory(pluginsPath);

                string path = Path.Combine(pluginsPath, FormatFileName(functionWorkspace.Name) + ".dyf");
                SaveWorkspace(path, functionWorkspace);
                return path;
            }
            catch (Exception e)
            {
                Log("Error saving:" + e.GetType());
                Log(e);
                return "";
            }

        }

        private void nodeWorkspaceWasLoaded(
            FunctionDefinition def,
            Dictionary<Guid, HashSet<FunctionDefinition>> children,
            Dictionary<Guid, HashSet<Guid>> parents)
        {
            //If there were some workspaces that depended on this node...
            if (children.ContainsKey(def.FunctionId))
            {
                //For each workspace...
                foreach (FunctionDefinition child in children[def.FunctionId])
                {
                    //Nodes the workspace depends on
                    HashSet<Guid> allParents = parents[child.FunctionId];
                    //Remove this workspace, since it's now loaded.
                    allParents.Remove(def.FunctionId);
                    //If everything the node depends on has been loaded...
                    if (!allParents.Any())
                    {
                        SaveFunction(child, false);
                        nodeWorkspaceWasLoaded(child, children, parents);
                    }
                }
            }
        }

        /// <summary>
        ///     Create a node from a type object in a given workspace.
        /// </summary>
        /// <param name="elementType"> The Type object from which the node can be activated </param>
        /// <param name="nickName"> A nickname for the node.  If null, the nickName is loaded from the NodeNameAttribute of the node </param>
        /// <param name="guid"> The unique identifier for the node in the workspace. </param>
        /// <param name="x"> The x coordinate where the dynNodeUI will be placed </param>
        /// <param name="y"> The x coordinate where the dynNodeUI will be placed</param>
        /// <returns> The newly instantiate dynNode</returns>
        public dynNode CreateInstanceAndAddNodeToWorkspace(Type elementType, string nickName, Guid guid,
            double x, double y, dynWorkspace ws,
            Visibility vis = Visibility.Visible)
        {
            try
            {
                var node = CreateNodeInstance(elementType, nickName, guid);
                //var nodeUI = node.NodeUI;

                //store the element in the elements list
                ws.Nodes.Add(node);
                node.WorkSpace = ws;

                nodeUI.Visibility = vis;

                //Bench.WorkBench.Children.Add(nodeUI);

                Canvas.SetLeft(nodeUI, x);
                Canvas.SetTop(nodeUI, y);

                //create an event on the element itself
                //to update the elements ports and connectors
                //nodeUI.PreviewMouseRightButtonDown += new MouseButtonEventHandler(UpdateElement);

                return node;
            }
            catch (Exception e)
            {
                Log("Could not create an instance of the selected type: " + elementType);
                Log(e);
                return null;
            }
        }

        /// <summary>
        ///     Create a build-in node from a type object in a given workspace.
        /// </summary>
        /// <param name="elementType"> The Type object from which the node can be activated </param>
        /// <param name="nickName"> A nickname for the node.  If null, the nickName is loaded from the NodeNameAttribute of the node </param>
        /// <param name="guid"> The unique identifier for the node in the workspace. </param>
        /// <returns> The newly instantiated dynNode</returns>
        public static dynNode CreateNodeInstance(Type elementType, string nickName, Guid guid)
        {
            var node = (dynNode)Activator.CreateInstance(elementType);

            //dynNodeUI nodeUI = node.NodeUI;

            //if (!string.IsNullOrEmpty(nickName))
            //{
            //    nodeUI.NickName = nickName;
            //}
            //else
            //{
            //    var elNameAttrib =
            //        node.GetType().GetCustomAttributes(typeof(NodeNameAttribute), true)[0] as NodeNameAttribute;
            //    if (elNameAttrib != null)
            //    {
            //        nodeUI.NickName = elNameAttrib.Name;
            //    }
            //}

            //nodeUI.GUID = guid;

            //string name = nodeUI.NickName;
            return node;
        }

        /// <summary>
        ///     Change the currently visible workspace to the home workspace
        /// </summary>
        /// <param name="symbol">The function definition for the custom node workspace to be viewed</param>
        internal void ViewHomeWorkspace()
        {
            //Step 1: Make function workspace invisible
            foreach (dynNode ele in _model.Nodes)
            {
                ele.NodeUI.Visibility = Visibility.Collapsed;
            }
            foreach (dynConnector con in _model.CurrentSpace.Connectors)
            {
                con.Visible = false;
            }
            foreach (dynNote note in _model.CurrentSpace.Notes)
            {
                note.Visibility = Visibility.Hidden;
            }

            //Step 3: Save function
            SaveFunction(dynSettings.FunctionDict.Values.FirstOrDefault(x => x.Workspace == _model.CurrentSpace));

            //Step 4: Make home workspace visible
            _model.CurrentSpace = _model.HomeSpace;

            foreach (dynNode ele in _model.Nodes)
            {
                ele.NodeUI.Visibility = Visibility.Visible;
            }
            foreach (dynConnector con in _model.CurrentSpace.Connectors)
            {
                con.Visible = true;
            }
            foreach (dynNote note in _model.CurrentSpace.Notes)
            {
                note.Visibility = Visibility.Visible;
            }

            Bench.homeButton.IsEnabled = false;

            // TODO: get this out of here
            PackageManagerClient.HidePackageControlInformation();

            Bench.workspaceLabel.Content = "Home";
            //Bench.editNameButton.Visibility = Visibility.Collapsed;
            //Bench.editNameButton.IsHitTestVisible = false;

            Bench.setHomeBackground();

            _model.CurrentSpace.OnDisplayed();
        }

        /// <summary>
        ///     Change the currently visible workspace to a custom node's workspace
        /// </summary>
        /// <param name="symbol">The function definition for the custom node workspace to be viewed</param>
        internal void ViewCustomNodeWorkspace(FunctionDefinition symbol)
        {
            if (symbol == null || _model.CurrentSpace.Name.Equals(symbol.Workspace.Name))
                return;

            dynWorkspace newWs = symbol.Workspace;

            //Make sure we aren't dragging
            Bench.WorkBench.isDragInProgress = false;
            Bench.WorkBench.ignoreClick = true;

            //Step 1: Make function workspace invisible
            foreach (dynNode ele in Nodes)
            {
                ele.NodeUI.Visibility = Visibility.Collapsed;
            }
            foreach (dynConnector con in _model.CurrentSpace.Connectors)
            {
                con.Visible = false;
            }
            foreach (dynNote note in _model.CurrentSpace.Notes)
            {
                note.Visibility = Visibility.Hidden;
            }
            //var ws = new dynWorkspace(this.elements, this.connectors, this.CurrentX, this.CurrentY);

            if (!ViewingHomespace)
            {
                //Step 2: Store function workspace in the function dictionary
                //this.FunctionDict[this.CurrentSpace.Name] = this.CurrentSpace;

                //Step 3: Save function
                SaveFunction(dynSettings.FunctionDict.Values.First(x => x.Workspace == _model.CurrentSpace));
            }

            _model.CurrentSpace = newWs;

            foreach (dynNode ele in Nodes)
            {
                ele.NodeUI.Visibility = Visibility.Visible;
            }
            foreach (dynConnector con in _model.CurrentSpace.Connectors)
            {
                con.Visible = true;
            }

            foreach (dynNote note in _model.CurrentSpace.Notes)
            {
                note.Visibility = Visibility.Visible;
            }

            //this.saveFuncItem.IsEnabled = true;
            Bench.homeButton.IsEnabled = true;
            //this.varItem.IsEnabled = true;

            Bench.workspaceLabel.Content = symbol.Workspace.Name;

            Bench.editNameButton.Visibility = Visibility.Visible;
            Bench.editNameButton.IsHitTestVisible = true;

            Bench.setFunctionBackground();

            PackageManagerClient.ShowPackageControlInformation();

            _model.CurrentSpace.OnDisplayed();
        }

        private static string FormatFileName(string filename)
        {
            return RemoveChars(
                filename,
                new[] { "\\", "/", ":", "*", "?", "\"", "<", ">", "|" }
                );
        }

        internal static string RemoveChars(string s, IEnumerable<string> chars)
        {
            foreach (string c in chars)
                s = s.Replace(c, "");
            return s;
        }

        public bool OpenWorkbench(string xmlPath)
        {
            Log("Opening home workspace " + xmlPath + "...");
            CleanWorkbench();

            try
            {
                #region read xml file

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);

                foreach (XmlNode node in xmlDoc.GetElementsByTagName("dynWorkspace"))
                {
                    foreach (XmlAttribute att in node.Attributes)
                    {
                        if (att.Name.Equals("X"))
                        {
                            //Bench.CurrentX = Convert.ToDouble(att.Value);
                            Bench.CurrentOffset = new Point(Convert.ToDouble(att.Value), Bench.CurrentOffset.Y);
                        }
                        else if (att.Name.Equals("Y"))
                        {
                            //Bench.CurrentY = Convert.ToDouble(att.Value);
                            Bench.CurrentOffset = new Point(Bench.CurrentOffset.X, Convert.ToDouble(att.Value));
                        }
                    }
                }

                XmlNodeList elNodes = xmlDoc.GetElementsByTagName("dynElements");
                XmlNodeList cNodes = xmlDoc.GetElementsByTagName("dynConnectors");
                XmlNodeList nNodes = xmlDoc.GetElementsByTagName("dynNotes");

                XmlNode elNodesList = elNodes[0];
                XmlNode cNodesList = cNodes[0];
                XmlNode nNodesList = nNodes[0];

                foreach (XmlNode elNode in elNodesList.ChildNodes)
                {
                    XmlAttribute typeAttrib = elNode.Attributes[0];
                    XmlAttribute guidAttrib = elNode.Attributes[1];
                    XmlAttribute nicknameAttrib = elNode.Attributes[2];
                    XmlAttribute xAttrib = elNode.Attributes[3];
                    XmlAttribute yAttrib = elNode.Attributes[4];

                    string typeName = typeAttrib.Value;

                    //test the GUID to confirm that it is non-zero
                    //if it is zero, then we have to fix it
                    //this will break the connectors, but it won't keep
                    //propagating bad GUIDs
                    var guid = new Guid(guidAttrib.Value);
                    if (guid == Guid.Empty)
                    {
                        guid = Guid.NewGuid();
                    }

                    string nickname = nicknameAttrib.Value;

                    double x = Convert.ToDouble(xAttrib.Value);
                    double y = Convert.ToDouble(yAttrib.Value);

                    if (typeName.StartsWith("Dynamo.Elements."))
                        typeName = "Dynamo.Nodes." + typeName.Remove(0, 16);

                    TypeLoadData tData;
                    Type t;

                    if (!builtinTypesByTypeName.TryGetValue(typeName, out tData))
                    {
                        t = Type.GetType(typeName);
                        if (t == null)
                        {
                            Log("Error loading workspace. Could not load node of type: " + typeName);
                            return false;
                        }
                    }
                    else
                        t = tData.Type;

                    dynNode el = CreateInstanceAndAddNodeToWorkspace(
                        t, nickname, guid, x, y,
                        _model.CurrentSpace
                        );

                    el.DisableReporting();

                    el.LoadElement(elNode);

                    if (ViewingHomespace)
                        el.SaveResult = true;

                    if (el is dynFunction)
                    {
                        var fun = el as dynFunction;

                        // we've found a custom node, we need to attempt to load its guid.  
                        // if it doesn't exist (i.e. its a legacy node), we need to assign it one,
                        // deterministically
                        Guid funId;
                        try
                        {
                            funId = Guid.Parse(fun.Symbol);
                        }
                        catch
                        {
                            funId = GuidUtility.Create(GuidUtility.UrlNamespace, nicknameAttrib.Value);
                            fun.Symbol = funId.ToString();
                        }

                        FunctionDefinition funcDef;
                        if (dynSettings.FunctionDict.TryGetValue(funId, out funcDef))
                            fun.Definition = funcDef;
                        else
                            fun.NodeUI.Error("No definition found.");
                    }

                    //read the sub elements
                    //set any numeric values 
                    //foreach (XmlNode subNode in elNode.ChildNodes)
                    //{
                    //   if (subNode.Name == "System.Double")
                    //   {
                    //      double val = Convert.ToDouble(subNode.Attributes[0].Value);
                    //      el.OutPortData[0].Object = val;
                    //      el.Update();
                    //   }
                    //   else if (subNode.Name == "System.Int32")
                    //   {
                    //      int val = Convert.ToInt32(subNode.Attributes[0].Value);
                    //      el.OutPortData[0].Object = val;
                    //      el.Update();
                    //   }
                    //}
                }

                dynSettings.Workbench.UpdateLayout();

                foreach (XmlNode connector in cNodesList.ChildNodes)
                {
                    XmlAttribute guidStartAttrib = connector.Attributes[0];
                    XmlAttribute intStartAttrib = connector.Attributes[1];
                    XmlAttribute guidEndAttrib = connector.Attributes[2];
                    XmlAttribute intEndAttrib = connector.Attributes[3];
                    XmlAttribute portTypeAttrib = connector.Attributes[4];

                    var guidStart = new Guid(guidStartAttrib.Value);
                    var guidEnd = new Guid(guidEndAttrib.Value);
                    int startIndex = Convert.ToInt16(intStartAttrib.Value);
                    int endIndex = Convert.ToInt16(intEndAttrib.Value);
                    int portType = Convert.ToInt16(portTypeAttrib.Value);

                    //find the elements to connect
                    dynNode start = null;
                    dynNode end = null;

                    foreach (dynNode e in Dynamo.Nodes)
                    {
                        if (e.NodeUI.GUID == guidStart)
                        {
                            start = e;
                        }
                        else if (e.NodeUI.GUID == guidEnd)
                        {
                            end = e;
                        }
                        if (start != null && end != null)
                        {
                            break;
                        }
                    }

                    //don't connect if the end element is an instance map
                    //those have a morphing set of inputs
                    //dynInstanceParameterMap endTest = end as dynInstanceParameterMap;

                    //if (endTest != null)
                    //{
                    //    continue;
                    //}

                    if (start != null && end != null && start != end)
                    {
                        var newConnector = new dynConnector(start.NodeUI, end.NodeUI,
                                                            startIndex, endIndex, portType);

                        _model.CurrentSpace.Connectors.Add(newConnector);
                    }
                }

                _model.CurrentSpace.Connectors.ForEach(x => x.Redraw());

                #region instantiate notes

                if (nNodesList != null)
                {
                    foreach (XmlNode note in nNodesList.ChildNodes)
                    {
                        XmlAttribute textAttrib = note.Attributes[0];
                        XmlAttribute xAttrib = note.Attributes[1];
                        XmlAttribute yAttrib = note.Attributes[2];

                        string text = textAttrib.Value;
                        double x = Convert.ToDouble(xAttrib.Value);
                        double y = Convert.ToDouble(yAttrib.Value);

                        //dynNote n = Bench.AddNote(text, x, y, this.CurrentSpace);
                        //Bench.AddNote(text, x, y, this.CurrentSpace);

                        var paramDict = new Dictionary<string, object>();
                        paramDict.Add("x", x);
                        paramDict.Add("y", y);
                        paramDict.Add("text", text);
                        paramDict.Add("workspace", CurrentSpace);
                        DynamoCommands.AddNoteCmd.Execute(paramDict);
                    }
                }

                #endregion

                foreach (dynNode e in CurrentSpace.Nodes)
                    e.EnableReporting();

                #endregion

                HomeSpace.FilePath = xmlPath;
            }
            catch (Exception ex)
            {
                Log("There was an error opening the workbench.");
                Log(ex);
                Debug.WriteLine(ex.Message + ":" + ex.StackTrace);
                CleanWorkbench();
                return false;
            }
            return true;
        }

        internal void CleanWorkbench()
        {
            Log("Clearing workflow...");

            //Copy locally
            List<dynNode> elements = _model.Nodes.ToList();

            foreach (dynNode el in elements)
            {
                el.DisableReporting();
                try
                {
                    el.Destroy();
                }
                catch
                {
                }
            }

            foreach (dynNode el in elements)
            {
                foreach (dynPortModel p in el.InPorts)
                {
                    for (int i = p.Connectors.Count - 1; i >= 0; i--)
                        p.Connectors[i].Kill();
                }
                foreach (dynPortModel port in el.OutPorts)
                {
                    for (int i = port.Connectors.Count - 1; i >= 0; i--)
                        port.Connectors[i].Kill();
                }

                dynSettings.Workbench.Children.Remove(el.NodeUI);
            }

            foreach (dynNote n in _model.CurrentSpace.Notes)
            {
                dynSettings.Workbench.Children.Remove(n);
            }

            _model.CurrentSpace.Nodes.Clear();
            _model.CurrentSpace.Connectors.Clear();
            _model.CurrentSpace.Notes.Clear();
            _model.CurrentSpace.Modified();
        }

        internal FunctionDefinition NewFunction(Guid id,
                                                string name,
                                                string category,
                                                bool display,
                                                double workspaceOffsetX = dynBench.CANVAS_OFFSET_X,
                                                double workspaceOffsetY = dynBench.CANVAS_OFFSET_Y)
        {
            //Add an entry to the funcdict
            var workSpace = new FuncWorkspace(
                name, category, workspaceOffsetX, workspaceOffsetY);

            List<dynNode> newElements = workSpace.Nodes;
            List<dynConnector> newConnectors = workSpace.Connectors;

            var functionDefinition = new FunctionDefinition(id)
            {
                Workspace = workSpace
            };

            dynSettings.FunctionDict[functionDefinition.FunctionId] = functionDefinition;

            // add the element to search
            SearchViewModel.Add(workSpace);

            if (display)
            {
                if (!ViewingHomespace)
                {
                    SaveFunction(dynSettings.FunctionDict.Values.First(x => x.Workspace == _model.CurrentSpace));
                }

                DynamoController.hideWorkspace(_model.CurrentSpace);
                _model.CurrentSpace = workSpace;

                //MVVM: replaced with CanGoHome property on DynamoViewModel
                //Bench.homeButton.IsEnabled = true;

                //MVVM: replaced with binding to Name on workspace view model
                //Bench.workspaceLabel.Content = CurrentSpace.Name;

                //Bench.editNameButton.Visibility = Visibility.Visible;
                //Bench.editNameButton.IsHitTestVisible = true;

                //MVVM: replaced with binding to backgroundToColorConverter
                Bench.setFunctionBackground();
            }

            return functionDefinition;
        }

        protected virtual dynFunction CreateFunction(IEnumerable<string> inputs, IEnumerable<string> outputs,
                                                     FunctionDefinition functionDefinition)
        {
            return new dynFunction(inputs, outputs, functionDefinition);
        }

        internal dynNode CreateNode(string name)
        {
            dynNode result;

            if (builtinTypesByTypeName.ContainsKey(name))
            {
                TypeLoadData tld = builtinTypesByTypeName[name];

                ObjectHandle obj = Activator.CreateInstanceFrom(tld.Assembly.Location, tld.Type.FullName);
                var newEl = (dynNode)obj.Unwrap();
                newEl.NodeUI.DisableInteraction();
                result = newEl;
            }
            else if (builtinTypesByNickname.ContainsKey(name))
            {
                TypeLoadData tld = builtinTypesByNickname[name];

                try
                {

                    ObjectHandle obj = Activator.CreateInstanceFrom(tld.Assembly.Location, tld.Type.FullName);
                    var newEl = (dynNode)obj.Unwrap();
                    newEl.NodeUI.DisableInteraction();
                    result = newEl;
                }
                catch (Exception ex)
                {
                    Log("Failed to load built-in type");
                    Log(ex);
                    result = null;
                }
            }
            else
            {
                FunctionDefinition def;
                dynSettings.FunctionDict.TryGetValue(Guid.Parse(name), out def);

                //dynFunction func;

                //if (CustomNodeLoader.GetNodeInstance(this, Guid.Parse(name), out func))
                //{
                //    result = func;
                //}
                //else
                //{
                //    Bench.Log("Failed to find FunctionDefinition.");
                //    return null;
                //}

                dynWorkspace ws = def.Workspace;

                //TODO: Update to base off of Definition
                IEnumerable<string> inputs =
                    ws.Nodes.Where(e => e is dynSymbol)
                      .Select(s => (s as dynSymbol).Symbol);

                IEnumerable<string> outputs =
                    ws.Nodes.Where(e => e is dynOutput)
                      .Select(o => (o as dynOutput).Symbol);

                if (!outputs.Any())
                {
                    var topMost = new List<Tuple<int, dynNode>>();

                    IEnumerable<dynNode> topMostNodes = ws.GetTopMostNodes();

                    foreach (dynNode topNode in topMostNodes)
                    {
                        foreach (int output in Enumerable.Range(0, topNode.OutPortData.Count))
                        {
                            if (!topNode.HasOutput(output))
                                topMost.Add(Tuple.Create(output, topNode));
                        }
                    }

                    outputs = topMost.Select(x => x.Item2.OutPortData[x.Item1].NickName);
                }

                result = new dynFunction(inputs, outputs, def);
                result.NodeUI.NickName = ws.Name;
            }

            //if (result is dynDouble)
            //    (result as dynDouble).Value = this.storedSearchNum;
            //else if (result is dynStringInput)
            //    (result as dynStringInput).Value = this.storedSearchStr;
            //else if (result is dynBool)
            //    (result as dynBool).Value = this.storedSearchBool;

            return result;
        }
    }


    //MVVM:Removed the splash screen commands
    //public class ShowSplashScreenCommand : ICommand
    //{
    //    public ShowSplashScreenCommand()
    //    {

    //    }

    //    public void Execute(object parameters)
    //    {
    //        if (dynSettings.Controller.SplashScreen == null)
    //        {
    //            dynSettings.Controller.SplashScreen = new Controls.DynamoSplash();
    //        }
    //        dynSettings.Controller.SplashScreen.Show();
    //    }

    //    public event EventHandler CanExecuteChanged
    //    {
    //        add { CommandManager.RequerySuggested += value; }
    //        remove { CommandManager.RequerySuggested -= value; }
    //    }

    //    public bool CanExecute(object parameters)
    //    {
    //        if (dynSettings.Controller != null)
    //        {
    //            return true;
    //        }

    //        return false;
    //    }
    //}

    //public class CloseSplashScreenCommand : ICommand
    //{
    //    public void Execute(object parameters)
    //    {
    //        dynSettings.Controller.SplashScreen.Close();
    //    }

    //    public event EventHandler CanExecuteChanged
    //    {
    //        add { CommandManager.RequerySuggested += value; }
    //        remove { CommandManager.RequerySuggested -= value; }
    //    }

    //    public bool CanExecute(object parameters)
    //    {
    //        if (dynSettings.Controller.SplashScreen != null)
    //        {
    //            return true;
    //        }

    //        return false;
    //    }
    //}
}