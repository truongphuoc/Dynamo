using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Xml;
using Dynamo.Connectors;
using Dynamo.Controls;
using Dynamo.FSchemeInterop.Node;
using Dynamo.Nodes;
using Dynamo.TypeSystem;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo
{
    namespace Nodes
    {
        [IsInteractive(false)]
        public class dynFunction : dynNodeModel
        {
            private FunctionDefinition _def;

            protected internal dynFunction(
                IEnumerable<string> inputs, IEnumerable<string> outputs, FunctionDefinition def)
            {
                Symbol = def.FunctionId.ToString();
                _def = def;

                //Set inputs and output
                SetInputs(inputs);
                foreach (string output in outputs)
                    OutPortData.Add(new PortData(output, "function output", null));

                RegisterAllPorts();

                ArgumentLacing = LacingStrategy.Disabled;
            }

            public override void SetupCustomUIElements(dynNodeView nodeUI)
            {
                nodeUI.MouseDoubleClick += ui_MouseDoubleClick;
            }

            private void ui_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                Controller.DynamoViewModel.GoToWorkspaceCommand.Execute(_def.FunctionId);
                e.Handled = true;
            }

            public FunctionDefinition Definition
            {
                get { return _def; }
                internal set
                {
                    _def = value;
                    if (value != null)
                        Symbol = value.FunctionId.ToString();
                }
            }

            public override bool RequiresRecalc
            {
                get
                {
                    //Do we already know we're dirty?
                    bool baseDirty = base.RequiresRecalc;
                    if (baseDirty)
                        return true;

                    return Definition.RequiresRecalc
                           || Definition.Dependencies.Any(x => x.RequiresRecalc);
                }
                set
                {
                    //Set the base value.
                    base.RequiresRecalc = value;
                    //If we're clean, then notify all internals.
                    if (!value)
                    {
                        if (dynSettings.Controller.Running)
                            dynSettings.FunctionWasEvaluated.Add(Definition);
                        else
                        {
                            //Recursion detection start.
                            Definition.RequiresRecalc = false;

                            foreach (FunctionDefinition dep in Definition.Dependencies)
                                dep.RequiresRecalc = false;
                        }
                    }
                }
            }

            protected override IDynamoType GetInputType(int port)
            {
                return Definition.InputTypes[port];
            }

            protected override IDynamoType GetOutputType(int port)
            {
                return Definition.OutputTypes[port];
            }

            /// <summary>
            /// Sets the inputs of this function.
            /// </summary>
            /// <param name="inputs"></param>
            public void SetInputs(IEnumerable<string> inputs)
            {
                int i = 0;
                foreach (string input in inputs)
                {
                    if (InPortData.Count > i)
                        InPortData[i].NickName = input;
                    else
                        InPortData.Add(new PortData(input, "Input #" + (i + 1), null));

                    i++;
                }

                if (i < InPortData.Count)
                {
                    for (int k = i; k < InPortData.Count; k++)
                        InPorts[k].KillAllConnectors();

                    //MVVM: confirm that extension methods on observable collection do what we expect
                    InPortData.RemoveRange(i, InPortData.Count - i);
                }
            }

            public void SetOutputs(IEnumerable<string> outputs)
            {
                int i = 0;
                foreach (string output in outputs)
                {
                    if (OutPortData.Count > i)
                        OutPortData[i].NickName = output;
                    else
                        OutPortData.Add(new PortData(output, "Output #" + (i + 1), null));

                    i++;
                }

                if (i < OutPortData.Count)
                {
                    for (int k = i; k < OutPortData.Count; k++)
                        OutPorts[k].KillAllConnectors();

                    OutPortData.RemoveRange(i, OutPortData.Count - i);
                }
            }

            public override void SaveNode(XmlDocument xmlDoc, XmlElement dynEl, SaveContext context)
            {
                //Debug.WriteLine(pd.Object.GetType().ToString());
                XmlElement outEl = xmlDoc.CreateElement("ID");

                outEl.SetAttribute("value", Symbol);
                dynEl.AppendChild(outEl);

                outEl = xmlDoc.CreateElement("Name");
                outEl.SetAttribute("value", NickName);
                dynEl.AppendChild(outEl);

                outEl = xmlDoc.CreateElement("Inputs");
                foreach (string input in InPortData.Select(x => x.NickName))
                {
                    XmlElement inputEl = xmlDoc.CreateElement("Input");
                    inputEl.SetAttribute("value", input);
                    outEl.AppendChild(inputEl);
                }
                dynEl.AppendChild(outEl);

                outEl = xmlDoc.CreateElement("Outputs");
                foreach (string output in OutPortData.Select(x => x.NickName))
                {
                    XmlElement outputEl = xmlDoc.CreateElement("Output");
                    outputEl.SetAttribute("value", output);
                    outEl.AppendChild(outputEl);
                }
                dynEl.AppendChild(outEl);
            }

            public override void LoadNode(XmlNode elNode)
            {
                foreach (XmlNode subNode in elNode.ChildNodes)
                {
                    if (subNode.Name.Equals("Name"))
                    {
                        NickName = subNode.Attributes[0].Value;
                    }
                    else if (subNode.Name.Equals("ID"))
                    {
                        Symbol = subNode.Attributes[0].Value;

                        Guid funcId;
                        Guid.TryParse(Symbol, out funcId);

                        // if the dyf does not exist on the search path...
                        if (!dynSettings.Controller.CustomNodeLoader.Contains(funcId))
                        {
                            var proxyDef = new FunctionDefinition(funcId)
                            {
                                Workspace =
                                    new FuncWorkspace(
                                        NickName, BuiltinNodeCategories.SCRIPTING_CUSTOMNODES)
                                    {
                                        FilePath = null
                                    }
                            };

                            SetInputs(new List<string>());
                            SetOutputs(new List<string>());
                            RegisterAllPorts();
                            State = ElementState.Error;

                            var userMsg = "Failed to load custom node: " + NickName + ".  Replacing with proxy custom node.";

                            DynamoLogger.Instance.Log(userMsg);

                            // tell custom node loader, but don't provide path, forcing user to resave explicitly
                            dynSettings.Controller.CustomNodeLoader.SetFunctionDefinition(funcId, proxyDef);
                            Definition = dynSettings.Controller.CustomNodeLoader.GetFunctionDefinition(funcId);
                            ArgumentLacing = LacingStrategy.Disabled;
                            return;
                        }
                    }
                    else if (subNode.Name.Equals("Outputs"))
                    {
                        int i = 0;
                        var query = from XmlNode outputNode in subNode.ChildNodes
                                    let xmlAttributeCollection = outputNode.Attributes
                                    where xmlAttributeCollection != null
                                    select
                                        new PortData(
                                        xmlAttributeCollection[0].Value, "Output #" + (i + 1), null);
                        foreach (var data in query)
                        {
                            if (OutPortData.Count > i)
                                OutPortData[i] = data;
                            else
                                OutPortData.Add(data);

                            i++;
                        }
                    }
                    else if (subNode.Name.Equals("Inputs"))
                    {
                        int i = 0;
                        var query = from XmlNode inputNode in subNode.ChildNodes
                                    let xmlAttributeCollection = inputNode.Attributes
                                    where xmlAttributeCollection != null
                                    select
                                        new PortData(
                                        xmlAttributeCollection[0].Value, "Input #" + (i + 1), null);
                        foreach (var data in query)
                        {
                            if (InPortData.Count > i)
                                InPortData[i] = data;
                            else
                                InPortData.Add(data);

                            i++;
                        }
                    }
                    #region Legacy output support

                    else if (subNode.Name.Equals("Output"))
                    {
                        if (subNode.Attributes != null)
                        {
                            var data = new PortData(
                                subNode.Attributes[0].Value, "function output", null);

                            if (OutPortData.Any())
                                OutPortData[0] = data;
                            else
                                OutPortData.Add(data);
                        }
                    }

                    #endregion
                }

                RegisterAllPorts();

                //argument lacing on functions should be set to disabled
                //by default in the constructor, but for any workflow saved
                //before this was the case, we need to ensure it here.
                ArgumentLacing = LacingStrategy.Disabled;

                // we've found a custom node, we need to attempt to load its guid.  
                // if it doesn't exist (i.e. its a legacy node), we need to assign it one,
                // deterministically
                Guid funId;
                try
                {
                    funId = Guid.Parse(Symbol);
                }
                catch
                {
                    funId = GuidUtility.Create(
                        GuidUtility.UrlNamespace, elNode.Attributes["nickname"].Value);
                    Symbol = funId.ToString();
                }

                Definition = dynSettings.Controller.CustomNodeLoader.GetFunctionDefinition(funId);
            }

            protected override InputNode Compile(IEnumerable<string> portNames)
            {
                return SaveResult
                           ? base.Compile(portNames)
                           : new FunctionNode(Symbol);
            }

            public string Symbol { get; set; }

            public override void Evaluate(FSharpList<FScheme.Value> args, Dictionary<PortData, FScheme.Value> outPuts)
            {
                var output = Definition.CompiledValue.Invoke(args);

                if (OutPortData.Count > 1)
                {
                    var query = (output as FScheme.Value.List).Item.Zip(
                        OutPortData, (value, data) => new { value, data });

                    foreach (var result in query)
                        outPuts[result.data] = result.value;
                }
                else
                    outPuts[OutPortData[0]] = output;
            }
        }

        [NodeName("Output")]
        [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
        [NodeDescription("A function output")]
        [IsInteractive(false)]
        public class dynOutput : dynNodeModel
        {
            private string _symbol = "";
            private TextBox _tb;

            public dynOutput()
            {
                InPortData.Add(new PortData("", "", null));

                RegisterAllPorts();
            }

            public override bool RequiresRecalc
            {
                get { return false; }
                set { }
            }

            public string Symbol
            {
                get { return _symbol; }
                set
                {
                    _symbol = value;
                    RaisePropertyChanged("Symbol");
                }
            }

            public override void SetupCustomUIElements(dynNodeView nodeUI)
            {
                //add a text box to the input grid of the control
                _tb = new TextBox
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };
                nodeUI.inputGrid.Children.Add(_tb);
                Grid.SetColumn(_tb, 0);
                Grid.SetRow(_tb, 0);

                //turn off the border
                var backgroundBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                _tb.Background = backgroundBrush;
                _tb.BorderThickness = new Thickness(0);

                _tb.DataContext = this;
                var bindingSymbol = new Binding("Symbol")
                {
                    Mode = BindingMode.TwoWay,
                    Converter = new StringDisplay()
                };
                _tb.SetBinding(TextBox.TextProperty, bindingSymbol);

                _tb.TextChanged += tb_TextChanged;
            }

            private void tb_TextChanged(object sender, TextChangedEventArgs e)
            {
                Symbol = _tb.Text;
            }

            public override void SaveNode(XmlDocument xmlDoc, XmlElement dynEl, SaveContext context)
            {
                //Debug.WriteLine(pd.Object.GetType().ToString());
                XmlElement outEl = xmlDoc.CreateElement("Symbol");
                outEl.SetAttribute("value", Symbol);
                dynEl.AppendChild(outEl);
            }

            public override void LoadNode(XmlNode elNode)
            {
                foreach (XmlNode subNode in elNode.ChildNodes.Cast<XmlNode>().Where(subNode => subNode.Name == "Symbol"))
                {
                    if (subNode.Attributes != null) Symbol = subNode.Attributes[0].Value;
                }
            }
        }

        [NodeName("Input")]
        [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
        [NodeDescription("A function parameter")]
        [NodeSearchTags("variable", "argument", "parameter")]
        [IsInteractive(false)]
        public class dynSymbol : dynNodeModel
        {
            private string _symbol = "";
            private TextBox _tb;

            public dynSymbol()
            {
                OutPortData.Add(new PortData("", "Symbol", null));

                RegisterAllPorts();
            }

            public override bool RequiresRecalc
            {
                get { return false; }
                set { }
            }

            //MVVM: removed direct set of tb.text
            public string Symbol
            {
                get
                {
                    //return tb.Text;
                    return _symbol;
                }
                set
                {
                    //tb.Text = value;
                    _symbol = value;
                    RaisePropertyChanged("Symbol");
                }
            }

            internal override List<TypeCheckResult> TypeCheck(
                int port, FSharpMap<string, TypeScheme> env, FSharpMap<GuessType, IDynamoType> guessEnv,
                Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
            {
                IDynamoType result = env[GUID.ToString()].Instantiate();
                typeDict[this] = new NodeTypeInformation
                {
                    //Inputs = new List<IDynamoType>(),
                    Outputs = new List<List<TypeCheckResult>> { new List<TypeCheckResult> { result } },
                    MapPorts = new List<int>()
                }; 
                return new List<TypeCheckResult> 
                {
                    new TypeCheckResult { Type = result, GuessEnv = guessEnv } 
                };
            }

            public override void SetupCustomUIElements(dynNodeView nodeUI)
            {
                //add a text box to the input grid of the control
                _tb = new TextBox
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };
                nodeUI.inputGrid.Children.Add(_tb);
                Grid.SetColumn(_tb, 0);
                Grid.SetRow(_tb, 0);

                //turn off the border
                var backgroundBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                _tb.Background = backgroundBrush;
                _tb.BorderThickness = new Thickness(0);

                _tb.DataContext = this;
                var bindingSymbol = new Binding("Symbol")
                {
                    Mode = BindingMode.TwoWay
                };
                _tb.SetBinding(TextBox.TextProperty, bindingSymbol);

                _tb.TextChanged += tb_TextChanged;
            }

            private void tb_TextChanged(object sender, TextChangedEventArgs e)
            {
                Symbol = _tb.Text;
            }

            protected internal override INode Build(
                Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort,
                Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
            {
                Dictionary<int, INode> result;
                if (!preBuilt.TryGetValue(this, out result))
                {
                    result = new Dictionary<int, INode>();
                    result[outPort] = new SymbolNode(GUID.ToString());
                    preBuilt[this] = result;
                }
                return result[outPort];
            }

            public override void SaveNode(XmlDocument xmlDoc, XmlElement dynEl, SaveContext context)
            {
                //Debug.WriteLine(pd.Object.GetType().ToString());
                XmlElement outEl = xmlDoc.CreateElement("Symbol");
                outEl.SetAttribute("value", Symbol);
                dynEl.AppendChild(outEl);
            }

            public override void LoadNode(XmlNode elNode)
            {
                foreach (XmlNode subNode in elNode.ChildNodes.Cast<XmlNode>().Where(subNode => subNode.Name == "Symbol"))
                {
                    if (subNode.Attributes != null) Symbol = subNode.Attributes[0].Value;
                }
            }
        }

        #region Disabled Anonymous Function Node

        //[RequiresTransaction(false)]
        //[IsInteractive(false)]
        //public class dynAnonFunction : dynElement
        //{
        //   private INode entryPoint;

        //   public dynAnonFunction(IEnumerable<string> inputs, string output, INode entryPoint)
        //   {
        //      int i = 1;
        //      foreach (string input in inputs)
        //      {
        //         InPortData.Add(new PortData(null, input, "Input #" + i++, typeof(object)));
        //      }

        //      OutPortData = new PortData(null, output, "function output", typeof(object));

        //      entryPoint = entryPoint;

        //      NodeUI.RegisterInputsAndOutput();
        //   }

        //   protected internal override ProcedureCallNode Compile(IEnumerable<string> portNames)
        //   {
        //      return new AnonymousFunctionNode(portNames, entryPoint);
        //   }
        //}

        #endregion
    }

    public class FunctionDefinition
    {
        internal FunctionDefinition() : this(Guid.NewGuid()) { }

        internal FunctionDefinition(Guid id)
        {
            FunctionId = id;
            RequiresRecalc = true;
        }

        public Guid FunctionId { get; private set; }
        public dynWorkspaceModel Workspace { get; internal set; }
        public List<Tuple<int, dynNodeModel>> OutPortMappings { get; internal set; }
        public List<Tuple<int, dynNodeModel>> InPortMappings { get; internal set; }
        public List<IDynamoType> InputTypes { get; internal set; }
        public List<IDynamoType> OutputTypes { get; internal set; }
        public bool RequiresRecalc { get; internal set; }

        public FSharpFunc<FSharpList<FScheme.Value>, FScheme.Value> CompiledValue { get; internal set; }

        public IEnumerable<FunctionDefinition> Dependencies
        {
            get { return findAllDependencies(new HashSet<FunctionDefinition>()); }
        }

        private IEnumerable<FunctionDefinition> findAllDependencies(
            HashSet<FunctionDefinition> dependencySet)
        {
            IEnumerable<FunctionDefinition> query =
                Workspace.Nodes
                         .OfType<dynFunction>()
                         .Select(node => node.Definition)
                         .Where(
                             def => !dependencySet.Contains(def));
            foreach (FunctionDefinition definition in query)
            {
                yield return definition;
                dependencySet.Add(definition);
                foreach (FunctionDefinition def in definition.findAllDependencies(dependencySet))
                    yield return def;
            }
        }

        internal IDynamoType GetOutputType(int port, FSharpMap<string, TypeScheme> env)
        {
            throw new NotImplementedException();
        }
    }
}
