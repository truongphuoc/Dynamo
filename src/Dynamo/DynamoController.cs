using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Threading;

using Dynamo.Controls;
using Dynamo.FSchemeInterop;
using Dynamo.FSchemeInterop.Node;
using Dynamo.Nodes;
using Dynamo.TypeSystem;
using Dynamo.PackageManager;
using Dynamo.Search;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;

using NUnit.Core;
using NUnit.Framework;

namespace Dynamo
{

    /// <summary>
    /// Context values are required during controller instantiation to flag
    /// what application Dynamo is running within. Use NONE for the sandbox and
    /// other applications where context-sensitive loading are not required.
    /// </summary>
    public static partial class Context
    {
        public const string NONE = "None";
        public const string REVIT_2013 = "Autodesk Revit 2013";
        public const string REVIT_2014 = "Autodesk Revit 2014";
        public const string VASARI_2013 = "Autodesk Vasari 2013";
        public const string VASARI_2014 = "Autodesk Vasari 2014";
    }

    public class DynamoController
    {
        #region properties

        private readonly SortedDictionary<string, TypeLoadData> _builtinTypesByNickname =
            new SortedDictionary<string, TypeLoadData>();

        private readonly Dictionary<string, TypeLoadData> _builtinTypesByTypeName =
            new Dictionary<string, TypeLoadData>();

        private readonly Queue<Tuple<object, object>> _commandQueue = new Queue<Tuple<object, object>>();

        private bool isProcessingCommandQueue = false;
        private bool testing = false;

        public CustomNodeLoader CustomNodeLoader { get; internal set; }
        public SearchViewModel SearchViewModel { get; internal set; }
        public PackageManagerLoginViewModel PackageManagerLoginViewModel { get; internal set; }
        public PackageManagerPublishViewModel PackageManagerPublishViewModel { get; internal set; }
        public PackageManagerClient PackageManagerClient { get; internal set; }
        public DynamoViewModel DynamoViewModel { get; internal set; }
        public DynamoModel DynamoModel { get; set; }
        public Dispatcher UIDispatcher { get; set; }
        
        /// <summary>
        /// Testing flag is used to defer calls to run in the idle thread
        /// with the assumption that the entire test will be wrapped in an
        /// idle thread call.
        /// </summary>
        public bool Testing 
        {
            get { return testing; }
            set { testing = value; }
        }
        
        public List<dynModelBase> ClipBoard { get; set; }

        public bool IsProcessingCommandQueue { get; private set; }

        public Queue<Tuple<object, object>> CommandQueue
        {
            get { return _commandQueue; }
        }

        public SortedDictionary<string, TypeLoadData> BuiltInTypesByNickname
        {
            get { return _builtinTypesByNickname; }
        }

        public Dictionary<string, TypeLoadData> BuiltInTypesByName
        {
            get { return _builtinTypesByTypeName; }
        }

        public ExecutionEnvironment FSchemeEnvironment { get; private set; }

        public string Context { get; set; }

        #endregion

        #region events

        /// <summary>
        /// An event which requests that a node be selected
        /// </summary>
        public event NodeEventHandler RequestNodeSelect;
        public virtual void OnRequestSelect(object sender, ModelEventArgs e)
        {
            if (RequestNodeSelect != null)
                RequestNodeSelect(sender, e);
        }

        #endregion

        #region Constructor and Initialization

        /// <summary>
        ///     Class constructor
        /// </summary>
        public DynamoController(ExecutionEnvironment env, bool withUI, Type viewModelType, string context)
        {
            ClipBoard = new List<dynModelBase>();
            IsProcessingCommandQueue = false;
            dynSettings.Controller = this;

            Context = context;

            //MVVM: don't construct the main window with a reference to the controller
            //dynSettings.Bench = new dyndynSettings.Bench(this);

            //MVVM : create the view model to which the main window will bind
            //the DynamoModel is created therein
            //this.DynamoViewModel = new DynamoViewModel(this);
            DynamoViewModel = (DynamoViewModel)Activator.CreateInstance(viewModelType,new object[]{this});

            // custom node loader
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            if (directory != null) 
            {
                string pluginsPath = Path.Combine(directory, "definitions");

                CustomNodeLoader = new CustomNodeLoader(pluginsPath);
            }

            if (withUI)
            {
                dynSettings.Bench = new DynamoView();
                dynSettings.Bench = dynSettings.Bench;
                dynSettings.Bench.DataContext = DynamoViewModel;
                this.UIDispatcher = dynSettings.Bench.Dispatcher;
            }


            SearchViewModel = new SearchViewModel();
            PackageManagerClient = new PackageManagerClient(this);
            PackageManagerLoginViewModel = new PackageManagerLoginViewModel(PackageManagerClient);
            PackageManagerPublishViewModel = new PackageManagerPublishViewModel(PackageManagerClient);

            FSchemeEnvironment = env;

            DynamoViewModel.Model.CurrentSpace.X = DynamoView.CANVAS_OFFSET_X;
            DynamoViewModel.Model.CurrentSpace.Y = DynamoView.CANVAS_OFFSET_Y;

            dynSettings.Controller.DynamoViewModel.Log(String.Format(
                "Dynamo -- Build {0}",
                Assembly.GetExecutingAssembly().GetName().Version));

            DynamoLoader.LoadBuiltinTypes(SearchViewModel, this);

            if(dynSettings.Bench != null)
                DynamoLoader.LoadSamplesMenu(dynSettings.Bench);

            if (dynSettings.Bench != null && withUI)
            {
                //dynSettings.Bench.LockUI();

                //MVVM : callback has been restructured so that it sends a command back to the view model
                //dynSettings.Bench.Activated += OndynSettings.BenchActivated;

                //MVVM: we've gone to using a model and a model view of a workspace
                //do not reference a specific workdynSettings.Bench here.
                //dynSettings.WorkdynSettings.Bench = dynSettings.Bench.WorkdynSettings.Bench;
            }

            //run tests
            if (FScheme.RunTests(dynSettings.Controller.DynamoViewModel.Log))
            {
                if (dynSettings.Bench != null)
                    DynamoViewModel.Log("All Tests Passed. Core library loaded OK.");
            }
        }

