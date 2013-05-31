using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml;
using Dynamo.Nodes.TypeSystem;
using Microsoft.FSharp.Collections;

using Dynamo.Controls;
using Dynamo.Connectors;
using Dynamo.FSchemeInterop;
using Value = Dynamo.FScheme.Value;

using NCalc;

namespace Dynamo.Nodes
{
    [NodeName("Formula")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Design and compute mathematical expressions.")]
    [NodeSearchTags("Equation", "Arithmetic")]
    [IsInteractive(true)]
    public class dynFormula : dynNodeWithOneOutput
    {
        private string _formula;
        public string Formula
        {
            get
            {
                return _formula;
            }

            set
            {
                if (_formula == null || !_formula.Equals(value))
                {
                    _formula = value;
                    if (value != null)
                    {
                        DisableReporting();
                        processFormula();
                        RaisePropertyChanged("Formula");
                        RequiresRecalc = true;
                        EnableReporting();
                        WorkSpace.Modified();
                    }
                }
            }
        }

        public dynFormula()
        {
            OutPortData.Add(new PortData("", "Result of math computation", new NumberType()));
            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            var tb = new dynTextBox
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                IsNumeric = false,
                Background = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
                DataContext = this
            };

            var bindingVal = new Binding("Formula")
            {
                Mode = BindingMode.TwoWay,
                NotifyOnValidationError = false,
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            tb.SetBinding(TextBox.TextProperty, bindingVal);

            nodeUI.inputGrid.Children.Add(tb);
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, 0);
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            dynEl.SetAttribute("formula", Formula);
        }

        public override void LoadElement(XmlNode elNode)
        {
            Formula = elNode.Attributes["formula"].Value;
            processFormula();
        }

        private static readonly HashSet<string> ReservedNames = new HashSet<string>() { 
            "Abs", "Acos", "Asin", "Atan", "Ceiling", "Cos",
            "Exp", "Floor", "IEEERemainder", "Log", "Log10",
            "Max", "Min", "Pow", "Round", "Sign", "Sin", "Sqrt",
            "Tan", "Truncate", "in", "if"
        };

        private void processFormula()
        {
            Expression e;
            try
            {
                e = new Expression(Formula);
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return;
            }

            if (e.HasErrors())
            {
                Error(e.Error);
                return;
            }

            var parameters = new SortedList<int, Tuple<string, IDynamoType>>();
            var paramSet = new HashSet<string>();

            e.EvaluateFunction += delegate(string name, FunctionArgs args)
            {
                if (paramSet.Contains(name) || ReservedNames.Contains(name))
                    return;

                paramSet.Add(name);

                IEnumerable<IDynamoType> inputs = args.Parameters.Select(x => new GuessType() as IDynamoType);

                parameters.Add(
                    Formula.IndexOf(name, StringComparison.Ordinal), 
                    Tuple.Create(name, new FunctionType(inputs, new NumberType()) as IDynamoType));

                foreach (var p in args.Parameters)
                {
                    p.Evaluate();
                }
                args.Result = 0;
            };

            e.EvaluateParameter += delegate(string name, ParameterArgs args)
            {
                if (paramSet.Contains(name))
                    return;

                paramSet.Add(name);
                parameters.Add(
                    Formula.IndexOf(name, StringComparison.Ordinal), 
                    Tuple.Create(name, new NumberType() as IDynamoType));
                args.Result = 0;
            };

            try
            {
                e.Evaluate();
            }
            catch { }

            InPortData.Clear();

            foreach (var p in parameters.Values)
            {
                InPortData.Add(new PortData(p.Item1, "variable", p.Item2));
            }

            RegisterInputs();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            var e = new Expression(Formula);

            var functionLookup = new Dictionary<string, Value>();

            foreach (var arg in args.Select((arg, i) => new { Value = arg, Index = i }))
            {
                var parameter = InPortData[arg.Index].NickName;
                if (arg.Value.IsFunction)
                    functionLookup[parameter] = arg.Value;
                else
                    e.Parameters[parameter] = ((Value.Number)arg.Value).Item;
            }

            e.EvaluateFunction += delegate(string name, FunctionArgs fArgs)
            {
                if (functionLookup.ContainsKey(name))
                {
                    var func = ((Value.Function)functionLookup[name]).Item;
                    fArgs.Result = ((Value.Number)func.Invoke(
                        Utils.SequenceToFSharpList(
                            fArgs.Parameters.Select(
                                p => Value.NewNumber(Convert.ToDouble(p.Evaluate())))))).Item;
                }
            };

            return Value.NewNumber(Convert.ToDouble(e.Evaluate()));
        }
    }
}
