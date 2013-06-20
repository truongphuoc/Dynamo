using System;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

using Dynamo.Controls;
using Dynamo.Connectors;
using Dynamo.TypeSystem;
using Microsoft.FSharp.Collections;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    [IsInteractive(true)]
    public abstract class dynEnum : dynNodeWithOneOutput
    {
        public int SelectedIndex { get; set; }
        public Array Items { get; set; }

        protected dynEnum()
        {
            Items = new[] { "" };
            SelectedIndex = 0;
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            var comboBox = new ComboBox
                {
                    MinWidth = 150,
                    Padding = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };

            nodeUI.inputGrid.Children.Add(comboBox);

            Grid.SetColumn(comboBox, 0);
            Grid.SetRow(comboBox, 0);

            comboBox.ItemsSource = this.Items;
            comboBox.SelectedIndex = this.SelectedIndex;

            comboBox.SelectionChanged += delegate
            {
                if (comboBox.SelectedIndex == -1) return;
                this.RequiresRecalc = true;
                this.SelectedIndex = comboBox.SelectedIndex;
            };
        }

        public void WireToEnum(Array arr)
        {
            Items = arr;
        }

        public override void SaveNode(XmlDocument xmlDoc, XmlElement dynEl, SaveContext context)
        {
            dynEl.SetAttribute("index", this.SelectedIndex.ToString());
        }

        public override void LoadNode(XmlNode elNode)
        {
            try
            {
                this.SelectedIndex = Convert.ToInt32(elNode.Attributes["index"].Value);
            }
            catch { }
        }
    }

    [IsInteractive(true)]
    public abstract class dynEnumAsInt : dynEnum
    {
        protected dynEnumAsInt()
        {
            OutPortData.Add(new PortData("Int", "The index of the enum", new NumberType()));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            if (this.SelectedIndex < this.Items.Length)
            {
                var value = Value.NewNumber(this.SelectedIndex);
                return value;
            }
            throw new Exception("There is nothing selected.");
        }

    }

    [IsInteractive(true)]
    public abstract class dynEnumAsString : dynEnum
    {
        protected dynEnumAsString()
        {
            OutPortData.Add(new PortData("String", "The enum as a string", new StringType()));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            if (this.SelectedIndex < this.Items.Length)
            {
                var value = Value.NewString( Items.GetValue(this.SelectedIndex).ToString() );
                return value;
            }
            else
            {
                throw new Exception("There is nothing selected.");
            }
        }

    }

}
