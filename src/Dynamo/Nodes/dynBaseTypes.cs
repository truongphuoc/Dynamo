using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Dynamo.Connectors;
using Dynamo.Controls;
using Dynamo.FSchemeInterop;
using Dynamo.FSchemeInterop.Node;
using Dynamo.TypeSystem;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using Panel = System.Windows.Controls.Panel;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    /// <summary>
    /// Built-in Dynamo Categories. If you want your node to appear in one of the existing Dynamo
    /// categories, then use these constants. This ensures that if the names of the categories
    /// change down the road, your node will still be placed there.
    /// </summary>
    public static class BuiltinNodeCategories
    {
        public const string CORE = "Core";
        public const string CORE_PRIMITIVES = "Core.Primitives";
        public const string CORE_STRINGS = "Core.Strings";
        public const string CORE_LISTS = "Core.Lists";
        public const string CORE_VIEW = "Core.View";
        public const string CORE_ANNOTATE = "Core.Annotate";
        public const string CORE_SELECTION = "Revit.Selection";
        public const string CORE_EVALUATE = "Core.Evaluate";
        public const string CORE_TIME = "Core.Time";

        public const string LOGIC = "Logic";
        public const string LOGIC_MATH = "Logic.Math";
        public const string LOGIC_COMPARISON = "Logic.Comparison";
        public const string LOGIC_CONDITIONAL = "Logic.Conditional";
        public const string LOGIC_LOOP = "Logic.Loop";

        public const string CREATEGEOMETRY = "Create Geometry";
        public const string CREATEGEOMETRY_POINT = "Create Geometry.Point";
        public const string CREATEGEOMETRY_CURVE = "Create Geometry.Curve";
        public const string CREATEGEOMETRY_SOLID = "Create Geometry.Solid";
        public const string CREATEGEOMETRY_SURFACE = "Create Geometry.Surface";

        public const string MODIFYGEOMETRY = "Modify Geometry";
        public const string MODIFYGEOMETRY_INTERSECT = "Modify Geometry.Intersect";
        public const string MODIFYGEOMETRY_TRANSFORM = "Modify Geometry.Transform";
        public const string MODIFYGEOMETRY_TESSELATE = "Modify Geometry.Tesselate";

        public const string REVIT = "Revit";
        public const string REVIT_DOCUMENT = "Revit.Document";
        public const string REVIT_DATUMS = "Revit.Datums";
        public const string REVIT_FAMILYCREATION = "Revit.Family Creation";
        public const string REVIT_VIEW = "Revit.View";
        public const string REVIT_PARAMETERS = "Revit.Parameters";
        public const string REVIT_BAKE = "Revit.Bake";
        public const string REVIT_API = "Revit.API";

        public const string IO = "Input/Output";
        public const string IO_FILE = "Input/Output.File";
        public const string IO_NETWORK = "Input/Output.Network";
        public const string IO_HARDWARE = "Input/Output.Hardware";

        public const string ANALYZE = "Analyze";
        public const string ANALYZE_MEASURE = "Analyze.Measure";
        public const string ANALYZE_DISPLAY = "Analyze.Display";
        public const string ANALYZE_SURFACE = "Analyze.Surface";
        public const string ANALYZE_STRUCTURE = "Analyze.Structure";
        public const string ANALYZE_CLIMATE = "Analyze.Climate";
        public const string ANALYZE_ACOUSTIC = "Analyze.Acoustic";
        public const string ANALYZE_SOLAR = "Analyze.Solar";

        public const string SCRIPTING = "Scripting";
        public const string SCRIPTING_CUSTOMNODES = "Scripting.Custom Nodes";
        public const string SCRIPTING_PYTHON = "Scripting.Python";
        public const string SCRIPTING_DESIGNSCRIPT = "Scripting.DesignScript";
    }

    internal static class Utilities
    {
        public static string Ellipsis(string value, int desiredLength)
        {
            return desiredLength > value.Length ? value : value.Remove(desiredLength - 1) + "...";
        }
    }

    #region FScheme Builtin Interop

    public abstract class dynBuiltinFunction : dynNodeWithOneOutput
    {
        internal dynBuiltinFunction(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; protected internal set; }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return SaveResult ? base.Compile(portNames) : new FunctionNode(Symbol, portNames);
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FScheme.Value val = ((FScheme.Value.Function)Controller.FSchemeEnvironment.LookupSymbol(Symbol))
                .Item.Invoke(args);

            FSharpFunc<FSharpList<FScheme.Value>, FScheme.Value> symbol =
                ((FScheme.Value.Function)Controller.FSchemeEnvironment.LookupSymbol(Symbol)).Item;

            return val;
        }
    }

    #endregion

    public abstract class dynVariableInput : dynNodeWithOneOutput
    {
        private int _lastEvaledAmt;

        public override bool RequiresRecalc
        {
            get { return _lastEvaledAmt != InPortData.Count || base.RequiresRecalc; }
            set { base.RequiresRecalc = value; }
        }

        protected abstract override IDynamoType GetInputType(int index);

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            var addButton = new Button
            {
                Content = "+",
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var subButton = new Button
            {
                Content = "-",
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            nodeUI.inputGrid.ColumnDefinitions.Add(new ColumnDefinition());
            nodeUI.inputGrid.ColumnDefinitions.Add(new ColumnDefinition());

            nodeUI.inputGrid.Children.Add(addButton);
            Grid.SetColumn(addButton, 0);

            nodeUI.inputGrid.Children.Add(subButton);
            Grid.SetColumn(subButton, 1);

            addButton.Click += delegate
            {
                AddInput();
                RegisterAllPorts();
            };
            subButton.Click += delegate
            {
                RemoveInput();
                RegisterAllPorts();
            };
        }

        protected abstract string getInputRootName();

        protected virtual int getNewInputIndex()
        {
            return InPortData.Count;
        }

        protected internal virtual void RemoveInput()
        {
            int count = InPortData.Count;
            if (count > 0)
                InPortData.RemoveAt(count - 1);
        }

        protected internal virtual void AddInput()
        {
            InPortData.Add(new PortData(getInputRootName() + getNewInputIndex(), "", null));
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            //Debug.WriteLine(pd.Object.GetType().ToString());
            foreach (PortData inport in InPortData)
            {
                XmlElement input = xmlDoc.CreateElement("Input");

                input.SetAttribute("name", inport.NickName);

                dynEl.AppendChild(input);
            }
        }

        public override void LoadElement(XmlNode elNode)
        {
            int i = InPortData.Count;
            foreach (XmlNode subNode in elNode.ChildNodes)
            {
                if (i > 0)
                {
                    i--;
                    continue;
                }

                if (subNode.Name == "Input")
                    InPortData.Add(new PortData(subNode.Attributes["name"].Value, "", null));
            }
            RegisterAllPorts();
        }

        protected override void OnEvaluate()
        {
            _lastEvaledAmt = InPortData.Count;
        }
    }

    [NodeName("Identity")]
    [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
    [NodeDescription("Identity function")]
    public class dynIdentity : dynNodeWithOneOutput
    {
        public dynIdentity()
        {
            var t = new PolymorphicType();

            InPortData.Add(new PortData("x", "in", t));
            OutPortData.Add(new PortData("x", "out", t));
            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return args[0];
        }
    }

    #region Lists

    [NodeName("Reverse")]
    [NodeDescription("Reverses a list")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    public class dynReverse : dynBuiltinFunction
    {
        public dynReverse()
            : base("reverse")
        {
            var pType = new PolymorphicType();
            var t1 = new ListType(pType);

            InPortData.Add(new PortData("list", "List to sort", t1));
            OutPortData.Add(new PortData("rev", "Reversed list", t1));

            RegisterAllPorts();
        }
    }

    [NodeName("List")]
    [NodeDescription("Makes a new list out of the given inputs")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    public class dynNewList : dynVariableInput
    {
        public dynNewList()
        {
            InPortData.Add(new PortData("item(s)", "Item(s) to build a list out of", new PolymorphicType()));
            OutPortData.Add(new PortData("list", "A list", null));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Disabled;
        }

        protected override IDynamoType GetInputType(int index)
        {
            return InPortData[index].PortType;
        }

        protected override IDynamoType GetOutputType(int index)
        {
            return new ListType(
                InPortData.Count == 1
                    ? GetInputType(0)
                    : new TypeUnion(InPortData.Select((x, i) => GetInputType(i))));
        }

        protected override string getInputRootName()
        {
            return "index";
        }

        protected internal override void RemoveInput()
        {
            if (InPortData.Count == 2)
            {
                InPortData[0].NickName = "item(s)";
                InPortData[0].ToolTipString = "Item(s) to build a list out of";
            }
            if (InPortData.Count > 1)
                base.RemoveInput();
        }

        protected internal override void AddInput()
        {
            if (InPortData.Count == 1)
            {
                InPortData[0].NickName = "index0";
                InPortData[0].ToolTipString = "First item";
            }
            base.AddInput();
        }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return SaveResult ? base.Compile(portNames) : new FunctionNode("list", portNames);
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return ((FScheme.Value.Function)Controller.FSchemeEnvironment.LookupSymbol("list"))
                .Item.Invoke(args);
        }
    }

    [NodeName("Sort-With")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Returns a sorted list, using the given comparitor.")]
    public class dynSortWith : dynBuiltinFunction
    {
        public dynSortWith()
            : base("sort-with")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("c(x, y)", "Comparitor", new FunctionType(pType, pType, new NumberType())));
            InPortData.Add(new PortData("list", "List to sort", new ListType(pType)));
            OutPortData.Add(new PortData("sorted", "Sorted list", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("Sort-By")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Returns a sorted list, using the given key mapper.")]
    public class dynSortBy : dynBuiltinFunction
    {
        public dynSortBy()
            : base("sort-by")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("c(x)", "Key Mapper",
                                        new FunctionType(pType, new TypeUnion(new StringType(), new NumberType()))));
            InPortData.Add(new PortData("list", "List to sort", new ListType(pType)));
            OutPortData.Add(new PortData("sorted", "Sorted list", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("Sort")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Returns a sorted list of numbers or strings.")]
    public class dynSort : dynBuiltinFunction
    {
        public dynSort()
            : base("sort")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("list", "List of numbers or strings to sort", new ListType(pType)));
            OutPortData.Add(new PortData("sorted", "Sorted list", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("Reduce")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Reduces a sequence.")]
    [NodeSearchTags("foldl")]
    public class dynFold : dynBuiltinFunction
    {
        public dynFold()
            : base("foldl")
        {
            var a = new PolymorphicType();
            var b = new PolymorphicType();

            InPortData.Add(new PortData("f(x, a)", "Reductor Funtion", new FunctionType(a, b, b)));
            InPortData.Add(new PortData("a", "Seed", b));
            InPortData.Add(new PortData("seq", "Sequence", new ListType(a)));
            OutPortData.Add(new PortData("out", "Result", b));

            RegisterAllPorts();
        }
    }

    [NodeName("Filter")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Filters a sequence by a given predicate")]
    public class dynFilter : dynBuiltinFunction
    {
        public dynFilter()
            : base("filter")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("p(x)", "Predicate", new FunctionType(pType, new NumberType())));
            InPortData.Add(new PortData("seq", "Sequence to filter", new ListType(pType)));
            OutPortData.Add(new PortData("filtered", "Filtered Sequence", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("Number Sequence")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Creates a sequence of numbers")]
    [NodeSearchTags("range")]
    public class dynBuildSeq : dynBuiltinFunction
    {
        public dynBuildSeq()
            : base("build-list")
        {
            InPortData.Add(new PortData("start", "Number to start the sequence at", new NumberType()));
            InPortData.Add(new PortData("end", "Number to end the sequence at", new NumberType()));
            InPortData.Add(new PortData("step", "Space between numbers", new NumberType()));
            OutPortData.Add(new PortData("seq", "New sequence", new ListType(new NumberType())));

            RegisterAllPorts();
        }
    }

    [NodeName("Combine")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Applies a combinator to each element in two sequences")]
    [NodeSearchTags("zip")]
    public class dynCombine : dynVariableInput
    {
        private readonly PolymorphicType _outType = new PolymorphicType();

        public dynCombine()
        {
            var a = new PolymorphicType();
            var b = new PolymorphicType();

            InPortData.Add(new PortData("comb", "Combinator", null));
            InPortData.Add(new PortData("list1", "First list", new ListType(a)));
            InPortData.Add(new PortData("list2", "Second list", new ListType(b)));
            OutPortData.Add(new PortData("combined", "Combined lists", new ListType(_outType)));

            RegisterAllPorts();
            ArgumentLacing = LacingStrategy.Disabled;
        }

        protected override IDynamoType GetInputType(int index)
        {
            return index == 0
                       ? (IDynamoType)new FunctionType(InPortData.Skip(1).Select(x => x.PortType), _outType)
                       : new PolymorphicType();
        }

        protected override string getInputRootName()
        {
            return "list";
        }

        protected internal override void RemoveInput()
        {
            if (InPortData.Count == 3)
            {
                InPortData[1].NickName = "lists";
                InPortData[2].NickName = "List of lists to combine";
            }
            if (InPortData.Count > 2)
                base.RemoveInput();
        }

        protected internal override void AddInput()
        {
            if (InPortData.Count == 2)
            {
                InPortData[1].NickName = "list1";
                InPortData[1].ToolTipString = "First list";
            }
            base.AddInput();
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            dynEl.SetAttribute("inputs", (InPortData.Count - 1).ToString());
        }

        public override void LoadElement(XmlNode elNode)
        {
            XmlAttribute inputAttr = elNode.Attributes["inputs"];
            int inputs = inputAttr == null ? 2 : Convert.ToInt32(inputAttr.Value);
            if (inputs == 1)
                RemoveInput();
            else
            {
                for (; inputs > 2; inputs--)
                {
                    var t = new PolymorphicType();
                    InPortData.Add(new PortData(getInputRootName() + getNewInputIndex(), "", new ListType(t)));
                }

                RegisterAllPorts();
            }
        }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return SaveResult ? base.Compile(portNames) : new FunctionNode("map", portNames);
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return ((FScheme.Value.Function)Controller.FSchemeEnvironment.LookupSymbol("map"))
                .Item.Invoke(args);
        }
    }

    [NodeName("Cartesian Product")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Applies a combinator to each pair in the cartesian product of two sequences")]
    [NodeSearchTags("cross")]
    public class dynCartProd : dynVariableInput
    {
        private readonly PolymorphicType _outType = new PolymorphicType();

        public dynCartProd()
        {
            InPortData.Add(new PortData("comb", "Combinator", null));
            InPortData.Add(new PortData("list1", "First list", new ListType(new PolymorphicType())));
            InPortData.Add(new PortData("list2", "Second list", new ListType(new PolymorphicType())));
            OutPortData.Add(new PortData("combined", "Combined lists", new ListType(_outType)));

            RegisterAllPorts();
        }

        protected override IDynamoType GetInputType(int index)
        {
            return index == 0
                       ? (IDynamoType)new FunctionType(InPortData.Skip(1).Select(x => x.PortType), _outType)
                       : new PolymorphicType();
        }

        protected override string getInputRootName()
        {
            return "list";
        }

        protected internal override void RemoveInput()
        {
            if (InPortData.Count == 3)
            {
                InPortData[1].NickName = "lists";
                InPortData[1].ToolTipString = "List of lists to combine";
            }
            if (InPortData.Count > 2)
                base.RemoveInput();
        }

        protected internal override void AddInput()
        {
            if (InPortData.Count == 2)
            {
                InPortData[1].NickName = "list1";
                InPortData[1].ToolTipString = "First list";
            }
            base.AddInput();
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            dynEl.SetAttribute("inputs", (InPortData.Count - 1).ToString());
        }

        public override void LoadElement(XmlNode elNode)
        {
            XmlAttribute inputAttr = elNode.Attributes["inputs"];
            int inputs = inputAttr == null ? 2 : Convert.ToInt32(inputAttr.Value);
            if (inputs == 1)
                RemoveInput();
            else
            {
                for (; inputs > 2; inputs--)
                {
                    InPortData.Add(new PortData(getInputRootName() + getNewInputIndex(), "",
                                                new ListType(new PolymorphicType())));
                }

                RegisterAllPorts();
            }
        }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return SaveResult ? base.Compile(portNames) : new FunctionNode("cartesian-product", portNames);
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return ((FScheme.Value.Function)Controller.FSchemeEnvironment.LookupSymbol("cartesian-product"))
                .Item.Invoke(args);
        }
    }

    [NodeName("Map")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Maps a sequence")]
    public class dynMap : dynBuiltinFunction
    {
        public dynMap()
            : base("map")
        {
            var t = new PolymorphicType();
            var t2 = new PolymorphicType();

            InPortData.Add(new PortData("f(x)", "The procedure used to map elements", new FunctionType(t, t2)));
            InPortData.Add(new PortData("seq", "The sequence to map over.", new ListType(t)));
            OutPortData.Add(new PortData("mapped", "Mapped sequence", new ListType(t2)));

            RegisterAllPorts();
        }
    }

    [NodeName("Split Pair")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Deconstructs a list pair.")]
    public class dynDeCons : dynNodeModel
    {
        public dynDeCons()
        {
            var t = new PolymorphicType();

            InPortData.Add(new PortData("list", "", new ListType(t)));
            OutPortData.Add(new PortData("first", "", t));
            OutPortData.Add(new PortData("rest", "", new ListType(t)));

            RegisterAllPorts();
        }

        public override void Evaluate(FSharpList<FScheme.Value> args, Dictionary<PortData, FScheme.Value> outPuts)
        {
            var list = (FScheme.Value.List)args[0];

            outPuts[OutPortData[0]] = list.Item.Head;
            outPuts[OutPortData[1]] = FScheme.Value.NewList(list.Item.Tail);
        }
    }

    [NodeName("Make Pair")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Constructs a list pair.")]
    public class dynList : dynBuiltinFunction
    {
        public dynList()
            : base("cons")
        {
            var t = new PolymorphicType();

            InPortData.Add(new PortData("first", "The new Head of the list", t));
            InPortData.Add(new PortData("rest", "The new Tail of the list", new ListType(t)));
            OutPortData.Add(new PortData("list", "Result List", new ListType(t)));

            RegisterAllPorts();
        }
    }

    [NodeName("Take From List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Takes elements from a list")]
    public class dynTakeList : dynBuiltinFunction
    {
        public dynTakeList()
            : base("take")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("amt", "Amount of elements to extract", new NumberType()));
            InPortData.Add(new PortData("list", "The list to extract elements from", new ListType(pType)));
            OutPortData.Add(new PortData("elements", "List of extraced elements", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("Drop From List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Drops elements from a list")]
    public class dynDropList : dynBuiltinFunction
    {
        public dynDropList()
            : base("drop")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("amt", "Amount of elements to drop", new NumberType()));
            InPortData.Add(new PortData("list", "The list to drop elements from", new ListType(pType)));
            OutPortData.Add(new PortData("elements", "List of remaining elements", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("Get From List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Gets an element from a list at a specified index.")]
    public class dynGetFromList : dynBuiltinFunction
    {
        public dynGetFromList()
            : base("get")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("index", "Index of the element to extract", new NumberType()));
            InPortData.Add(new PortData("list", "The list to extract elements from", new ListType(pType)));
            OutPortData.Add(new PortData("element", "Extracted element", pType));

            RegisterAllPorts();
        }
    }

    [NodeName("Empty List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("An empty list")]
    [IsInteractive(false)]
    public class dynEmpty : dynNodeWithOneOutput
    {
        public dynEmpty()
        {
            OutPortData.Add(new PortData("empty", "An empty list", new ListType(new PolymorphicType())));

            RegisterAllPorts();
        }

        public override bool RequiresRecalc
        {
            get { return false; }
            set { }
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewList(FSharpList<FScheme.Value>.Empty);
        }

        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            Dictionary<int, INode> result;
            if (!preBuilt.TryGetValue(this, out result))
            {
                result = new Dictionary<int, INode>();
                result[outPort] = new SymbolNode("empty");
                preBuilt[this] = result;
            }
            return result[outPort];
        }
    }

    [NodeName("Is Empty List?")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Checks to see if the given list is empty.")]
    public class dynIsEmpty : dynBuiltinFunction
    {
        public dynIsEmpty()
            : base("empty?")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("list", "A list", new ListType(pType)));
            OutPortData.Add(new PortData("empty?", "Is the given list empty?", new NumberType()));

            RegisterAllPorts();
        }
    }

    [NodeName("List Length")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Gets the length of a list")]
    [NodeSearchTags("count")]
    public class dynLength : dynBuiltinFunction
    {
        public dynLength()
            : base("len")
        {
            InPortData.Add(new PortData("list", "A list", new ListType(new PolymorphicType())));
            OutPortData.Add(new PortData("length", "Length of the list", new NumberType()));

            RegisterAllPorts();
        }
    }

    [NodeName("Append to List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Appends two list")]
    public class dynAppend : dynBuiltinFunction
    {
        public dynAppend()
            : base("append")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("listA", "First list", new ListType(pType)));
            InPortData.Add(new PortData("listB", "Second list", new ListType(pType)));
            OutPortData.Add(new PortData("A+B", "A appended onto B", new ListType(pType)));

            RegisterAllPorts();
        }
    }

    [NodeName("First in List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Gets the first element of a list")]
    public class dynFirst : dynBuiltinFunction
    {
        public dynFirst()
            : base("first")
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("list", "A list", new ListType(pType)));
            OutPortData.Add(new PortData("first", "First element in the list", pType));

            RegisterAllPorts();
        }
    }

    [NodeName("Rest of List")]
    [NodeCategory(BuiltinNodeCategories.CORE_LISTS)]
    [NodeDescription("Gets the list with the first element removed.")]
    public class dynRest : dynBuiltinFunction
    {
        public dynRest()
            : base("rest")
        {
            var t = new ListType(new PolymorphicType());

            InPortData.Add(new PortData("list", "A list", t));
            OutPortData.Add(new PortData("rest", "List without the first element.", t));

            RegisterAllPorts();
        }
    }

    #endregion

    #region Boolean

    public abstract class dynComparison : dynBuiltinFunction
    {
        protected dynComparison(string op) : this(op, op) { }

        protected dynComparison(string op, string name)
            : base(op)
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x" + name + "y", "comp", new NumberType()));
            RegisterAllPorts();
        }
    }

    [NodeName("Less Than")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_COMPARISON)]
    [NodeDescription("Compares two numbers.")]
    [NodeSearchTags("less", "than", "<")]
    public class dynLessThan : dynComparison
    {
        public dynLessThan() : base("<") { }
    }

    [NodeName("Less Than Or Equal")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_COMPARISON)]
    [NodeDescription("Compares two numbers.")]
    [NodeSearchTags("<=")]
    public class dynLessThanEquals : dynComparison
    {
        public dynLessThanEquals() : base("<=", "≤") { }
    }

    [NodeName("Greater Than")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_COMPARISON)]
    [NodeDescription("Compares two numbers.")]
    [NodeSearchTags(">")]
    public class dynGreaterThan : dynComparison
    {
        public dynGreaterThan() : base(">") { }
    }

    [NodeName("Greater Than Or Equal")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_COMPARISON)]
    [NodeDescription("Compares two numbers.")]
    [NodeSearchTags(">=", "Greater Than Or Equal")]
    public class dynGreaterThanEquals : dynComparison
    {
        public dynGreaterThanEquals() : base(">=", "≥") { }
    }

    [NodeName("Equal")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_COMPARISON)]
    [NodeDescription("Compares two numbers.")]
    public class dynEqual : dynComparison
    {
        public dynEqual() : base("=") { }
    }

    [NodeName("And")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_CONDITIONAL)]
    [NodeDescription("Boolean AND.")]
    public class dynAnd : dynBuiltinFunction
    {
        public dynAnd()
            : base("and")
        {
            InPortData.Add(new PortData("a", "operand", new NumberType()));
            InPortData.Add(new PortData("b", "operand", new NumberType()));
            OutPortData.Add(new PortData("a∧b", "result", new NumberType()));
            RegisterAllPorts();
        }


        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            Dictionary<int, INode> result;
            if (!preBuilt.TryGetValue(this, out result))
            {
                if (Enumerable.Range(0, InPortData.Count).All(HasInput))
                {
                    var ifNode = new ConditionalNode();
                    ifNode.ConnectInput("test", Inputs[0].Item2.Build(preBuilt, Inputs[0].Item1, typeDict));
                    ifNode.ConnectInput("true", Inputs[1].Item2.Build(preBuilt, Inputs[1].Item1, typeDict));
                    ifNode.ConnectInput("false", new NumberNode(0));
                    result = new Dictionary<int, INode>();
                    result[outPort] = ifNode;
                }
                else
                {
                    var ifNode = new ConditionalNode();
                    ifNode.ConnectInput("test", new SymbolNode(InPortData[0].NickName));
                    ifNode.ConnectInput("true", new SymbolNode(InPortData[1].NickName));
                    ifNode.ConnectInput("false", new NumberNode(0));

                    var node = new AnonymousFunctionNode(
                        InPortData.Select(x => x.NickName),
                        ifNode);

                    //For each index in InPortData
                    //for (int i = 0; i < InPortData.Count; i++)
                    foreach (int data in Enumerable.Range(0, InPortData.Count).Where(HasInput)) 
                    {
                        //Compile input and connect it
                        node.ConnectInput(
                            InPortData[data].NickName,
                            Inputs[data].Item2.Build(preBuilt, Inputs[data].Item1, typeDict)
                            );
                    }

                    RequiresRecalc = false;
                    OnEvaluate();

                    result = new Dictionary<int, INode>();
                    result[outPort] = node;
                }
                preBuilt[this] = result;
            }
            return result[outPort];
        }
    }

    [NodeName("Or")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_CONDITIONAL)]
    [NodeDescription("Boolean OR.")]
    public class dynOr : dynBuiltinFunction
    {
        public dynOr()
            : base("or")
        {
            InPortData.Add(new PortData("a", "operand", new NumberType()));
            InPortData.Add(new PortData("b", "operand", new NumberType()));
            OutPortData.Add(new PortData("a∨b", "result", new NumberType()));
            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI) { }

        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            Dictionary<int, INode> result;
            if (!preBuilt.TryGetValue(this, out result))
            {
                if (Enumerable.Range(0, InPortData.Count).All(HasInput))
                {
                    var ifNode = new ConditionalNode();
                    ifNode.ConnectInput("test", Inputs[0].Item2.Build(preBuilt, Inputs[0].Item1, typeDict));
                    ifNode.ConnectInput("true", new NumberNode(1));
                    ifNode.ConnectInput("false", Inputs[1].Item2.Build(preBuilt, Inputs[1].Item1, typeDict));

                    result = new Dictionary<int, INode>();
                    result[outPort] = ifNode;
                }
                else
                {
                    var ifNode = new ConditionalNode();
                    ifNode.ConnectInput("test", new SymbolNode(InPortData[0].NickName));
                    ifNode.ConnectInput("true", new NumberNode(1));
                    ifNode.ConnectInput("false", new SymbolNode(InPortData[1].NickName));

                    var node = new AnonymousFunctionNode(
                        InPortData.Select(x => x.NickName),
                        ifNode);

                    //For each index in InPortData
                    //for (int i = 0; i < InPortData.Count; i++)
                    foreach (int data in Enumerable.Range(0, InPortData.Count).Where(HasInput)) 
                    {
                        //Compile input and connect it
                        node.ConnectInput(
                            InPortData[data].NickName,
                            Inputs[data].Item2.Build(preBuilt, Inputs[data].Item1, typeDict)
                            );
                    }

                    RequiresRecalc = false;
                    OnEvaluate();

                    result = new Dictionary<int, INode>();
                    result[outPort] = node;
                }
                preBuilt[this] = result;
            }
            return result[outPort];
        }
    }

    [NodeName("Xor")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_CONDITIONAL)]
    [NodeDescription("Boolean XOR.")]
    public class dynXor : dynBuiltinFunction
    {
        public dynXor()
            : base("xor")
        {
            InPortData.Add(new PortData("a", "operand", new NumberType()));
            InPortData.Add(new PortData("b", "operand", new NumberType()));
            OutPortData.Add(new PortData("a⊻b", "result", new NumberType()));
            RegisterAllPorts();
        }
    }

    [NodeName("Not")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_CONDITIONAL)]
    [NodeDescription("Boolean NOT.")]
    public class dynNot : dynBuiltinFunction
    {
        public dynNot()
            : base("not")
        {
            InPortData.Add(new PortData("a", "operand", new NumberType()));
            OutPortData.Add(new PortData("!a", "result", new NumberType()));
            RegisterAllPorts();
        }
    }

    #endregion

    #region Math

    [NodeName("Add")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Adds two numbers.")]
    [NodeSearchTags("plus", "sum", "+")]
    public class dynAddition : dynBuiltinFunction
    {
        public dynAddition()
            : base("+")
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x+y", "sum", new NumberType()));
            RegisterAllPorts();
        }
    }

    [NodeName("Subtract")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Subtracts two numbers.")]
    [NodeSearchTags("minus", "difference", "-")]
    public class dynSubtraction : dynBuiltinFunction
    {
        public dynSubtraction()
            : base("-")
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x-y", "difference", new NumberType()));
            RegisterAllPorts();
        }
    }

    [NodeName("Multiply")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Multiplies two numbers.")]
    [NodeSearchTags("times", "product", "*")]
    public class dynMultiplication : dynBuiltinFunction
    {
        public dynMultiplication()
            : base("*")
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x∙y", "product", new NumberType()));
            RegisterAllPorts();
        }
    }

    [NodeName("Divide")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Divides two numbers.")]
    [NodeSearchTags("division", "quotient", "/")]
    public class dynDivision : dynBuiltinFunction
    {
        public dynDivision()
            : base("/")
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x÷y", "result", new NumberType()));
            RegisterAllPorts();
        }
    }

    [NodeName("Modulo")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Remainder of division of two numbers.")]
    [NodeSearchTags("%", "remainder")]
    public class dynModulo : dynBuiltinFunction
    {
        public dynModulo()
            : base("%")
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x%y", "result", new NumberType()));

            RegisterAllPorts();
        }
    }

    [NodeName("Power")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Raises a number to the power of another.")]
    [NodeSearchTags("pow", "exponentiation", "^")]
    public class dynPow : dynBuiltinFunction
    {
        public dynPow()
            : base("pow")
        {
            InPortData.Add(new PortData("x", "operand", new NumberType()));
            InPortData.Add(new PortData("y", "operand", new NumberType()));
            OutPortData.Add(new PortData("x^y", "result", new NumberType()));

            RegisterAllPorts();
        }
    }

    [NodeName("Round")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Rounds a number to the nearest integer value.")]
    public class dynRound : dynNodeWithOneOutput
    {
        public dynRound()
        {
            InPortData.Add(new PortData("dbl", "A number", new NumberType()));
            OutPortData.Add(new PortData("int", "Rounded number", new NumberType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewNumber(
                Math.Round(((FScheme.Value.Number)args[0]).Item)
                );
        }
    }

    [NodeName("Floor")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Rounds a number to the nearest smaller integer.")]
    [NodeSearchTags("round")]
    public class dynFloor : dynNodeWithOneOutput
    {
        public dynFloor()
        {
            InPortData.Add(new PortData("dbl", "A number", new NumberType()));
            OutPortData.Add(new PortData("int", "Number rounded down", new NumberType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewNumber(
                Math.Floor(((FScheme.Value.Number)args[0]).Item)
                );
        }
    }

    [NodeName("Ceiling")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Rounds a number to the nearest larger integer value.")]
    [NodeSearchTags("round")]
    public class dynCeiling : dynNodeWithOneOutput
    {
        public dynCeiling()
        {
            InPortData.Add(new PortData("dbl", "A number", new NumberType()));
            OutPortData.Add(new PortData("int", "Number rounded up", new NumberType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewNumber(
                Math.Ceiling(((FScheme.Value.Number)args[0]).Item)
                );
        }
    }

    [NodeName("Random")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Generates a uniform random number in the range [0.0, 1.0).")]
    public class dynRandom : dynNodeWithOneOutput
    {
        private static readonly Random random = new Random();

        public dynRandom()
        {
            OutPortData.Add(new PortData("rand", "Random number between 0.0 and 1.0.", new NumberType()));
            RegisterAllPorts();
        }

        public override bool RequiresRecalc
        {
            get { return true; }
            set { }
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewNumber(random.NextDouble());
        }
    }

    [NodeName("Pi")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Pi constant")]
    [NodeSearchTags("trigonometry", "circle", "π")]
    [IsInteractive(false)]
    public class dynPi : dynNodeModel
    {
        public dynPi()
        {
            OutPortData.Add(new PortData("3.14159...", "pi", new NumberType()));
            RegisterAllPorts();
        }

        public override bool RequiresRecalc
        {
            get { return false; }
            set { }
        }

        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            Dictionary<int, INode> result;
            if (!preBuilt.TryGetValue(this, out result))
            {
                result = new Dictionary<int, INode>();
                result[outPort] = new NumberNode(Math.PI);
                preBuilt[this] = result;
            }
            return result[outPort];
        }
    }

    [NodeName("Sine")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Computes the sine of the given angle.")]
    public class dynSin : dynNodeWithOneOutput
    {
        public dynSin()
        {
            InPortData.Add(new PortData("θ", "Angle in radians", new NumberType()));
            OutPortData.Add(new PortData("sin(θ)", "Sine value of the given angle", new NumberType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FScheme.Value input = args[0];

            if (input.IsList)
            {
                return FScheme.Value.NewList(
                    Utils.SequenceToFSharpList(
                        ((FScheme.Value.List)input).Item.Select(
                            x =>
                            FScheme.Value.NewNumber(Math.Sin(((FScheme.Value.Number)x).Item))
                            )
                        )
                    );
            }
            double theta = ((FScheme.Value.Number)input).Item;
            return FScheme.Value.NewNumber(Math.Sin(theta));
        }
    }

    [NodeName("Cosine")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Computes the cosine of the given angle.")]
    public class dynCos : dynNodeWithOneOutput
    {
        public dynCos()
        {
            InPortData.Add(new PortData("θ", "Angle in radians", new NumberType()));
            OutPortData.Add(new PortData("cos(θ)", "Cosine value of the given angle", new NumberType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FScheme.Value input = args[0];

            if (input.IsList)
            {
                return FScheme.Value.NewList(
                    Utils.SequenceToFSharpList(
                        ((FScheme.Value.List)input).Item.Select(
                            x =>
                            FScheme.Value.NewNumber(Math.Cos(((FScheme.Value.Number)x).Item))
                            )
                        )
                    );
            }
            double theta = ((FScheme.Value.Number)input).Item;
            return FScheme.Value.NewNumber(Math.Cos(theta));
        }
    }

    [NodeName("Tangent")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_MATH)]
    [NodeDescription("Computes the tangent of the given angle.")]
    public class dynTan : dynNodeWithOneOutput
    {
        public dynTan()
        {
            InPortData.Add(new PortData("θ", "Angle in radians", new NumberType()));
            OutPortData.Add(new PortData("tan(θ)", "Tangent value of the given angle", new NumberType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FScheme.Value input = args[0];

            if (input.IsList)
            {
                return FScheme.Value.NewList(
                    Utils.SequenceToFSharpList(
                        ((FScheme.Value.List)input).Item.Select(
                            x =>
                            FScheme.Value.NewNumber(Math.Tan(((FScheme.Value.Number)x).Item))
                            )
                        )
                    );
            }
            double theta = ((FScheme.Value.Number)input).Item;
            return FScheme.Value.NewNumber(Math.Tan(theta));
        }
    }

    #endregion

    #region Control Flow

    //TODO: Setup proper IsDirty smart execution management
    [NodeName("Perform All")]
    [NodeCategory(BuiltinNodeCategories.CORE_EVALUATE)]
    [NodeDescription("Executes Values in a sequence")]
    [NodeSearchTags("begin")]
    public class dynBegin : dynVariableInput
    {
        private IDynamoType _outputType;

        public dynBegin()
        {
            InPortData.Add(new PortData("expr1", "Expression #1", null));
            InPortData.Add(new PortData("expr2", "Expression #2", null));
            OutPortData.Add(new PortData("last", "Result of final expression", null));

            RegisterAllPorts();
        }

        protected override void InPortConnected(PortData inPort, PortData outPortSender)
        {
            if (InPortData.Last() == inPort)
                _outputType = outPortSender.PortType;
        }

        protected override IDynamoType GetOutputType(int index)
        {
            return _outputType;
        }

        protected override IDynamoType GetInputType(int index)
        {
            return new AnyType();
        }

        protected internal override void RemoveInput()
        {
            if (InPortData.Count > 2)
                base.RemoveInput();
        }

        protected override string getInputRootName()
        {
            return "expr";
        }

        protected override int getNewInputIndex()
        {
            return InPortData.Count + 1;
        }

        private static INode nestedBegins(Stack<Tuple<int, dynNodeModel>> inputs,
                                          Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt,
                                          Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            Tuple<int, dynNodeModel> popped = inputs.Pop();
            INode firstVal = popped.Item2.Build(preBuilt, popped.Item1, typeDict);

            if (inputs.Any())
            {
                var newBegin = new BeginNode(new List<string> { "expr1", "expr2" });
                newBegin.ConnectInput("expr1", nestedBegins(inputs, preBuilt, typeDict));
                newBegin.ConnectInput("expr2", firstVal);
                return newBegin;
            }
            return firstVal;
        }

        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            if (!Enumerable.Range(0, InPortData.Count).All(HasInput))
            {
                Error("All inputs must be connected.");
                throw new Exception("Begin Node requires all inputs to be connected.");
            }

            Dictionary<int, INode> result;
            if (!preBuilt.TryGetValue(this, out result))
            {
                result = new Dictionary<int, INode>();
                result[outPort] =
                    nestedBegins(
                        new Stack<Tuple<int, dynNodeModel>>(
                            Enumerable.Range(0, InPortData.Count).Select(x => Inputs[x])),
                        preBuilt,
                        typeDict);
                preBuilt[this] = result;
            }
            return result[outPort];
        }
    }

    //TODO: Setup proper IsDirty smart execution management
    [NodeName("Apply")]
    [NodeCategory(BuiltinNodeCategories.CORE_EVALUATE)]
    [NodeDescription("Applies arguments to a function")]
    public class dynApply1 : dynVariableInput
    {
        private readonly PolymorphicType _outType = new PolymorphicType();

        private Dictionary<int, IDynamoType> _inTypes = new Dictionary<int, IDynamoType>(); 

        public dynApply1()
        {
            InPortData.Add(new PortData("func", "Procedure", null));
            OutPortData.Add(new PortData("result", "Result", _outType));

            RegisterAllPorts();
        }

        protected override IDynamoType GetInputType(int index)
        {
            if (index == 0)
                return new FunctionType(Enumerable.Range(1, InPortData.Count - 1).Select(GetInputType), _outType);

            if (!_inTypes.ContainsKey(index))
                _inTypes[index] = new PolymorphicType();

            return _inTypes[index];
        }

        protected override string getInputRootName()
        {
            return "arg";
        }

        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            if (!Enumerable.Range(0, InPortData.Count).All(HasInput))
            {
                Error("All inputs must be connected.");
                throw new Exception("Apply Node requires all inputs to be connected.");
            }
            return base.Build(preBuilt, outPort, typeDict);
        }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return new ApplierNode(portNames);
        }

        protected internal override void RemoveInput()
        {
            if (InPortData.Count > 1)
                base.RemoveInput();
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            //Debug.WriteLine(pd.Object.GetType().ToString());
            foreach (PortData inport in InPortData.Skip(1))
            {
                XmlElement input = xmlDoc.CreateElement("Input");

                input.SetAttribute("name", inport.NickName);

                dynEl.AppendChild(input);
            }
        }

        public override void LoadElement(XmlNode elNode)
        {
            var query = from XmlNode subNode in elNode.ChildNodes
                        where subNode.Name == "Input"
                        let attr = subNode.Attributes["name"].Value
                        where !attr.Equals("func")
                        select subNode;
            foreach (XmlNode subNode in query)
            {
                InPortData.Add(new PortData(subNode.Attributes["name"].Value, "", new PolymorphicType()));
            }
            RegisterAllPorts();
        }
    }

    //TODO: Setup proper IsDirty smart execution management
    [NodeName("If")]
    [NodeCategory(BuiltinNodeCategories.LOGIC_CONDITIONAL)]
    [NodeDescription("Conditional statement")]
    public class dynConditional : dynNodeModel
    {
        public dynConditional()
        {
            var a = new PolymorphicType();
            //TypeVar b = new PolymorphicType();

            InPortData.Add(new PortData("test", "Test block", new NumberType()));
            InPortData.Add(new PortData("true", "True block", a));
            InPortData.Add(new PortData("false", "False block", a));
            OutPortData.Add(new PortData("result", "Result", a));

            RegisterAllPorts();
        }

        internal override IDynamoType TypeCheck(int port, FSharpMap<string, TypeScheme> env, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            if (!Enumerable.Range(0, InPortData.Count).All(HasInput))
            {
                Error("All inputs must be connected.");
                throw new Exception("If Node requires all inputs to be connected.");
            }
            return base.TypeCheck(port, env, typeDict);
        }

        protected internal override INode Build(Dictionary<dynNodeModel, Dictionary<int, INode>> preBuilt, int outPort, Dictionary<dynNodeModel, NodeTypeInformation> typeDict)
        {
            if (!Enumerable.Range(0, InPortData.Count).All(HasInput))
            {
                Error("All inputs must be connected.");
                throw new Exception("If Node requires all inputs to be connected.");
            }
            return base.Build(preBuilt, outPort, typeDict);
        }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return new ConditionalNode(portNames);
        }
    }

    [NodeName("Debug Breakpoint")]
    [NodeCategory(BuiltinNodeCategories.CORE_EVALUATE)]
    [NodeDescription("Halts execution until user clicks button.")]
    public class dynBreakpoint : dynNodeWithOneOutput
    {
        private bool _enabled;
        private Button _button;

        public dynBreakpoint()
        {
            var pType = new PolymorphicType();

            InPortData.Add(new PortData("", "Object to inspect", pType));
            OutPortData.Add(new PortData("", "Object inspected", pType));
            RegisterAllPorts();
        }

        private bool enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _button.IsEnabled = value;
            }
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add a text box to the input grid of the control
            _button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Content = "Continue"
            };

            //inputGrid.RowDefinitions.Add(new RowDefinition());
            nodeUI.inputGrid.Children.Add(_button);
            Grid.SetColumn(_button, 0);
            Grid.SetRow(_button, 0);

            enabled = false;

            _button.Click += button_Click;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Deselect();
            enabled = false;
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FScheme.Value result = args[0];

            Controller.DynamoViewModel.Log(FScheme.print(result));

            if (Controller.DynamoViewModel.RunInDebug)
            {
                enabled = true;
                Select();
                Controller.DynamoViewModel.ShowElement(this);

                while (enabled)
                    Thread.Sleep(1);
            }

            return result;
        }
    }

    #endregion

    #region Interactive Primitive Types

    #region Base Classes

    internal class dynTextBox : TextBox
    {
        private static readonly Brush clear = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        private bool _numeric;

        private bool _pending;

        public dynTextBox()
        {
            //turn off the border
            Background = clear;
            BorderThickness = new Thickness(0);
        }

        public bool IsNumeric
        {
            get { return _numeric; }
            set
            {
                _numeric = value;
                if (value && Text.Length > 0)
                {
                    Text = dynSettings.RemoveChars(
                        Text,
                        Text.ToCharArray()
                            .Where(c => !char.IsDigit(c) && c != '-' && c != '.')
                            .Select(c => c.ToString())
                        );
                }
            }
        }

        public bool Pending
        {
            get { return _pending; }
            set
            {
                if (value)
                {
                    FontStyle = FontStyles.Italic;
                    FontWeight = FontWeights.Bold;
                }
                else
                {
                    FontStyle = FontStyles.Normal;
                    FontWeight = FontWeights.Normal;
                }
                _pending = value;
            }
        }

        public new string Text
        {
            get { return base.Text; }
            set
            {
                //base.Text = value;
                commit();
            }
        }

        public event Action OnChangeCommitted;

        private void commit()
        {
            BindingExpression expr = GetBindingExpression(TextProperty);
            if (expr != null)
                expr.UpdateSource();

            if (OnChangeCommitted != null)
                OnChangeCommitted();
            Pending = false;

            //dynSettings.Bench.mainGrid.Focus();
        }

        private bool shouldCommit()
        {
            return !dynSettings.Controller.DynamoViewModel.DynamicRunEnabled;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            Pending = true;

            if (IsNumeric)
            {
                int p = CaretIndex;

                base.Text = dynSettings.RemoveChars(
                    Text,
                    Text.ToCharArray()
                        .Where(c => !char.IsDigit(c) && c != '-' && c != '.')
                        .Select(c => c.ToString())
                    );

                CaretIndex = p;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                commit();
                dynSettings.ReturnFocusToSearch();
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            commit();
        }
    }

    [IsInteractive(true)]
    public abstract class dynBasicInteractive<T> : dynNodeWithOneOutput
    {
        private T _value;

        public dynBasicInteractive()
        {
            Type type = typeof(T);
        }

        public virtual T Value
        {
            get { return _value; }
            set
            {
                if (_value == null || !_value.Equals(value))
                {
                    _value = value;
                    RequiresRecalc = value != null;
                    RaisePropertyChanged("Value");
                }
            }
        }

        protected abstract T DeserializeValue(string val);

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add an edit window option to the 
            //main context window
            var editWindowItem = new MenuItem
            {
                Header = "Edit...",
                IsCheckable = false
            };

            nodeUI.MainContextMenu.Items.Add(editWindowItem);

            editWindowItem.Click += editWindowItem_Click;
        }

        public virtual void editWindowItem_Click(object sender, RoutedEventArgs e)
        {
            //override in child classes
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            //Debug.WriteLine(pd.Object.GetType().ToString());
            XmlElement outEl = xmlDoc.CreateElement(typeof(T).FullName);
            outEl.SetAttribute("value", Value.ToString());
            dynEl.AppendChild(outEl);
        }

        public override void LoadElement(XmlNode elNode)
        {
            var query = elNode.ChildNodes
                .Cast<XmlNode>()
                .Where(subNode => subNode.Name.Equals(typeof(T).FullName));
            foreach (XmlNode subNode in query)
            {
                Value = DeserializeValue(subNode.Attributes[0].Value);
            }
        }

        public override string PrintExpression()
        {
            return Value.ToString();
        }
    }

    public abstract class dynDouble : dynBasicInteractive<double>
    {
        public dynDouble()
        {
            OutPortData.Add(new PortData("", "", new NumberType()));
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewNumber(Value);
        }

        public override void editWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new dynEditWindow { editText = { Text = base.Value.ToString() } };

            //set the text of the edit window to begin

            if (editWindow.ShowDialog() != true)
                return;

            //set the value from the text in the box
            Value = DeserializeValue(editWindow.editText.Text);
        }
    }

    public abstract class dynBool : dynBasicInteractive<bool>
    {
        public dynBool()
        {
            OutPortData.Add(new PortData("", "", new NumberType()));
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewNumber(Value ? 1 : 0);
        }
    }

    public abstract class dynString : dynBasicInteractive<string>
    {
        public dynString()
        {
            OutPortData.Add(new PortData("", "", new StringType()));
        }

        public override string Value
        {
            get { return base.Value; }
            set { base.Value = EscapeString(value); }
        }

        // Taken from:
        // http://stackoverflow.com/questions/6378681/how-can-i-use-net-style-escape-sequences-in-runtime-values
        private static string EscapeString(string s)
        {
            if (s == null)
                return "";

            Contract.Requires(s != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    i++;
                    if (i == s.Length)
                        throw new ArgumentException("Escape sequence starting at end of string", s);
                    switch (s[i])
                    {
                        case '\\':
                            sb.Append('\\');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                            //TODO: ADD MORE CASES HERE
                    }
                }
                else
                    sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return FScheme.Value.NewString(Value);
        }

        public override string PrintExpression()
        {
            return "\"" + base.PrintExpression() + "\"";
        }

        public override void editWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new dynEditWindow { editText = { Text = base.Value } };

            //set the text of the edit window to begin

            if (editWindow.ShowDialog() != true)
                return;

            //set the value from the text in the box
            Value = DeserializeValue(editWindow.editText.Text);
        }
    }

    #endregion

    [NodeName("Number")]
    [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
    [NodeDescription("Creates a number.")]
    public class dynDoubleInput : dynDouble
    {
        public dynDoubleInput()
        {
            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add a text box to the input grid of the control
            var tb = new dynTextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                IsNumeric = true,
                Background = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
                DataContext = this
            };

            var bindingVal = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                Converter = new DoubleDisplay(),
                NotifyOnValidationError = false,
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            tb.SetBinding(TextBox.TextProperty, bindingVal);

            tb.Text = "0.0";

            nodeUI.inputGrid.Children.Add(tb);
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, 0);
        }

        protected override double DeserializeValue(string val)
        {
            try
            {
                return Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }
    }

    [NodeName("Number Slider")]
    [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
    [NodeDescription("Change a number value with a slider.")]
    public class dynDoubleSliderInput : dynDouble
    {
        private TextBox displayBox;
        private double max;
        private dynTextBox maxtb;
        private double min;
        private dynTextBox mintb;
        private Slider tb_slider;

        public dynDoubleSliderInput()
        {
            RegisterAllPorts();
            Value = 50.0;
            Min = 0.0;
            Max = 100.0;
        }

        public override double Value
        {
            get { return base.Value; }
            set
            {
                Debug.WriteLine("Setting Value...");
                base.Value = value;
                RaisePropertyChanged("Value");
            }
        }

        public double Max
        {
            get { return max; }
            set
            {
                Debug.WriteLine("Setting Max...");
                max = value;
                RaisePropertyChanged("Max");
            }
        }

        public double Min
        {
            get { return min; }
            set
            {
                Debug.WriteLine("Setting Min...");
                min = value;
                RaisePropertyChanged("Min");
            }
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add a slider control to the input grid of the control
            tb_slider = new Slider
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 100,
                Ticks = new DoubleCollection(10),
                TickPlacement = TickPlacement.BottomRight
            };

            tb_slider.ValueChanged += delegate
            {
                Point pos = Mouse.GetPosition(nodeUI.elementCanvas);
                Canvas.SetLeft(displayBox, pos.X);
                Canvas.SetTop(displayBox, Height);
            };

            tb_slider.PreviewMouseDown += delegate
            {
                if (nodeUI.IsEnabled && !nodeUI.elementCanvas.Children.Contains(displayBox))
                {
                    nodeUI.elementCanvas.Children.Add(displayBox);
                    Point pos = Mouse.GetPosition(nodeUI.elementCanvas);
                    Canvas.SetLeft(displayBox, pos.X);
                }
            };

            tb_slider.PreviewMouseUp += delegate
            {
                if (nodeUI.elementCanvas.Children.Contains(displayBox))
                    nodeUI.elementCanvas.Children.Remove(displayBox);

                dynSettings.ReturnFocusToSearch();
            };

            nodeUI.inputGrid.Children.Add(tb_slider);
            Grid.SetColumn(tb_slider, 1);
            Grid.SetRow(tb_slider, 0);

            mintb = new dynTextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = double.NaN,
                IsNumeric = true,
                Background = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF))
            };

            mintb.OnChangeCommitted += delegate
            {
                try
                {
                    Min = Convert.ToDouble(mintb.Text);
                }
                catch
                {
                    Min = 0;
                }
            };
            //mintb.Pending = false;

            maxtb = new dynTextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = double.NaN,
                IsNumeric = true,
                Background = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF))
            };

            maxtb.OnChangeCommitted += delegate
            {
                try
                {
                    Max = Convert.ToDouble(maxtb.Text);
                }
                catch
                {
                    Max = 100;
                }
            };

            nodeUI.inputGrid.ColumnDefinitions.Add(new ColumnDefinition());
            nodeUI.inputGrid.ColumnDefinitions.Add(new ColumnDefinition());
            nodeUI.inputGrid.ColumnDefinitions.Add(new ColumnDefinition());

            nodeUI.inputGrid.Children.Add(mintb);
            nodeUI.inputGrid.Children.Add(maxtb);

            Grid.SetColumn(mintb, 0);
            Grid.SetColumn(maxtb, 2);

            displayBox = new TextBox
            {
                IsReadOnly = true,
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            Canvas.SetTop(displayBox, nodeUI.Height);
            Panel.SetZIndex(displayBox, int.MaxValue);

            displayBox.DataContext = this;
            maxtb.DataContext = this;
            tb_slider.DataContext = this;
            mintb.DataContext = this;

            var bindingValue = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                Converter = new StringDisplay(),
            };
            displayBox.SetBinding(TextBox.TextProperty, bindingValue);

            var sliderBinding = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            tb_slider.SetBinding(RangeBase.ValueProperty, sliderBinding);

            var bindingMax = new Binding("Max")
            {
                Mode = BindingMode.TwoWay,
                Converter = new DoubleDisplay(),
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            tb_slider.SetBinding(RangeBase.MaximumProperty, bindingMax);
            maxtb.SetBinding(TextBox.TextProperty, bindingMax);

            var bindingMin = new Binding("Min")
            {
                Mode = BindingMode.TwoWay,
                Converter = new DoubleDisplay(),
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            tb_slider.SetBinding(RangeBase.MinimumProperty, bindingMin);
            mintb.SetBinding(TextBox.TextProperty, bindingMin);
        }

        protected override double DeserializeValue(string val)
        {
            try
            {
                return Convert.ToDouble(val);
            }
            catch
            {
                return 0;
            }
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            XmlElement outEl = xmlDoc.CreateElement(typeof(double).FullName);
            outEl.SetAttribute("value", Value.ToString());
            outEl.SetAttribute("min", tb_slider.Minimum.ToString());
            outEl.SetAttribute("max", tb_slider.Maximum.ToString());
            dynEl.AppendChild(outEl);
        }

        public override void LoadElement(XmlNode elNode)
        {
            var query = from XmlNode subNode in elNode.ChildNodes
                        where subNode.Name.Equals(typeof(double).FullName)
                        from XmlAttribute attr in subNode.Attributes
                        select attr;
            foreach (XmlAttribute attr in query)
            {
                if (attr.Name.Equals("value"))
                    Value = DeserializeValue(attr.Value);
                else if (attr.Name.Equals("min"))
                {
                    //tb_slider.Minimum = Convert.ToDouble(attr.Value);
                    //mintb.Text = attr.Value;
                    Min = Convert.ToDouble(attr.Value);
                }
                else if (attr.Name.Equals("max"))
                {
                    //tb_slider.Maximum = Convert.ToDouble(attr.Value);
                    //maxtb.Text = attr.Value;
                    Max = Convert.ToDouble(attr.Value);
                }
            }
        }
    }

    [NodeName("Boolean")]
    [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
    [NodeDescription("Selection between a true and false.")]
    [NodeSearchTags("true", "truth", "false")]
    public class dynBoolSelector : dynBool
    {
        private RadioButton _rbFalse;
        private RadioButton _rbTrue;

        public dynBoolSelector()
        {
            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add a text box to the input grid of the control
            _rbTrue = new RadioButton();
            _rbFalse = new RadioButton();
            _rbTrue.VerticalAlignment = VerticalAlignment.Center;
            _rbFalse.VerticalAlignment = VerticalAlignment.Center;

            //use a unique name for the button group
            //so other instances of this element don't get confused
            string groupName = Guid.NewGuid().ToString();
            _rbTrue.GroupName = groupName;
            _rbFalse.GroupName = groupName;

            _rbTrue.Content = "1";
            _rbFalse.Content = "0";

            var rd = new RowDefinition();
            var cd1 = new ColumnDefinition();
            var cd2 = new ColumnDefinition();
            nodeUI.inputGrid.ColumnDefinitions.Add(cd1);
            nodeUI.inputGrid.ColumnDefinitions.Add(cd2);
            nodeUI.inputGrid.RowDefinitions.Add(rd);

            nodeUI.inputGrid.Children.Add(_rbTrue);
            nodeUI.inputGrid.Children.Add(_rbFalse);

            Grid.SetColumn(_rbTrue, 0);
            Grid.SetRow(_rbTrue, 0);
            Grid.SetColumn(_rbFalse, 1);
            Grid.SetRow(_rbFalse, 0);

            //rbFalse.IsChecked = true;
            _rbTrue.Checked += rbTrue_Checked;
            _rbFalse.Checked += rbFalse_Checked;

            _rbFalse.DataContext = this;
            _rbTrue.DataContext = this;

            var rbTrueBinding = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
            };
            _rbTrue.SetBinding(ToggleButton.IsCheckedProperty, rbTrueBinding);

            var rbFalseBinding = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                Converter = new InverseBoolDisplay()
            };
            _rbFalse.SetBinding(ToggleButton.IsCheckedProperty, rbFalseBinding);
        }

        protected override bool DeserializeValue(string val)
        {
            try
            {
                return val.ToLower().Equals("true");
            }
            catch
            {
                return false;
            }
        }

        private void rbFalse_Checked(object sender, RoutedEventArgs e)
        {
            //Value = false;
            dynSettings.ReturnFocusToSearch();
        }

        private void rbTrue_Checked(object sender, RoutedEventArgs e)
        {
            //Value = true;
            dynSettings.ReturnFocusToSearch();
        }
    }

    [NodeName("String")]
    [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
    [NodeDescription("Creates a string.")]
    public class dynStringInput : dynString
    {
        private dynTextBox tb;
        //TextBlock tb;

        public dynStringInput()
        {
            RegisterAllPorts();
            Value = "";
        }

        public override string Value
        {
            get { return base.Value; }
            set
            {
                if (base.Value == value)
                    return;

                base.Value = value;
            }
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add a text box to the input grid of the control
            tb = new dynTextBox();

            nodeUI.inputGrid.Children.Add(tb);
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, 0);

            tb.DataContext = this;
            var bindingVal = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                //Converter = new StringDisplay(),
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit
            };
            tb.SetBinding(TextBox.TextProperty, bindingVal);
        }

        protected override string DeserializeValue(string val)
        {
            return val;
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            XmlElement outEl = xmlDoc.CreateElement(typeof(string).FullName);
            outEl.SetAttribute("value", HttpUtility.UrlEncode(Value));
            dynEl.AppendChild(outEl);
        }

        public override void LoadElement(XmlNode elNode)
        {
            var query = from XmlNode subNode in elNode.ChildNodes
                        where subNode.Name.Equals(typeof(string).FullName)
                        from XmlAttribute attr in subNode.Attributes
                        where attr.Name.Equals("value")
                        select attr;
            foreach (XmlAttribute attr in query) 
            {
                Value = DeserializeValue(HttpUtility.UrlDecode(attr.Value));
                //tb.Text = Utilities.Ellipsis(Value, 30);
            }
        }
    }

    [NodeName("Filename")]
    [NodeCategory(BuiltinNodeCategories.CORE_PRIMITIVES)]
    [NodeDescription("Allows you to select a file on the system to get its filename.")]
    public class dynStringFilename : dynBasicInteractive<string>
    {
        private TextBox tb;

        public dynStringFilename()
        {
            OutPortData.Add(new PortData("", "", new StringType()));

            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //add a button to the inputGrid on the dynElement
            var readFileButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Content = "Browse..."
            };
            //readFileButton.Margin = new System.Windows.Thickness(0, 0, 0, 0);
            readFileButton.Click += readFileButton_Click;

            tb = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false
            };

            tb.TextChanged += delegate
            {
                tb.ScrollToHorizontalOffset(double.PositiveInfinity);
                dynSettings.ReturnFocusToSearch();
            };

            //NodeUI.SetRowAmount(2);
            nodeUI.inputGrid.RowDefinitions.Add(new RowDefinition());
            nodeUI.inputGrid.RowDefinitions.Add(new RowDefinition());

            nodeUI.inputGrid.Children.Add(tb);
            nodeUI.inputGrid.Children.Add(readFileButton);

            Grid.SetRow(readFileButton, 0);
            Grid.SetRow(tb, 1);

            //NodeUI.topControl.Height = 60;
            //NodeUI.UpdateLayout();

            tb.DataContext = this;
            var bindingVal = new Binding("Value")
            {
                Mode = BindingMode.TwoWay,
                Converter = new FilePathDisplay()
            };
            tb.SetBinding(TextBox.TextProperty, bindingVal);

            if (string.IsNullOrEmpty(Value))
                Value = "No file selected.";
        }

        protected override string DeserializeValue(string val)
        {
            return File.Exists(val) ? val : "";
        }

        private void readFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();

            if (openDialog.ShowDialog() == DialogResult.OK)
                Value = openDialog.FileName;
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            if (string.IsNullOrEmpty(Value))
                throw new Exception("No file selected.");

            return FScheme.Value.NewString(Value);
        }

        public override string PrintExpression()
        {
            return "\"" + base.PrintExpression() + "\"";
        }
    }

    #endregion

    #region Strings and Conversions

    [NodeName("Concat Strings")]
    [NodeDescription("Concatenates two or more strings")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynConcatStrings : dynVariableInput
    {
        public dynConcatStrings()
        {
            InPortData.Add(new PortData("s1", "First string", new StringType()));
            InPortData.Add(new PortData("s2", "Second string", new StringType()));
            OutPortData.Add(new PortData("combined", "Combined lists", new StringType()));

            RegisterAllPorts();
        }

        protected override IDynamoType GetInputType(int index)
        {
            return new StringType();
        }

        protected override string getInputRootName()
        {
            return "s";
        }

        protected override int getNewInputIndex()
        {
            return InPortData.Count + 1;
        }

        protected internal override void RemoveInput()
        {
            if (InPortData.Count > 2)
                base.RemoveInput();
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            //Debug.WriteLine(pd.Object.GetType().ToString());
            foreach (PortData inport in InPortData.Skip(2))
            {
                XmlElement input = xmlDoc.CreateElement("Input");
                input.SetAttribute("name", inport.NickName);
                dynEl.AppendChild(input);
            }
        }

        public override void LoadElement(XmlNode elNode)
        {
            foreach (XmlNode subNode in elNode.ChildNodes.Cast<XmlNode>().Where(subNode => subNode.Name == "Input"))
                InPortData.Add(new PortData(subNode.Attributes["name"].Value, "", new StringType()));
            RegisterAllPorts();
        }

        protected override InputNode Compile(IEnumerable<string> portNames)
        {
            return SaveResult
                       ? base.Compile(portNames)
                       : new FunctionNode("concat-strings", portNames);
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            return ((FScheme.Value.Function)Controller.FSchemeEnvironment.LookupSymbol("concat-strings"))
                .Item.Invoke(args);
        }
    }

    [NodeName("String to Number")]
    [NodeDescription("Converts a string to a number")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynString2Num : dynBuiltinFunction
    {
        public dynString2Num()
            : base("string->num")
        {
            InPortData.Add(new PortData("s", "A string", new StringType()));
            OutPortData.Add(new PortData("n", "A number", new NumberType()));

            RegisterAllPorts();
        }
    }

    [NodeName("Number to String")]
    [NodeDescription("Converts a number to a string")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynNum2String : dynBuiltinFunction
    {
        public dynNum2String()
            : base("num->string")
        {
            InPortData.Add(new PortData("n", "A number", new NumberType()));
            OutPortData.Add(new PortData("s", "A string", new StringType()));
            RegisterAllPorts();
        }
    }

    [NodeName("String Length")]
    [NodeDescription("Calculates the length of a string.")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynStringLen : dynNodeWithOneOutput
    {
        public dynStringLen()
        {
            InPortData.Add(new PortData("s", "A string", new StringType()));
            OutPortData.Add(new PortData("len(s)", "Length of given string", new NumberType()));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            return Value.NewNumber(((Value.String)args[0]).Item.Length);
        }
    }

    [NodeName("Split String")]
    [NodeDescription("Splits given string around given delimiter into a list of sub strings.")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynSplitString : dynNodeWithOneOutput
    {
        public dynSplitString()
        {
            InPortData.Add(new PortData("str", "String to split", new StringType()));
            InPortData.Add(new PortData("del", "Delimiter", new StringType()));
            OutPortData.Add(new PortData("strs", "List of split strings", new ListType(new StringType())));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            string str = ((FScheme.Value.String)args[0]).Item;
            string del = ((FScheme.Value.String)args[1]).Item;

            return FScheme.Value.NewList(
                Utils.SequenceToFSharpList(
                    str.Split(new[] { del }, StringSplitOptions.None)
                       .Select(FScheme.Value.NewString)
                    )
                );
        }
    }

    [NodeName("Join Strings")]
    [NodeDescription("Joins the given list of strings around the given delimiter.")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynJoinStrings : dynNodeWithOneOutput
    {
        public dynJoinStrings()
        {
            InPortData.Add(new PortData("strs", "List of strings to join.", new ListType(new StringType())));
            InPortData.Add(new PortData("del", "Delimier", new StringType()));
            OutPortData.Add(new PortData("str", "Joined string", new StringType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            FSharpList<FScheme.Value> strs = ((FScheme.Value.List)args[0]).Item;
            string del = ((FScheme.Value.String)args[1]).Item;

            return FScheme.Value.NewString(
                string.Join(del, strs.Select(x => ((FScheme.Value.String)x).Item))
                );
        }
    }

    [NodeName("String Case")]
    [NodeDescription("Converts a string to uppercase or lowercase")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynStringCase : dynNodeWithOneOutput
    {
        public dynStringCase()
        {
            InPortData.Add(new PortData("str", "String to convert", new StringType()));
            InPortData.Add(new PortData("upper?", "True = Uppercase, False = Lowercase", new NumberType()));
            OutPortData.Add(new PortData("s", "Converted string", new StringType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            string s = ((FScheme.Value.String)args[0]).Item;
            bool upper = ((FScheme.Value.Number)args[1]).Item == 1.0;

            return FScheme.Value.NewString(
                upper ? s.ToUpper() : s.ToLower()
                );
        }
    }

    [NodeName("Substring")]
    [NodeDescription("Gets a substring of a given string")]
    [NodeCategory(BuiltinNodeCategories.CORE_STRINGS)]
    public class dynSubstring : dynNodeWithOneOutput
    {
        public dynSubstring()
        {
            InPortData.Add(new PortData("str", "String to take substring from", new StringType()));
            InPortData.Add(new PortData("start", "Starting index of substring", new NumberType()));
            InPortData.Add(new PortData("length", "Length of substring", new NumberType()));
            OutPortData.Add(new PortData("sub", "Substring", new StringType()));

            RegisterAllPorts();
        }

        public override FScheme.Value Evaluate(FSharpList<FScheme.Value> args)
        {
            string s = ((FScheme.Value.String)args[0]).Item;
            double start = ((FScheme.Value.Number)args[1]).Item;
            double length = ((FScheme.Value.Number)args[2]).Item;

            return FScheme.Value.NewString(s.Substring((int)start, (int)length));
        }
    }

    #endregion

    #region Value Conversion

    [ValueConversion(typeof(double), typeof(String))]
    public class DoubleDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? "" : ((double)value).ToString("F4");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }

    public class StringDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? "" : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FilePathDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value.ToString())
                       ? "No file selected."
                       : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InverseBoolDisplay : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return !(bool)value;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return !(bool)value;
            return value;
        }
    }

    #endregion
}
