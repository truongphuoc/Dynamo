using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Dynamo.Connectors;
using Dynamo.Controls;
using Dynamo.TypeSystem;
using Dynamo.Utilities;
using DynamoPython;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using IronPython.Hosting;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace Dynamo.Nodes
{
    internal static class Converters
    {
        internal static FScheme.Value convertPyFunction(Func<IList<dynamic>, dynamic> pyf)
        {
            return FScheme.Value.NewFunction(
                FSharpFunc<FSharpList<FScheme.Value>, FScheme.Value>.FromConverter(
                    args =>
                    convertToValue(
                        pyf(args.Select(ex => convertFromValue(ex)).ToList()))));
        }

        internal static FScheme.Value convertToValue(dynamic data)
        {
            if (data is FScheme.Value)
                return data;
            else if (data is string)
                return FScheme.Value.NewString(data);
            else if (data is double)
                return FScheme.Value.NewNumber(data);
            else if (data is IEnumerable<dynamic>)
            {
                FSharpList<FScheme.Value> result = FSharpList<FScheme.Value>.Empty;

                data.reverse();

                foreach (dynamic x in data)
                    result = FSharpList<FScheme.Value>.Cons(convertToValue(x), result);

                return FScheme.Value.NewList(result);
            }
                //else if (data is PythonFunction)
                //{
                //   return FuncContainer.MakeFunction(
                //      new FScheme.ExternFunc(
                //         args =>
                //            convertToValue(
                //               data(args.Select(ex => convertFromValue(ex)))
                //            )
                //      )
                //   );
                //}
                //else if (data is Func<dynamic, dynamic>)
                //{
                //   return Value.NewCurrent(FuncContainer.MakeContinuation(
                //      new Continuation(
                //         exp =>
                //            convertToValue(
                //               data(convertFromValue(exp))
                //            )
                //      )
                //   ));
                //}
            else
                return FScheme.Value.NewContainer(data);
        }

        internal static dynamic convertFromValue(FScheme.Value exp)
        {
            if (exp.IsList)
                return ((FScheme.Value.List)exp).Item.Select(x => convertFromValue(x)).ToList();
            else if (exp.IsNumber)
                return ((FScheme.Value.Number)exp).Item;
            else if (exp.IsString)
                return ((FScheme.Value.String)exp).Item;
            else if (exp.IsContainer)
                return ((FScheme.Value.Container)exp).Item;
                //else if (exp.IsFunction)
                //{
                //   return new Func<IList<dynamic>, dynamic>(
                //      args =>
                //         ((Value.Function)exp).Item
                //            .Invoke(ExecutionEnvironment.IDENT)
                //            .Invoke(Utils.convertSequence(args.Select(
                //               x => (Value)Converters.convertToValue(x)
                //            )))
                //   );
                //}
                //else if (exp.IsSpecial)
                //{
                //   return new Func<IList<dynamic>, dynamic>(
                //      args =>
                //         ((Value.Special)exp).Item
                //            .Invoke(ExecutionEnvironment.IDENT)
                //            .Invoke(
                //}
                //else if (exp.IsCurrent)
                //{
                //   return new Func<dynamic, dynamic>(
                //      ex => 
                //         Converters.convertFromValue(
                //            ((Value.Current)exp).Item.Invoke(Converters.convertToValue(ex))
                //         )
                //   );
                //}
            else
                throw new Exception("Not allowed to pass Functions into a Python Script.");
        }
    }

    internal class DynPythonEngine
    {
        private readonly ScriptEngine engine;
        private ScriptSource source;

        public DynPythonEngine()
        {
            engine = Python.CreateEngine();
        }

        public void ProcessCode(string code)
        {
            code =
                "import clr\nclr.AddReference('RevitAPI')\nclr.AddReference('RevitAPIUI')\nfrom Autodesk.Revit.DB import *\nimport Autodesk\n"
                + code;
            source = engine.CreateScriptSourceFromString(code, SourceCodeKind.Statements);
        }

        public FScheme.Value Evaluate(IEnumerable<Binding> bindings)
        {
            ScriptScope scope = engine.CreateScope();

            foreach (Binding bind in bindings)
                scope.SetVariable(bind.Symbol, bind.Value);

            try
            {
                source.Execute(scope);
            }
            catch (SyntaxErrorException ex)
            {
                throw new Exception(
                    ex.Message
                    + " at Line " + (ex.Line - 4)
                    + ", Column " + ex.Column
                    );
            }
            catch (Exception e)
            {
                dynSettings.Controller.DynamoViewModel.Log("Unable to execute python script:");
                dynSettings.Controller.DynamoViewModel.Log(e.Message);
                dynSettings.Controller.DynamoViewModel.Log(e.StackTrace);

                return FScheme.Value.NewNumber(0);
            }

            FScheme.Value result = FScheme.Value.NewNumber(1);

            if (scope.ContainsVariable("OUT"))
            {
                dynamic output = scope.GetVariable("OUT");

                result = Converters.convertToValue(output);
            }

            return result;
        }
    }

    public struct Binding
    {
        public string Symbol;
        public dynamic Value;

        public Binding(string sym, dynamic val)
        {
            Symbol = sym;
            Value = val;
        }
    }

    public static class PythonBindings
    {
        static PythonBindings()
        {
            Bindings = new HashSet<Binding> { new Binding("__dynamo__", dynSettings.Controller) };
        }

        public static HashSet<Binding> Bindings { get; private set; }
    }

    public static class PythonEngine
    {
        public delegate void DrawDelegate(FScheme.Value val, RenderDescription rd);

        public delegate FScheme.Value EvaluationDelegate(
            bool dirty, string script, IEnumerable<Binding> bindings);

        public static EvaluationDelegate Evaluator;

        public static DrawDelegate Drawing;

        private static readonly DynPythonEngine Engine = new DynPythonEngine();

        static PythonEngine()
        {
            Evaluator = delegate(bool dirty, string script, IEnumerable<Binding> bindings)
            {
                if (dirty)
                    Engine.ProcessCode(script);

                return Engine.Evaluate(PythonBindings.Bindings.Concat(bindings));
            };

            Drawing = delegate { };
        }
    }

    [NodeName("Python Script")]
    [NodeCategory(BuiltinNodeCategories.SCRIPTING_PYTHON)]
    [NodeDescription("Runs an embedded IronPython script")]
    public class dynPython : dynNodeWithOneOutput, IDrawable
    {
        private readonly Dictionary<string, dynamic> _stateDict = new Dictionary<string, dynamic>();
        private bool _dirty = true;
        private FScheme.Value _lastEvalValue;

        private string _script =
            "#The input to this node will be stored in the IN variable.\ndataEnteringNode = IN\n\n#Assign your output to the OUT variable\nOUT = 0";

        private dynScriptEditWindow _editWindow;
        private bool initWindow = false;

        public dynPython()
        {
            InPortData.Add(new PortData("IN", "Input", new AnyType()));
            OutPortData.Add(new PortData("OUT", "Result of the python script", new AnyType()));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Disabled;
        }

        //TODO: Make this smarter
        public override bool RequiresRecalc
        {
            get { return true; }
            set { }
        }

        public RenderDescription RenderDescription { get; set; }

        public void Draw()
        {
            if (RenderDescription == null)
                RenderDescription = new RenderDescription();
            else
                RenderDescription.ClearAll();

            PythonEngine.Drawing(_lastEvalValue, RenderDescription);
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //topControl.Height = 200;
            //topControl.Width = 300;

            //add an edit window option to the 
            //main context window
            var editWindowItem = new MenuItem
            {
                Header = "Edit...",
                IsCheckable = false
            };
            nodeUI.MainContextMenu.Items.Add(editWindowItem);
            editWindowItem.Click += editWindowItem_Click;
            nodeUI.UpdateLayout();
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            XmlElement script = xmlDoc.CreateElement("Script");
            //script.InnerText = this.tb.Text;
            script.InnerText = _script;
            dynEl.AppendChild(script);
        }

        public override void LoadElement(XmlNode elNode)
        {
            foreach (
                XmlNode subNode in
                    elNode.ChildNodes.Cast<XmlNode>().Where(subNode => subNode.Name == "Script"))
            {
                _script = subNode.InnerText;
            }
        }

        private List<Binding> makeBindings(IEnumerable<FScheme.Value> args)
        {
            //Zip up our inputs
            List<Binding> bindings = InPortData
                .Select(x => x.NickName)
                .Zip(args, (s, v) => new Binding(s, Converters.convertFromValue(v)))
                .Concat(PythonBindings.Bindings)
                .ToList();

            bindings.Add(new Binding("__persistant__", _stateDict));

            return bindings;
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FScheme.Value result = PythonEngine.Evaluator(_dirty, _script, makeBindings(args));
            _lastEvalValue = result;
            return result;
        }

        private void editWindowItem_Click(object sender, RoutedEventArgs e)
        {
            if (!initWindow)
            {
                _editWindow = new dynScriptEditWindow();
                // callbacks for autocompletion
                _editWindow.editText.TextArea.TextEntering += textEditor_TextArea_TextEntering;
                _editWindow.editText.TextArea.TextEntered += textEditor_TextArea_TextEntered;

                const string pythonHighlighting = "ICSharpCode.PythonBinding.Resources.Python.xshd";
                Stream elem =
                    GetType()
                        .Assembly.GetManifestResourceStream(
                            "DynamoPython.Resources." + pythonHighlighting);

                _editWindow.editText.SyntaxHighlighting =
                    HighlightingLoader.Load(
                        new XmlTextReader(elem),
                        HighlightingManager.Instance);
            }

            //set the text of the edit window to begin
            _editWindow.editText.Text = _script;

            if (_editWindow.ShowDialog() != true)
                return;

            //set the value from the text in the box
            _script = _editWindow.editText.Text;

            _dirty = true;
        }

        #region Autocomplete

        private readonly IronPythonCompletionProvider completionProvider =
            new IronPythonCompletionProvider();

        private CompletionWindow completionWindow;

        private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                completionWindow = new CompletionWindow(_editWindow.editText.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                ICompletionData[] completions =
                    completionProvider.GetCompletionData(
                        _editWindow.editText.Text.Substring(0, _editWindow.editText.CaretOffset));

                if (completions.Length == 0)
                    return;

                foreach (ICompletionData ele in completions)
                    data.Add(ele);

                completionWindow.Show();

                completionWindow.Closed += delegate { completionWindow = null; };
            }
        }

        private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                    completionWindow.CompletionList.RequestInsertion(e);
            }
        }

        #endregion
    }


    [NodeName("Python Script From String")]
    [NodeCategory(BuiltinNodeCategories.SCRIPTING_PYTHON)]
    [NodeDescription("Runs a IronPython script from a string")]
    public class dynPythonString : dynNodeWithOneOutput
    {
        private readonly Dictionary<string, dynamic> _stateDict = new Dictionary<string, dynamic>();

        public dynPythonString()
        {
            InPortData.Add(new PortData("script", "Script to run", new StringType()));
            InPortData.Add(new PortData("IN", "Input", new AnyType()));
            OutPortData.Add(new PortData("OUT", "Result of the python script", new AnyType()));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Disabled;
        }

        private List<Binding> makeBindings(IEnumerable<FScheme.Value> args)
        {
            //Zip up our inputs
            List<Binding> bindings = InPortData
                .Select(x => x.NickName)
                .Zip(args, (s, v) => new Binding(s, Converters.convertFromValue(v)))
                .Concat(PythonBindings.Bindings)
                .ToList();

            bindings.Add(new Binding("__persistant__", _stateDict));

            return bindings;
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return PythonEngine.Evaluator(
                RequiresRecalc,
                ((FScheme.Value.String)args[0]).Item,
                makeBindings(args.Skip(1)));
        }
    }
}