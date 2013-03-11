//Copyright 2012 Ian Keough

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
using System.Linq;
using System.Windows.Controls;
using Dynamo.Connectors;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;

using Dynamo.FSchemeInterop;
using Value = Dynamo.FScheme.Value;

namespace Dynamo.Nodes
{
    [NodeName("ASM Test")]
    [NodeCategory(BuiltinNodeCategories.REVIT_POINTS)]
    [NodeDescription("Test ASM geometry")]
    public class dynASMTest : dynNodeWithOneOutput
    {
        public dynASMTest()
        {
            InPortData.Add(new PortData("xyz", "The point(s) from which to create reference points.", typeof(object)));
            OutPortData.Add(new PortData("pt", "The Reference Point(s) created from this operation.", typeof(object)));

            NodeUI.RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            //Fin
            return Value.NewNumber(0);
        }
    }
}
