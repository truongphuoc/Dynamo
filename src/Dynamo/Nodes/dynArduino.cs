//Copyright © Autodesk, Inc. 2012. All rights reserved.
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Xml;
using System.IO.Ports;
using Dynamo.TypeSystem;
using Microsoft.FSharp.Collections;

using Dynamo.Connectors;
using Dynamo.FSchemeInterop;
using Value = Dynamo.FScheme.Value;


namespace Dynamo.Nodes
{
    [NodeName("Arduino")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Manages connection to an Arduino microcontroller.")]
    public class dynArduino : dynNodeWithOneOutput
    {
        SerialPort _port;
        System.Windows.Controls.MenuItem _comItem;

        public dynArduino()
        {
            InPortData.Add(new PortData("exec", "Execution Interval", new NumberType()));
            OutPortData.Add(new PortData("arduino", "Serial port for later read/write", new ObjectType(typeof(SerialPort))));

            RegisterAllPorts();

            if (_port == null)
            {
                _port = new SerialPort();
            }
            _port.BaudRate = 9600;
            _port.NewLine = "\r\n";
            _port.DtrEnable = true;

        }

        public override void SetupCustomUIElements(Controls.dynNodeView nodeUI)
        {
            string[] serialPortNames = SerialPort.GetPortNames();

            foreach (string portName in serialPortNames)
            {

                if (_lastComItem != null)
                {
                    _lastComItem.IsChecked = false; // uncheck last checked item
                }
                _comItem = new MenuItem
                {
                    Header = portName,
                    IsCheckable = true,
                    IsChecked = true
                };
                _comItem.Checked += comItem_Checked;
                nodeUI.MainContextMenu.Items.Add(_comItem);

                _port.PortName = portName;
                _lastComItem = _comItem;
            }
        }

        MenuItem _lastComItem;
        
        void comItem_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            var comItem = e.Source as MenuItem;

            if (_lastComItem != null)
            {
                _lastComItem.IsChecked = false; // uncheck last checked item
            }

            if (_port != null)
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            _port.PortName = comItem.Header.ToString();
            comItem.IsChecked = true;
            _lastComItem = comItem;
            
        }

        public override void Cleanup()
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            _port = null;
        }

        public override void SaveElement(XmlDocument xmlDoc, XmlElement dynEl)
        {
            //Debug.WriteLine(pd.Object.GetType().ToString());
            XmlElement outEl = xmlDoc.CreateElement(typeof(double).FullName);
            outEl.SetAttribute("value", _port.PortName);
            dynEl.AppendChild(outEl);
        }

        public override void LoadElement(XmlNode elNode)
        {
            foreach (XmlNode subNode in elNode.ChildNodes)
            {
                if (subNode.Name == typeof(double).FullName)
                {
                    _port.PortName = subNode.Attributes[0].Value;
                }
            }
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            if (((Value.Number)args[0]).Item == 1)
            {
                if (_port != null)
                {
                    if (!_port.IsOpen)
                    {
                        _port.Open();
                    }
                }
            }

            return Value.NewContainer(_port); // pass the port downstream
        }


    }

    [NodeName("Read Arduino")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Reads values from an Arduino microcontroller.")]
    public class dynArduinoRead : dynNodeWithOneOutput
    {
        SerialPort _port;
        int _range;
        List<string> _serialLine = new List<string>();


        public dynArduinoRead()
        {
            InPortData.Add(new PortData("arduino", "Arduino serial connection", new ObjectType(typeof(SerialPort))));
            InPortData.Add(new PortData("range", "Number of lines to read", new NumberType()));
            OutPortData.Add(new PortData("output", "Serial output line", new ListType(new StringType())));

            RegisterAllPorts();
        }

        private List<string> GetArduinoData()
        {
            string data = _port.ReadExisting();
            var serialRange = new List<string>();

            string[] allData = data.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (allData.Length > 2)
            {
                //get the sensor values, tailing the values list and passing back a range from [range to count-2]
                //int end = allData.Length - 2; 
                try
                {
                    serialRange = allData.ToList().GetRange(0, _range);
                    return serialRange;
                }
                catch
                {
                    return serialRange;
                }

            }

            return serialRange;

        }


        public override Value Evaluate(FSharpList<Value> args)
        {
            _port = (SerialPort)((Value.Container)args[0]).Item;
            _range = (int)((Value.Number)args[1]).Item;
            

            if (_port != null)
            {
                if (!_port.IsOpen)
                {
                    _port.Open();
                }

                //get the values from the serial port as a list of strings
                _serialLine = GetArduinoData();
            }


            return Value.NewList(Utils.SequenceToFSharpList(_serialLine.Select(Value.NewString)));
        }


    }

    [NodeName("Write Arduino")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Writes values to an Arduino microcontroller.")]
    public class dynArduinoWrite : dynNodeWithOneOutput
    {
        SerialPort _port;

        public dynArduinoWrite()
        {
            InPortData.Add(new PortData("arduino", "Arduino serial connection", new ObjectType(typeof(SerialPort))));
            InPortData.Add(new PortData("text", "Text to be written", new StringType()));
            OutPortData.Add(new PortData("success?", "Whether or not the operation was successful.", new NumberType()));

            RegisterAllPorts();
        }

        private void WriteDataToArduino(string dataLine)
        {

            dataLine = dataLine + "\r\n"; //termination
            _port.WriteLine(dataLine);

        }

        public override Value Evaluate(FSharpList<Value> args)
        {

            _port = (SerialPort)((Value.Container)args[0]).Item;
            string dataToWrite = ((Value.String)args[1]).Item;// ((Value.Container)args[1]).Item;

            if (_port != null)
            {
                if (!_port.IsOpen)
                {
                    _port.Open();
                }

                //write data to the serial port
                WriteDataToArduino(dataToWrite);
            }
            

            return Value.NewNumber(1);// catch failures here 
        }
    }
}
