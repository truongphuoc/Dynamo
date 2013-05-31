//Copyright 2013 Ian Keough

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Threading;
using System.Security.Cryptography;
using Dynamo.Nodes.TypeSystem;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using Dynamo.Connectors;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    [NodeName("Web Request")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Fetches data from the web using a URL.")]
    public class dynWebRequest : dynNodeWithOneOutput
    {
        public dynWebRequest()
        {
            InPortData.Add(new PortData("url", "A URL to query.", new StringType()));
            OutPortData.Add(new PortData("str", "The string returned from the web request.", new StringType()));
            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            string url = ((Value.String)args[0]).Item;

            //send a webrequest to the URL
            // Initialize the WebRequest.
            var myRequest = WebRequest.Create(url);

            // Return the response. 
            WebResponse myResponse = myRequest.GetResponse();

            Stream dataStream = myResponse.GetResponseStream();

            // Open the stream using a StreamReader for easy access.
            if (dataStream != null)
            {
                var reader = new StreamReader(dataStream);

                // Read the content.
                string responseFromServer = reader.ReadToEnd();

                reader.Close();

                // Close the response to free resources.
                myResponse.Close();

                return Value.NewString(responseFromServer);
            }
            return Value.NewString(string.Empty);
        }
    }

    [NodeName("UDP Listener")]
    [NodeCategory(BuiltinNodeCategories.IO_HARDWARE)]
    [NodeDescription("Listens for data from the web using a UDP port")]
    public class dynUDPListener : dynNodeWithOneOutput
    {
        public dynUDPListener()
        {
            InPortData.Add(new PortData("exec", "Execution Interval", new NumberType()));
            InPortData.Add(new PortData("udp port", "A UDP port to listen to.", new NumberType()));
            OutPortData.Add(new PortData("str", "The string returned from the web request.", new StringType()));

            RegisterAllPorts();
        }

        private delegate void LogDelegate(string msg);

        public string UDPResponse = "";
        int listenPort;
        //bool UDPInitialized = false;

        public class UdpState
        {
            public IPEndPoint E;
            public UdpClient U;

        }

        public static bool MessageReceived = false;

        public void ReceiveCallback(IAsyncResult ar)
        {
            LogDelegate log = dynSettings.Controller.DynamoViewModel.Log;

            try
            {
                var u = ((UdpState)ar.AsyncState).U;
                var e = ((UdpState)ar.AsyncState).E;

                Byte[] receiveBytes = u.EndReceive(ar, ref e);
                string receiveString = Encoding.ASCII.GetString(receiveBytes);

                UDPResponse = Encoding.ASCII.GetString(receiveBytes, 0, receiveBytes.Length);
                string verboseLog = "Received broadcast from " + e + ":\n" + UDPResponse + "\n";
                log(verboseLog);

                Console.WriteLine("Received: {0}", receiveString);
                MessageReceived = true;
            }
            catch (Exception e)
            {
                UDPResponse = "";
                log(e.ToString());
            }
        }

        private void ListenOnUDP()
        {
            LogDelegate log = dynSettings.Controller.DynamoViewModel.Log;

            // UDP sample from http://stackoverflow.com/questions/8274247/udp-listener-respond-to-client
            var listener = new UdpClient(listenPort);
            var groupEP = new IPEndPoint(IPAddress.Any, listenPort);

            try
            {

                if (MessageReceived == false)
                {

                    var s = new UdpState { E = groupEP, U = listener };


                    log("Waiting for broadcast");
                    listener.BeginReceive(ReceiveCallback, s);
                    //byte[] bytes = listener.Receive(ref groupEP);
                }
            }
            catch (Exception e)
            {
                UDPResponse = "";
                log(e.ToString());
            }
            finally
            {
                if (MessageReceived)
                {
                    listener.Close();
                    MessageReceived = false;
                }
            }
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            listenPort = (int)((Value.Number)args[1]).Item; // udp port to listen to

            if (((Value.Number)args[0]).Item == 1) // if exec node has pumped
            {
                //MVVM: now using node's dispatch on UI thread method
                //NodeUI.Dispatcher.BeginInvoke(new UDPListening(ListenOnUDP));
                DispatchOnUIThread(ListenOnUDP);
            }

            return Value.NewString(UDPResponse);
        }

    }
}