        #endregion

        public void ShutDown()
        {
            dynSettings.Controller = null;
            Selection.DynamoSelection.Instance.ClearSelection();
        }

        #region CommandQueue


        /// <summary>
        /// Add a command to the CommandQueue and run ProcessCommandQueue(), providing null as the 
        /// command arguments
        /// </summary>
        /// <param name="command">The command to run</param>
        public void RunCommand(ICommand command)
        {
            RunCommand(command, null);
        }

        /// <summary>
        /// Add a command to the CommandQueue and run ProcessCommandQueue(), providing the given
        /// arguments to the command
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="args">Arguments to give to the command</param>
        public void RunCommand(ICommand command, object args)
        {
            var commandAndParams = Tuple.Create<object, object>(command, args);
            CommandQueue.Enqueue(commandAndParams);
            ProcessCommandQueue();
        }

        private void Hooks_DispatcherInactive(object sender, EventArgs e)
        {
            ProcessCommandQueue();
        }


        /// <summary>
        ///     Run all of the commands in the CommandQueue
        /// </summary>
        public void ProcessCommandQueue()
        {
            while (_commandQueue.Count > 0)
            {
                var cmdData = _commandQueue.Dequeue();
                var cmd = cmdData.Item1 as ICommand;
                if (cmd != null)
                {
                    if (cmd.CanExecute(cmdData.Item2))
                    {
                        cmd.Execute(cmdData.Item2);
                    }
                }
            }
            _commandQueue.Clear();

            if (dynSettings.Bench != null)
            {
                DynamoLogger.Instance.Log(string.Format("dynSettings.Bench Thread : {0}",
                                                       dynSettings.Bench.Dispatcher.Thread.ManagedThreadId.ToString()));
            }
        }

        #endregion

        #region Running

        //protected bool _debug;
        private bool _showErrors;

        private bool _runAgain;
        public bool Running { get; protected set; }

        public bool RunCancelled { get; protected internal set; }

        internal void QueueRun()
        {
            RunCancelled = true;
            _runAgain = true;
        }

        public void RunExpression(bool showErrors = true)
        {

            //If we're already running, do nothing.
            if (Running)
                return;

            _showErrors = showErrors;

            //TODO: Hack. Might cause things to break later on...
            //Reset Cancel and Rerun flags
            RunCancelled = false;
            _runAgain = false;

            //We are now considered running
            Running = true;

            if (!testing)
            {
                //Setup background worker
                var worker = new BackgroundWorker();
                worker.DoWork += EvaluationThread;

                DynamoViewModel.RunEnabled = false;

                //Let's start
                worker.RunWorkerAsync();
            }
            else
                //for testing, we do not want to run
                //asynchronously, as it will finish the 
                //test before the evaluation (and the run)
                //is complete
                EvaluationThread(null, null);
        }

        public delegate void RunCompletedHandler(object controller, bool success);
        public event RunCompletedHandler RunCompleted;
        public virtual void OnRunCompleted(object sender, bool success)
        {
            if (RunCompleted != null)
                RunCompleted(sender, success);
        }
        
