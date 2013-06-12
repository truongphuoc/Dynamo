using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Dynamo.Connectors;
using Dynamo.TypeSystem;
using DynamoPython;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

using Microsoft.FSharp.Collections;

using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    
    [NodeName("Python Script")]
    [NodeCategory(BuiltinNodeCategories.SCRIPTING_PYTHON)]
    [NodeDescription("Runs an embedded IronPython script")]
    public class dynPython : dynNodeWithOneOutput, IDrawable
    {
        private bool _dirty = true;
        private Value _lastEvalValue;

        /// <summary>
        /// Allows a scripter to have a persistent reference to previous runs.
        /// </summary>
        private Dictionary<string, dynamic> stateDict = new Dictionary<string, dynamic>();

        private string _script =
            "#The input to this node will be stored in the IN variable.\ndataEnteringNode = IN\n\n#Assign your output to the OUT variable\nOUT = 0";

        public RenderDescription RenderDescription { get; set; }

        private dynScriptEditWindow _editWindow;
        private bool initWindow = false;

        public dynPython()
        {
            InPortData.Add(new PortData("IN", "Input", new AnyType()));
            OutPortData.Add(new PortData("OUT", "Result of the python script", new AnyType()));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Disabled;
        }

        public override void SetupCustomUIElements(Controls.dynNodeView nodeUI)
        {
            //topControl.Height = 200;
            //topControl.Width = 300;

            //add an edit window option to the 
            //main context window
            var editWindowItem = new System.Windows.Controls.MenuItem
            {
                Header = "Edit...",
                IsCheckable = false
            };
            nodeUI.MainContextMenu.Items.Add(editWindowItem);
            editWindowItem.Click += editWindowItem_Click;
            nodeUI.UpdateLayout();
        }

        //TODO: Make this smarter
        public override bool RequiresRecalc
        {
            get { return true; }
            set { }
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

        private List<Binding> makeBindings(IEnumerable<Value> args)
        {
            //Zip up our inputs
            List<Binding> bindings = InPortData
                .Select(x => x.NickName)
                .Zip(args, (s, v) => new Binding(s, Converters.convertFromValue(v)))
                .Concat(PythonBindings.Bindings)
                .ToList();

            bindings.Add(new Binding("__persistent__", stateDict));

            return bindings;
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            Value result = PythonEngine.Evaluator(_dirty, _script, makeBindings(args));
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
                var elem = GetType().Assembly.GetManifestResourceStream("DynamoPython.Resources." + pythonHighlighting);

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
            try
            {
                if (e.Text == ".")
                {
                    completionWindow = new CompletionWindow(_editWindow.editText.TextArea);
                    var data = completionWindow.CompletionList.CompletionData;

                    var completions =
                        completionProvider.GetCompletionData(
                            _editWindow.editText.Text.Substring(
                                0, _editWindow.editText.CaretOffset));

                    if (completions.Length == 0)
                        return;

                    foreach (var ele in completions)
                    {
                        data.Add(ele);
                    }

                    completionWindow.Show();

                    completionWindow.Closed += delegate
                    {
                        completionWindow = null;
                    };
                }
            }
            catch (Exception ex)
            {
                DynamoLogger.Instance.Log("Failed to perform python autocomplete with exception:");
                DynamoLogger.Instance.Log(ex.Message);
                DynamoLogger.Instance.Log(ex.StackTrace);
            }
        }

        private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            try {
                if (e.Text.Length > 0 && completionWindow != null)
                {
                    if (!char.IsLetterOrDigit(e.Text[0]))
                    {
                        completionWindow.CompletionList.RequestInsertion(e);
                    }
                }
            }
            catch (Exception ex)
            {
                DynamoLogger.Instance.Log("Failed to perform python autocomplete with exception:");
                DynamoLogger.Instance.Log(ex.Message);
                DynamoLogger.Instance.Log(ex.StackTrace);
            }
        }

        #endregion

        public void Draw()
        {
            if (RenderDescription == null)
                RenderDescription = new RenderDescription();
            else
                RenderDescription.ClearAll();

            PythonEngine.Drawing(_lastEvalValue, RenderDescription);
        }
    }

    [NodeName("Python Script From String")]
    [NodeCategory(BuiltinNodeCategories.SCRIPTING_PYTHON)]
    [NodeDescription("Runs a IronPython script from a string")]
    public class dynPythonString : dynNodeWithOneOutput
    {

        /// <summary>
        /// Allows a scripter to have a persistent reference to previous runs.
        /// </summary>
        private Dictionary<string, dynamic> stateDict = new Dictionary<string, dynamic>();

        public dynPythonString()
        {
            InPortData.Add(new PortData("script", "Script to run", new StringType()));
            InPortData.Add(new PortData("IN", "Input", new AnyType()));
            OutPortData.Add(new PortData("OUT", "Result of the python script", new AnyType()));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Disabled;
        }

        private List<Binding> makeBindings(IEnumerable<Value> args)
        {
            //Zip up our inputs
            var bindings = 
               InPortData
               .Select(x => x.NickName)
               .Zip(args, (s, v) => new Binding(s, Converters.convertFromValue(v)))
               .Concat(PythonBindings.Bindings)
               .ToList();

            bindings.Add(new Binding("__persistent__", stateDict));

            return bindings;
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            var script = ((Value.String) args[0]).Item;
            var bindings = makeBindings(args);
            var value = PythonEngine.Evaluator( RequiresRecalc, script, bindings);
            return value;
        }
    }
}
