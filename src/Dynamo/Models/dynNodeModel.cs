using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Dynamo.Commands;
using Dynamo.Connectors;
using Dynamo.Controls;
using Dynamo.FSchemeInterop;
using Dynamo.FSchemeInterop.Node;
using Dynamo.Selection;
using Dynamo.TypeSystem;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo.Nodes
{
    public enum ElementState
    {
        Dead,
        Active,
        Error
    };

    public enum LacingStrategy
    {
        Disabled,
        First,
        Shortest,
        Longest,
        CrossProduct
    };

    public delegate void PortsChangedHandler(object sender, EventArgs e);

    public delegate void DispatchedToUIThreadHandler(object sender, UIDispatcherEventArgs e);

    public abstract class dynNodeModel : dynModelBase
    {
        #region Abstract Members

        /// <summary>
        /// The dynElement's Evaluation Logic.
        /// </summary>
        /// <param name="args">Arguments to the node. You are guaranteed to have as many arguments as you have InPorts at the time it is run.</param>
        /// <returns>An expression that is the result of the Node's evaluation. It will be passed along to whatever the OutPort is connected to.</returns>
        public virtual void Evaluate(FSharpList<FScheme.Value> args, Dictionary<PortData, FScheme.Value> outPuts)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Properties

        private readonly Dictionary<dynPortModel, PortData> _portDataDict =
            new Dictionary<dynPortModel, PortData>();

//MVVM : node should not reference its view directly
        //public dynNodeView NodeUI;

        private readonly Dictionary<int, Tuple<int, dynNodeModel>> _previousInputPortMappings =
            new Dictionary<int, Tuple<int, dynNodeModel>>();

        private readonly Dictionary<int, HashSet<Tuple<int, dynNodeModel>>>
            _previousOutputPortMappings =
                new Dictionary<int, HashSet<Tuple<int, dynNodeModel>>>();

        public Dictionary<int, Tuple<int, dynNodeModel>> Inputs =
            new Dictionary<int, Tuple<int, dynNodeModel>>();

        public Dictionary<int, HashSet<Tuple<int, dynNodeModel>>> Outputs =
            new Dictionary<int, HashSet<Tuple<int, dynNodeModel>>>();

        public dynWorkspaceModel WorkSpace;

        private LacingStrategy _argumentLacing = LacingStrategy.First;

        private ObservableCollection<dynPortModel> _inPorts =
            new ObservableCollection<dynPortModel>();

        private bool _interactionEnabled = true;
        private bool _isDirty = true;
        private string _nickName;

        private ObservableCollection<dynPortModel> _outPorts =
            new ObservableCollection<dynPortModel>();

        /// <summary>
        /// Should changes be reported to the containing workspace?
        /// </summary>
        private bool _report = true;

        private bool _saveResult;

        private ElementState _state;
        private string _toolTipText = "";
        public ObservableCollection<PortData> InPortData { get; private set; }
        public ObservableCollection<PortData> OutPortData { get; private set; }
        //bool isSelected = false;

        /// <summary>
        /// Returns whether this node represents a built-in or custom function.
        /// </summary>
        public bool IsCustomFunction
        {
            get { return GetType().IsAssignableFrom(typeof(dynFunction)); }
        }

        public ElementState State
        {
            get { return _state; }
            set
            {
                if (value != ElementState.Error)
                    SetTooltip();

                _state = value;
                RaisePropertyChanged("State");
            }
        }

        public string ToolTipText
        {
            get { return _toolTipText; }
            set
            {
                _toolTipText = value;
                RaisePropertyChanged("ToolTipText");
            }
        }

        public string NickName
        {
            get { return _nickName; }
            set
            {
                _nickName = value;
                RaisePropertyChanged("NickName");
            }
        }

        public ObservableCollection<dynPortModel> InPorts
        {
            get { return _inPorts; }
            set
            {
                _inPorts = value;
                RaisePropertyChanged("InPorts");
            }
        }

        public ObservableCollection<dynPortModel> OutPorts
        {
            get { return _outPorts; }
            set
            {
                _outPorts = value;
                RaisePropertyChanged("OutPorts");
            }
        }

        /// <summary>
        /// Control how arguments lists of various sizes are laced.
        /// </summary>
        public LacingStrategy ArgumentLacing
        {
            get { return _argumentLacing; }
            set
            {
                _argumentLacing = value;
                isDirty = true;
                RaisePropertyChanged("ArgumentLacing");
            }
        }

        /// <summary>
        ///     Category property
        /// </summary>
        /// <value>
        ///     If the node has a category, return it.  Other wise return empty string.
        /// </value>
        public string Category
        {
            get
            {
                Type type = GetType();
                object[] attribs = type.GetCustomAttributes(typeof(NodeCategoryAttribute), false);
                if (type.Namespace == "Dynamo.Nodes" &&
                    !type.IsAbstract &&
                    attribs.Length > 0 &&
                    type.IsSubclassOf(typeof(dynNodeModel)))
                {
                    var elCatAttrib = attribs[0] as NodeCategoryAttribute;
                    return elCatAttrib.ElementCategory;
                }
                return "";
            }
        }

        /// <summary>
        /// Get the last computed value from the node.
        /// </summary>
        public FScheme.Value OldValue { get; protected set; }

        //TODO: don't make this static (maybe)
        protected DynamoView Bench
        {
            get { return dynSettings.Bench; }
        }

        protected DynamoController Controller
        {
            get { return dynSettings.Controller; }
        }

        ///<summary>
        ///Does this Element need to be regenerated? Setting this to true will trigger a modification event
        ///for the dynWorkspace containing it. If Automatic Running is enabled, setting this to true will
        ///trigger an evaluation.
        ///</summary>
        public virtual bool RequiresRecalc
        {
            get
            {
                //TODO: When marked as clean, remember so we don't have to re-traverse
                return _isDirty
                       || (_isDirty =
                           Inputs.Values.Where(x => x != null).Any(x => x.Item2.RequiresRecalc));
            }
            set
            {
                _isDirty = value;
                if (value && _report && WorkSpace != null)
                    WorkSpace.Modified();
            }
        }

        /// <summary>
        /// Returns if this node requires a recalculation without checking input nodes.
        /// </summary>
        protected internal bool isDirty
        {
            get { return _isDirty; }
            set { RequiresRecalc = value; }
        }

        /// <summary>
        /// Determines whether or not the output of this Element will be saved. If true, Evaluate() will not be called
        /// unless IsDirty is true. Otherwise, Evaluate will be called regardless of the IsDirty value.
        /// </summary>
        internal bool SaveResult
        {
            get
            {
                return _saveResult
                       && Enumerable.Range(0, InPortData.Count).All(HasInput);
            }
            set { _saveResult = value; }
        }

        /// <summary>
        /// Is this node an entry point to the program?
        /// </summary>
        public bool IsTopmost
        {
            get
            {
                return OutPorts == null
                       || OutPorts.All(x => !x.Connectors.Any());
            }
        }

        public List<string> Tags
        {
            get
            {
                Type t = GetType();
                object[] rtAttribs = t.GetCustomAttributes(typeof(NodeSearchTagsAttribute), true);

                return rtAttribs.Length > 0
                           ? ((NodeSearchTagsAttribute)rtAttribs[0]).Tags
                           : new List<string>();
            }
        }

        public string Description
        {
            get
            {
                Type t = GetType();
                object[] rtAttribs = t.GetCustomAttributes(typeof(NodeDescriptionAttribute), true);
                return ((NodeDescriptionAttribute)rtAttribs[0]).ElementDescription;
            }
        }

        public bool InteractionEnabled
        {
            get { return _interactionEnabled; }
            set
            {
                _interactionEnabled = value;
                RaisePropertyChanged("InteractionEnabled");
            }
        }

        public event DispatchedToUIThreadHandler DispatchedToUI;

        public void OnDispatchedToUI(object sender, UIDispatcherEventArgs e)
        {
            if (DispatchedToUI != null)
                DispatchedToUI(this, e);
        }

        #endregion

        /* TODO:
         * Incorporate INode in here somewhere
         */

        private readonly Dictionary<PortData, FScheme.Value> _evaluationDict =
            new Dictionary<PortData, FScheme.Value>();

        public dynNodeModel()
        {
            InPortData = new ObservableCollection<PortData>();
            OutPortData = new ObservableCollection<PortData>();

            //Fetch the element name from the custom attribute.
            object[] nameArray = GetType().GetCustomAttributes(typeof(NodeNameAttribute), true);

            if (nameArray.Length > 0)
            {
                var elNameAttrib = nameArray[0] as NodeNameAttribute;
                if (elNameAttrib != null)
                    NickName = elNameAttrib.Name;
            }
            else
                NickName = "";

            IsSelected = false;
            State = ElementState.Dead;
            ArgumentLacing = LacingStrategy.First;
        }

        protected internal bool ReportingEnabled
        {
            get { return _report; }
        }

        /// <summary>
        /// Check current ports against ports used for previous mappings.
        /// </summary>
        private void CheckPortsForRecalc()
        {
            RequiresRecalc = 
                Enumerable.Range(0, InPortData.Count).Any(
                    delegate(int input)
                    {
                        Tuple<int, dynNodeModel> oldInput;
                        Tuple<int, dynNodeModel> currentInput;

                        //this is dirty if there wasn't anything set last time (implying it was never run)...
                        return !_previousInputPortMappings.TryGetValue(input, out oldInput)
                               || oldInput == null
                               || !TryGetInput(input, out currentInput)
                               //or If what's set doesn't match
                               || (oldInput.Item2 != currentInput.Item2
                                   && oldInput.Item1 != currentInput.Item1);
                    })
                || Enumerable.Range(0, OutPortData.Count).Any(
                    delegate(int output)
                    {
                        HashSet<Tuple<int, dynNodeModel>> oldOutputs;
                        HashSet<Tuple<int, dynNodeModel>> newOutputs;

                        return
                            !_previousOutputPortMappings.TryGetValue(
                                output, out oldOutputs)
                            || !TryGetOutput(output, out newOutputs)
                            || oldOutputs.SetEquals(newOutputs);
                    });
        }

        /// <summary>
        /// Override this to implement custom save data for your Element. If overridden, you should also override
        /// LoadElement() in order to read the data back when loaded.
        /// </summary>
        /// <param name="xmlDoc">The XmlDocument representing the whole workspace containing this Element.</param>
        /// <param name="dynEl">The XmlElement representing this Element.</param>
        public virtual void SaveElement(XmlDocument xmlDoc, XmlElement dynEl) { }

        /// <summary>
        /// Override this to implement loading of custom data for your Element. If overridden, you should also override
        /// SaveElement() in order to write the data when saved.
        /// </summary>
        /// <param name="elNode">The XmlNode representing this Element.</param>
        public virtual void LoadElement(XmlNode elNode) { }

        /// <summary>
        /// Forces the node to refresh it's dirty state by checking all inputs.
        /// </summary>
        public void MarkDirty()
        {
            bool dirty = false;
            foreach (var input in Inputs.Values.Where(x => x != null))
            {
                input.Item2.MarkDirty();
                if (input.Item2.RequiresRecalc)
                    dirty = true;
            }
            if (!_isDirty)
                _isDirty = dirty;
        }

        protected virtual IDynamoType GetInputType(int port)
        {
            return InPortData[port].PortType;
        }

        protected virtual IDynamoType GetOutputType(int port)
        {
            return OutPortData[port].PortType;
        }

        internal virtual IDynamoType TypeCheck(
            int port, FSharpMap<dynSymbol, TypeScheme> env,
            Dictionary<dynNodeModel, Tuple<List<IDynamoType>, List<IDynamoType>>> typeDict)
        {
            if (Enumerable.Range(0, InPortData.Count).All(HasInput))
            {
                IDynamoType t1 = TypeScheme.Generalize(
                    env,
                    new FunctionType(
                        Enumerable.Range(0, InPortData.Count).Select(GetInputType),
                        GetOutputType(port)))
                                           .Instantiate();

                var t = new GuessType();

                bool success = t1.Unify(
                    new FunctionType(
                        Enumerable.Range(0, InPortData.Count).Select(
                            x =>
                            {
                                Tuple<int, dynNodeModel> input = Inputs[x];
                                return input.Item2.TypeCheck(input.Item1, env, typeDict);
                            }),
                        t));
                if (success)
                {
                    IDynamoType result = t.Unwrap();
                    if (typeDict.ContainsKey(this))
                    {
                        Tuple<List<IDynamoType>, List<IDynamoType>> thisType = typeDict[this];
                        thisType.Item2[port] = result;
                    }
                    else
                    {
                        var thisType = (FunctionType)t1.Unwrap();
                        List<IDynamoType> outTypes =
                            Enumerable.Repeat<IDynamoType>(null, OutPortData.Count).ToList();
                        outTypes[port] = result;
                        typeDict[this] = Tuple.Create(
                            thisType.Inputs.Select(x => x.Unwrap()).ToList(),
                            outTypes);
                    }
                    return result;
                }
                Error("Type check failed.");
                throw new Exception("Type check failed.");
            }

            var inputs = new List<IDynamoType>();

            foreach (int inDataIdx in InPortData.Select((_, i) => i))
            {
                Tuple<int, dynNodeModel> input;
                if (TryGetInput(inDataIdx, out input))
                {
                    if (
                        input.Item2.TypeCheck(input.Item1, env, typeDict)
                             .Unify(GetInputType(inDataIdx)))
                        continue;
                    throw new Exception("Type check failed.");
                }
                inputs.Add(GetInputType(inDataIdx));
            }

            return new FunctionType(inputs, OutPortData[port].PortType);
        }

        internal virtual INode BuildExpression(
            Dictionary<dynNodeModel, Dictionary<int, INode>> buildDict,
            Dictionary<dynNodeModel, Tuple<List<IDynamoType>, List<IDynamoType>>> typeDict)
        {
            //Debug.WriteLine("Building expression...");

            if (OutPortData.Count > 1)
            {
                List<string> names = OutPortData.Select(x => x.NickName)
                                                .Zip(
                                                    Enumerable.Range(0, OutPortData.Count),
                                                    (x, i) => x + i)
                                                .ToList();
                var listNode = new FunctionNode("list", names);
                foreach (
                    var data in
                        names.Zip(
                            Enumerable.Range(0, OutPortData.Count),
                            (name, index) => new { Name = name, Index = index }))
                    listNode.ConnectInput(data.Name, Build(buildDict, data.Index, typeDict));
                return listNode;
            }
            return Build(buildDict, 0, typeDict);
        }

        //TODO: do all of this as the Ui is modified, simply return this?
        /// <summary>
        /// Builds an INode out of this Element. Override this or Compile() if you want complete control over this Element's
        /// execution.
        /// </summary>
        /// <returns>The INode representation of this Element.</returns>
        protected internal virtual INode Build(
            Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort,
            Dictionary<dynNodeModel, Tuple<List<IDynamoType>, List<IDynamoType>>> typeDict)
        {
            //Debug.WriteLine("Building node...");

            Dictionary<int, INode> result;
            if (preBuilt.TryGetValue(this, out result))
                return result[outPort];

            //Fetch the names of input ports.
            List<string> portNames =
                InPortData.Zip(
                    Enumerable.Range(0, InPortData.Count),
                    (x, i) => x.NickName + i)
                          .ToList();

            //Compile the procedure for this node.
            InputNode node = Compile(portNames);

            //Is this a partial application?
            bool partial = false;

            var partialSymList = new List<string>();

            //For each index in InPortData
            //for (int i = 0; i < InPortData.Count; i++)
            foreach (
                var data in
                    Enumerable.Range(0, InPortData.Count)
                              .Zip(portNames, (data, name) => new { Index = data, Name = name }))
            {
                //Fetch the corresponding port
                //var port = InPorts[i];

                Tuple<int, dynNodeModel> input;

                //If this port has connectors...
                //if (port.Connectors.Any())
                if (TryGetInput(data.Index, out input))
                {
                    //Debug.WriteLine(string.Format("Connecting input {0}", data.Name));

                    //Compile input and connect it
                    node.ConnectInput(data.Name, input.Item2.Build(preBuilt, input.Item1, typeDict));
                }
                else //othwise, remember that this is a partial application
                {
                    partial = true;
                    partialSymList.Add(data.Name);
                }
            }

            var nodes = new Dictionary<int, INode>();

            if (OutPortData.Count > 1)
            {
                foreach (string data in partialSymList)
                    node.ConnectInput(data, new SymbolNode(data));

                InputNode prev = node;
                int prevIndex = 0;

                var query =
                    Enumerable.Range(0, OutPortData.Count)
                              .Zip(OutPortData, (i, d) => new { Index = i, Data = d })
                              .Where(data => HasOutput(data.Index));
                foreach (var data in query)
                {
                    if (data.Index > 0)
                    {
                        int diff = data.Index - prevIndex;
                        InputNode restNode;
                        if (diff > 1)
                        {
                            restNode = new ExternalFunctionNode(
                                FScheme.Drop, new List<string> { "amt", "list" });
                            restNode.ConnectInput("amt", new NumberNode(diff));
                            restNode.ConnectInput("list", prev);
                        }
                        else
                        {
                            restNode = new ExternalFunctionNode(
                                FScheme.Cdr, new List<string> { "list" });
                            restNode.ConnectInput("list", prev);
                        }
                        prev = restNode;
                        prevIndex = data.Index;
                    }

                    var firstNode = new ExternalFunctionNode(
                        FScheme.Car, new List<string> { "list" });
                    firstNode.ConnectInput("list", prev);

                    if (partial)
                        nodes[data.Index] = new AnonymousFunctionNode(partialSymList, firstNode);
                    else
                        nodes[data.Index] = firstNode;
                }
            }
            else
                nodes[outPort] = node;

            //If this is a partial application, then remember not to re-eval.
            if (partial)
                RequiresRecalc = false;

            preBuilt[this] = nodes;

            //And we're done
            return nodes[outPort];
        }

        /// <summary>
        /// Compiles this Element into a ProcedureCallNode. Override this instead of Build() if you don't want to set up all
        /// of the inputs for the ProcedureCallNode.
        /// </summary>
        /// <param name="portNames">The names of the inputs to the node.</param>
        /// <returns>A ProcedureCallNode which will then be processed recursively to be connected to its inputs.</returns>
        protected virtual InputNode Compile(IEnumerable<string> portNames)
        {
            //Debug.WriteLine(string.Format("Compiling InputNode with ports {0}.", string.Join(",", portNames)));

            //Return a Function that calls eval.
            return new ExternalFunctionNode(evalIfDirty, portNames);
        }

        /// <summary>
        /// Called right before Evaluate() is called. Useful for processing side-effects without touching Evaluate()
        /// </summary>
        protected virtual void OnEvaluate() { }

        /// <summary>
        /// Called when the node's workspace has been saved.
        /// </summary>
        protected internal virtual void OnSave() { }

        internal void onSave()
        {
            savePortMappings();
            OnSave();
        }

        private void savePortMappings()
        {
            //Save all of the connection states, so we can check if this is dirty
            foreach (int data in Enumerable.Range(0, InPortData.Count))
            {
                Tuple<int, dynNodeModel> input;

                _previousInputPortMappings[data] = TryGetInput(data, out input)
                                                       ? input
                                                       : null;
            }

            foreach (int data in Enumerable.Range(0, OutPortData.Count))
            {
                HashSet<Tuple<int, dynNodeModel>> outputs;

                _previousOutputPortMappings[data] = TryGetOutput(data, out outputs)
                                                        ? outputs
                                                        : new HashSet<Tuple<int, dynNodeModel>>();
            }
        }


        private FScheme.Value evalIfDirty(FSharpList<FScheme.Value> args)
        {
            if (OldValue == null || !SaveResult || RequiresRecalc)
            {
                //Evaluate arguments, then evaluate 
                OldValue = evaluateNode(args);
            }
            else
                OnEvaluate();

            return OldValue;
        }

        public FScheme.Value GetValue(int outPortIndex)
        {
            return _evaluationDict.Values.ElementAt(outPortIndex);
        }

        protected internal virtual FScheme.Value evaluateNode(FSharpList<FScheme.Value> args)
        {
            //Debug.WriteLine("Evaluating node...");

            if (SaveResult)
                savePortMappings();

            _evaluationDict.Clear();

            object[] iaAttribs = GetType()
                .GetCustomAttributes(typeof(IsInteractiveAttribute), false);
            bool isInteractive = iaAttribs.Length > 0
                                 && ((IsInteractiveAttribute)iaAttribs[0]).IsInteractive;

            InnerEvaluationDelegate evaluation = delegate
            {
                FScheme.Value expr = null;

                try
                {
                    if (Controller.RunCancelled)
                        throw new CancelEvaluationException(false);

                    __eval_internal(args, _evaluationDict);

                    expr = OutPortData.Count == 1
                               ? _evaluationDict[OutPortData[0]]
                               : FScheme.Value.NewList(
                                   Utils.SequenceToFSharpList(
                                       _evaluationDict.OrderBy(
                                           pair => OutPortData.IndexOf(pair.Key))
                                                      .Select(
                                                          pair => pair.Value)));

                    ValidateConnections();
                }
                catch (CancelEvaluationException)
                {
                    OnRunCancelled();
                    return FSharpOption<FScheme.Value>.None;
                }
                catch (Exception ex)
                {
                    Bench.Dispatcher.Invoke(
                        new Action(
                            delegate
                            {
                                Debug.WriteLine(ex.Message + " : " + ex.StackTrace);
                                dynSettings.Controller.DynamoViewModel.Log(ex);

                                if (DynamoCommands.WriteToLogCmd.CanExecute(null))
                                {
                                    DynamoCommands.WriteToLogCmd.Execute(ex.Message);
                                    DynamoCommands.WriteToLogCmd.Execute(ex.StackTrace);
                                }

                                Controller.DynamoViewModel.ShowElement(this);
                            }
                            ));

                    Error(ex.Message);
                }

                OnEvaluate();

                RequiresRecalc = false;

                return FSharpOption<FScheme.Value>.Some(expr);
            };

//MVVM : Switched from nodeUI dispatcher to bench dispatcher 
            //C# doesn't have a Option type, so we'll just borrow F#'s instead.
            FSharpOption<FScheme.Value> result = isInteractive && dynSettings.Bench != null
                                                     ? (FSharpOption<FScheme.Value>)
                                                       dynSettings.Bench.Dispatcher.Invoke(
                                                           evaluation)
                                                     : evaluation();

            if (Equals(result, FSharpOption<FScheme.Value>.None))
                throw new CancelEvaluationException(false);
            if (result.Value != null)
                return result.Value;
            throw new Exception("");
        }

        protected virtual void OnRunCancelled() { }

        protected internal virtual void __eval_internal(
            FSharpList<FScheme.Value> args,
            Dictionary<PortData, FScheme.Value> outPuts)
        {
            List<string> argList = args.Select(x => x.ToString()).ToList();
            List<string> outPutsList = outPuts.Keys.Select(x => x.NickName).ToList();

            Debug.WriteLine(
                string.Format(
                    "__eval_internal : {0} : {1}",
                    string.Join(",", argList),
                    string.Join(",", outPutsList)));

            Evaluate(args, outPuts);
        }

        /// <summary>
        /// Destroy this dynElement
        /// </summary>
        public virtual void Destroy() { }

        protected internal void DisableReporting()
        {
            _report = false;
        }

        protected internal void EnableReporting()
        {
            _report = true;
        }

        /// <summary>
        /// Creates a Scheme representation of this dynNode and all connected dynNodes.
        /// </summary>
        /// <returns>S-Expression</returns>
        public virtual string PrintExpression()
        {
            string nick = NickName.Replace(' ', '_');

            if (!Enumerable.Range(0, InPortData.Count).Any(HasInput))
                return nick;

            string s = "";

            if (Enumerable.Range(0, InPortData.Count).All(HasInput))
            {
                s += "(" + nick;
                //for (int i = 0; i < InPortData.Count; i++)
                foreach (int data in Enumerable.Range(0, InPortData.Count))
                {
                    Tuple<int, dynNodeModel> input;
                    TryGetInput(data, out input);
                    s += " " + input.Item2.PrintExpression();
                }
                s += ")";
            }
            else
            {
                s += "(lambda ("
                     + string.Join(
                         " ", InPortData.Where((_, i) => !HasInput(i)).Select(x => x.NickName))
                     + ") (" + nick;
                //for (int i = 0; i < InPortData.Count; i++)
                foreach (int data in Enumerable.Range(0, InPortData.Count))
                {
                    s += " ";
                    Tuple<int, dynNodeModel> input;
                    if (TryGetInput(data, out input))
                        s += input.Item2.PrintExpression();
                    else
                        s += InPortData[data].NickName;
                }
                s += "))";
            }

            return s;
        }

        internal void ConnectInput(int inputData, int outputData, dynNodeModel node)
        {
            Inputs[inputData] = Tuple.Create(outputData, node);
            CheckPortsForRecalc();

            InPortConnected(InPortData[inputData], node.OutPortData[outputData]);
        }

        protected virtual void InPortConnected(PortData inPort, PortData outPortSender) { }

        internal void ConnectOutput(int portData, int inputData, dynNodeModel nodeLogic)
        {
            if (!Outputs.ContainsKey(portData))
                Outputs[portData] = new HashSet<Tuple<int, dynNodeModel>>();
            Outputs[portData].Add(Tuple.Create(inputData, nodeLogic));
        }

        internal void DisconnectInput(int data)
        {
            Inputs[data] = null;
            CheckPortsForRecalc();
        }

        /// <summary>
        /// Attempts to get the input for a certain port.
        /// </summary>
        /// <param name="data">PortData to look for an input for.</param>
        /// <param name="input">If an input is found, it will be assigned.</param>
        /// <returns>True if there is an input, false otherwise.</returns>
        public bool TryGetInput(int data, out Tuple<int, dynNodeModel> input)
        {
            return Inputs.TryGetValue(data, out input) && input != null;
        }

        public bool TryGetOutput(int output, out HashSet<Tuple<int, dynNodeModel>> newOutputs)
        {
            return Outputs.TryGetValue(output, out newOutputs);
        }

        /// <summary>
        /// Checks if there is an input for a certain port.
        /// </summary>
        /// <param name="data">PortData to look for an input for.</param>
        /// <returns>True if there is an input, false otherwise.</returns>
        public bool HasInput(int data)
        {
            return Inputs.ContainsKey(data) && Inputs[data] != null;
        }

        public bool HasOutput(int portData)
        {
            return Outputs.ContainsKey(portData) && Outputs[portData].Any();
        }

        internal void DisconnectOutput(int portData, int inPortData)
        {
            HashSet<Tuple<int, dynNodeModel>> output;
            if (Outputs.TryGetValue(portData, out output))
                output.RemoveWhere(x => x.Item1 == inPortData);
            CheckPortsForRecalc();
        }

        /// <summary>
        /// Implement on derived classes to cleanup resources when 
        /// </summary>
        public virtual void Cleanup() { }

        public void RegisterAllPorts()
        {
            RegisterInputs();
            RegisterOutputs();
            ValidateConnections();
        }

        /// <summary>
        /// Add a port to this node. If the port already exists, return that port.
        /// </summary>
        /// <param name="portType"></param>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public dynPortModel AddPort(PortType portType, string name, int index)
        {
            if (portType == PortType.INPUT)
            {
                if (_inPorts.Count > index)
                {
                    dynPortModel p = _inPorts[index];

                    //update the name on the node
                    //e.x. when the node is being re-registered during a custom
                    //node save
                    p.PortName = name;

                    return p;
                }
                else
                {
                    var p = new dynPortModel(index, portType, this, name);

                    InPorts.Add(p);

                    //register listeners on the port
                    p.PortConnected += p_PortConnected;
                    p.PortDisconnected += p_PortDisconnected;

                    return p;
                }
            }
            if (portType == PortType.OUTPUT)
            {
                if (_outPorts.Count > index)
                    return _outPorts[index];

                var p = new dynPortModel(index, portType, this, name);

                OutPorts.Add(p);

                //register listeners on the port
                p.PortConnected += p_PortConnected;
                p.PortDisconnected += p_PortDisconnected;

                return p;
            }
            return null;
        }

        //TODO: call connect and disconnect for dynNode

        /// <summary>
        /// When a port is connected, register a listener for the dynElementUpdated event
        /// and tell the object to build
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_PortConnected(object sender, EventArgs e)
        {
            ValidateConnections();

            var port = (dynPortModel)sender;
            if (port.PortType == PortType.INPUT)
            {
                int data = InPorts.IndexOf(port);
                dynPortModel startPort = port.Connectors[0].Start;
                int outData = startPort.Owner.OutPorts.IndexOf(startPort);
                ConnectInput(
                    data,
                    outData,
                    startPort.Owner);
                startPort.Owner.ConnectOutput(
                    outData,
                    data,
                    this
                    );
            }
        }

        private void p_PortDisconnected(object sender, EventArgs e)
        {
            ValidateConnections();

            var port = (dynPortModel)sender;
            if (port.PortType == PortType.INPUT)
            {
                int data = InPorts.IndexOf(port);
                dynPortModel startPort = port.Connectors[0].Start;
                DisconnectInput(data);
                startPort.Owner.DisconnectOutput(
                    startPort.Owner.OutPorts.IndexOf(startPort),
                    data);
            }
        }

        private void RemovePort(dynPortModel inport)
        {
            while (inport.Connectors.Any())
            {
                dynConnectorModel connector = inport.Connectors[0];
                dynSettings.Controller.DynamoModel.CurrentSpace.Connectors.Remove(connector);
                connector.NotifyConnectedPortsOfDeletion();
            }
        }

        /// <summary>
        /// Reads inputs list and adds ports for each input.
        /// </summary>
        public void RegisterInputs()
        {
            //read the inputs list and create a number of
            //input ports
            int count = 0;
            foreach (PortData pd in InPortData)
            {
                //add a port for each input
                //distribute the ports along the 
                //edges of the icon
                dynPortModel port = AddPort(PortType.INPUT, InPortData[count].NickName, count);

                //MVVM: AddPort now returns a port model. You can't set the data context here.
                //port.DataContext = this;

                _portDataDict[port] = pd;
                count++;
            }

            if (_inPorts.Count > count)
            {
                foreach (dynPortModel inport in _inPorts.Skip(count))
                    RemovePort(inport);

                for (int i = _inPorts.Count - 1; i >= count; i--)
                    _inPorts.RemoveAt(i);
                //InPorts.RemoveRange(count, inPorts.Count - count);
            }
        }

        /// <summary>
        /// Reads outputs list and adds ports for each output
        /// </summary>
        public void RegisterOutputs()
        {
            //read the inputs list and create a number of
            //input ports
            int count = 0;
            foreach (PortData pd in OutPortData)
            {
                //add a port for each input
                //distribute the ports along the 
                //edges of the icon
                dynPortModel port = AddPort(PortType.OUTPUT, pd.NickName, count);

//MVVM : don't set the data context in the model
                //port.DataContext = this;

                _portDataDict[port] = pd;
                count++;
            }

            if (_outPorts.Count > count)
            {
                foreach (dynPortModel outport in _outPorts.Skip(count))
                    RemovePort(outport);

                for (int i = _outPorts.Count - 1; i >= count; i--)
                    _outPorts.RemoveAt(i);

                //OutPorts.RemoveRange(count, outPorts.Count - count);
            }
        }

        private void SetTooltip()
        {
            Type t = GetType();
            object[] rtAttribs = t.GetCustomAttributes(typeof(NodeDescriptionAttribute), true);
            if (rtAttribs.Length > 0)
            {
                string description = ((NodeDescriptionAttribute)rtAttribs[0]).ElementDescription;
                ToolTipText = description;
            }
        }

        public IEnumerable<dynConnectorModel> AllConnectors()
        {
            return _inPorts.Concat(_outPorts).SelectMany(port => port.Connectors);
        }

        /// <summary>
        /// Color the connection according to it's port connectivity
        /// if all ports are connected, color green, else color orange
        /// </summary>
        public void ValidateConnections()
        {
            // if there are inputs without connections
            // mark as dead
            State = _inPorts.Select(x => x).Any(x => x.Connectors.Count == 0)
                        ? ElementState.Dead
                        : ElementState.Active;
        }

        public void Error(string p)
        {
            State = ElementState.Error;
            ToolTipText = p;
        }

        public void SelectNeighbors()
        {
            IEnumerable<dynConnectorModel> outConnectors = _outPorts.SelectMany(x => x.Connectors);
            IEnumerable<dynConnectorModel> inConnectors = _inPorts.SelectMany(x => x.Connectors);

            foreach (
                dynConnectorModel c in
                    outConnectors.Where(
                        c => !DynamoSelection.Instance.Selection.Contains(c.End.Owner)))
                DynamoSelection.Instance.Selection.Add(c.End.Owner);

            foreach (
                dynConnectorModel c in
                    inConnectors.Where(
                        c => !DynamoSelection.Instance.Selection.Contains(c.Start.Owner)))
                DynamoSelection.Instance.Selection.Add(c.Start.Owner);
        }

        //private Dictionary<UIElement, bool> enabledDict
        //    = new Dictionary<UIElement, bool>();

        internal void DisableInteraction()
        {
            State = ElementState.Dead;
            InteractionEnabled = false;
        }

        internal void EnableInteraction()
        {
            ValidateConnections();
            InteractionEnabled = true;
        }

        /// <summary>
        /// Called back from the view to enable users to setup their own view elements
        /// </summary>
        /// <param name="nodeUI"></param>
        public virtual void SetupCustomUIElements(dynNodeView nodeUI) { }

        /// <summary>
        /// Called by nodes for behavior that they want to dispatch on the UI thread
        /// Triggers event to be received by the UI. If no UI exists, behavior will not be executed.
        /// </summary>
        /// <param name="a"></param>
        public void DispatchOnUIThread(Action a)
        {
            OnDispatchedToUI(this, new UIDispatcherEventArgs(a));
        }

        #region ISelectable Interface

        public override void Deselect()
        {
            ValidateConnections();
            IsSelected = false;
        }

        #endregion

        /// <summary>
        /// Wraps node evaluation logic so that it can be called in different threads.
        /// </summary>
        /// <returns>Some(Value) -> Result | None -> Run was cancelled</returns>
        private delegate FSharpOption<FScheme.Value> InnerEvaluationDelegate();
    }

    public abstract class dynNodeWithOneOutput : dynNodeModel
    {
        public override void Evaluate(
            FSharpList<FScheme.Value> args, Dictionary<PortData, FScheme.Value> outPuts)
        {
            outPuts[OutPortData[0]] = Evaluate(args);
        }

        public virtual FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            throw new NotImplementedException();
        }
    }

    #region class attributes

    [AttributeUsage(AttributeTargets.All)]
    public class NodeNameAttribute : Attribute
    {
        public NodeNameAttribute(string elementName)
        {
            Name = elementName;
        }

        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class NodeCategoryAttribute : Attribute
    {
        public NodeCategoryAttribute(string category)
        {
            ElementCategory = category;
        }

        public string ElementCategory { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class NodeSearchTagsAttribute : Attribute
    {
        public NodeSearchTagsAttribute(params string[] tags)
        {
            Tags = tags.ToList();
        }

        public List<string> Tags { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class IsInteractiveAttribute : Attribute
    {
        public IsInteractiveAttribute(bool isInteractive)
        {
            IsInteractive = isInteractive;
        }

        public bool IsInteractive { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class NodeDescriptionAttribute : Attribute
    {
        public NodeDescriptionAttribute(string description)
        {
            ElementDescription = description;
        }

        public string ElementDescription { get; set; }
    }

    /// <summary>
    /// The DoNotLoadOnPlatforms attribute allows the node implementor
    /// to define an array of contexts in which the node will not
    /// be loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class DoNotLoadOnPlatformsAttribute : Attribute
    {
        public DoNotLoadOnPlatformsAttribute(params string[] values)
        {
            Values = values;
        }

        public string[] Values { get; set; }
    }

    /// <summary>
    /// The AlsoKnownAs attribute allows the node implementor to
    /// define an array of names that this node might have had
    /// in the past.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class AlsoKnownAsAttribute : Attribute
    {
        public AlsoKnownAsAttribute(params string[] values)
        {
            Values = values;
        }

        public string[] Values { get; set; }
    }

    #endregion

    public class PredicateTraverser
    {
        private readonly Predicate<dynNodeModel> _predicate;

        private readonly Dictionary<dynNodeModel, bool> _resultDict =
            new Dictionary<dynNodeModel, bool>();

        private bool _inProgress;

        public PredicateTraverser(Predicate<dynNodeModel> p)
        {
            _predicate = p;
        }

        public bool TraverseUntilAny(dynNodeModel entry)
        {
            _inProgress = true;
            bool result = traverseAny(entry);
            _resultDict.Clear();
            _inProgress = false;
            return result;
        }

        public bool ContinueTraversalUntilAny(dynNodeModel entry)
        {
            if (_inProgress)
                return traverseAny(entry);
            throw new Exception(
                "ContinueTraversalUntilAny cannot be used except in a traversal predicate.");
        }

        private bool traverseAny(dynNodeModel entry)
        {
            bool result;
            if (_resultDict.TryGetValue(entry, out result))
                return result;

            result = _predicate(entry);
            _resultDict[entry] = result;
            if (result)
                return true;

            if (entry is dynFunction)
            {
                Guid symbol = Guid.Parse((entry as dynFunction).Symbol);
                if (!dynSettings.Controller.CustomNodeLoader.Contains(symbol))
                {
                    dynSettings.Controller.DynamoViewModel.Log(
                        "WARNING -- No implementation found for node: " + symbol);
                    entry.Error("Could not find .dyf definition file for this node.");
                    return false;
                }

                result = dynSettings.Controller.CustomNodeLoader.GetFunctionDefinition(symbol)
                                    .Workspace.GetTopMostNodes().Any(ContinueTraversalUntilAny);
            }
            _resultDict[entry] = result;
            return result || entry.Inputs.Values.Any(x => x != null && traverseAny(x.Item2));
        }
    }

    public class UIDispatcherEventArgs : EventArgs
    {
        public UIDispatcherEventArgs(Action a)
        {
            ActionToDispatch = a;
        }

        public Action ActionToDispatch { get; set; }
    }
}