        protected virtual void EvaluationThread(object s, DoWorkEventArgs args)
        {
            //Get our entry points (elements with nothing connected to output)
            List<dynNodeModel> topElements = DynamoViewModel.Model.HomeSpace.GetTopMostNodes().ToList();

            //Mark the topmost as dirty/clean
            foreach (dynNodeModel topMost in topElements)
                topMost.MarkDirty();

            //TODO: Flesh out error handling
            try
            {
                var typeDict = new Dictionary<dynNodeModel, NodeTypeInformation>();
                FSharpMap<string, TypeScheme> typeEnv = MapModule.Empty<string, TypeScheme>();

                foreach (dynNodeModel node in topElements)
                {
                    foreach (var j in Enumerable.Range(0, node.OutPortData.Count))
                        node.TypeCheck(j, typeEnv, TODO, typeDict);
                }

                var topNode = new BeginNode(new List<string>());
                int i = 0;
                var buildDict = new Dictionary<dynNodeModel, Dictionary<int, INode>>();
                foreach (dynNodeModel topMost in topElements)
                {
                    string inputName = i.ToString();
                    topNode.AddInput(inputName);
                    topNode.ConnectInput(inputName, topMost.BuildExpression(buildDict, typeDict));

                    i++;

                    DynamoLogger.Instance.Log(topMost);
                }

                FScheme.Expression runningExpression = topNode.Compile();

                Run(topElements, runningExpression);

                // inform any objects that a run has happened

                DynamoLogger.Instance.Log(runningExpression);
            }
            catch (CancelEvaluationException ex)
            {
                /* Evaluation was cancelled */

                OnRunCancelled(false);
                //this.CancelRun = false; //Reset cancel flag
                RunCancelled = true;

                //If we are forcing this, then make sure we don't run again either.
                if (ex.Force)
                    _runAgain = false;

                OnRunCompleted(this, false);
            }
            catch (Exception ex)
            {
                /* Evaluation has an error */

                //Catch unhandled exception
                if (ex.Message.Length > 0)
                {
                    dynSettings.Controller.DynamoViewModel.Log(ex);
                }

                OnRunCancelled(true);

                //Reset the flags
                _runAgain = false;
                RunCancelled = true;

                OnRunCompleted(this, false);

                if (Testing)
                    Assert.Fail(ex.Message + ":" + ex.StackTrace);
            }
            finally
            {
                /* Post-evaluation cleanup */

                DynamoViewModel.RunEnabled = true;

                //No longer running
                Running = false;

                foreach (FunctionDefinition def in dynSettings.FunctionWasEvaluated)
                    def.RequiresRecalc = false;

                
                //If we should run again...
                if (_runAgain)
                {
                    //Reset flag
                    _runAgain = false;

                    if (dynSettings.Bench != null)
                    {
                        //Run this method again from the main thread
                        dynSettings.Bench.Dispatcher.BeginInvoke(new Action(() => RunExpression(_showErrors)));
                    }
                }
                else
                {
                    OnRunCompleted(this, true);
                }
            }
        }

        protected internal virtual void Run(IEnumerable<dynNodeModel> topElements, FScheme.Expression runningExpression)
        {
            //Print some stuff if we're in debug mode
            if (DynamoViewModel.RunInDebug)
            {
                if (dynSettings.Bench != null)
                {
                    foreach (dynNodeModel node in topElements)
                    {
                        string exp = node.PrintExpression();
                        dynSettings.Controller.DynamoViewModel.Log("> " + exp);
                    }
                }
            }

            try
            {
                DynamoLogger.Instance.Log("Evaluating the expression...");

                //Evaluate the expression
                FScheme.Value expr = FSchemeEnvironment.Evaluate(runningExpression);

                if (dynSettings.Bench != null)
                {
                    //Print some more stuff if we're in debug mode
                    if (DynamoViewModel.RunInDebug && expr != null)
                    {
                        dynSettings.Controller.DynamoViewModel.Log(FScheme.print(expr));
                    }
                }
            }
            catch (CancelEvaluationException ex)
            {
                /* Evaluation was cancelled */

                OnRunCancelled(false);
                //this.RunCancelled = false;
                if (ex.Force)
                    _runAgain = false;
            }
            catch (Exception ex)
            {
                /* Evaluation failed due to error */

                if (dynSettings.Bench != null)
                {
                    //Print unhandled exception
                    if (ex.Message.Length > 0)
                    {
                        dynSettings.Bench.Dispatcher.Invoke(new Action(
                            () => dynSettings.Controller.DynamoViewModel.Log(ex)));
                    }
                }

                OnRunCancelled(true);
                RunCancelled = true;
                _runAgain = false;

                //If we are testing, we need to throw an exception here
                //which will, in turn, throw an Assert.Fail in the 
                //Evaluation thread.
                if (Testing)
                    throw new Exception(ex.Message);
            }

            OnEvaluationCompleted();
        }

        protected virtual void OnRunCancelled(bool error)
        {
            if (error)
                dynSettings.FunctionWasEvaluated.Clear();
        }

        protected virtual void OnEvaluationCompleted()
        {
        }

    #endregion

    }
}
