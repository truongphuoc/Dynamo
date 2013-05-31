using System;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

using Dynamo.Controls;
using Dynamo.Connectors;
using Dynamo.Nodes.TypeSystem;
using Microsoft.FSharp.Collections;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    [IsInteractive(true)]
    public abstract class dynEnum : dynNodeWithOneOutput
    {
        ComboBox _combo;

        public dynEnum()
        {
            OutPortData.Add(new PortData("", "Enum", new ObjectType(typeof(object))));

            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(dynNodeView nodeUI)
        {
            //widen the control
            nodeUI.topControl.Width = 300;

            //add a drop down list to the window
            _combo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            nodeUI.inputGrid.Children.Add(_combo);

            Grid.SetColumn(_combo, 0);
            Grid.SetRow(_combo, 0);

            _combo.SelectionChanged += delegate
            {
                if (_combo.SelectedIndex != -1)
                    RequiresRecalc = true;
            };
        }

        public void WireToEnum(Array arr)
        {
            _combo.ItemsSource = arr;
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            if (_combo.SelectedItem != null)
            {
                return Value.NewContainer(_combo.SelectedItem);
            }
            throw new Exception("There is nothing selected.");
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            dynEl.SetAttribute("index", _combo.SelectedIndex.ToString());
        }

        public override void LoadElement(XmlNode elNode)
        {
            try
            {
                _combo.SelectedIndex = Convert.ToInt32(elNode.Attributes["index"].Value);
            }
            catch { }
        }
    }

}
