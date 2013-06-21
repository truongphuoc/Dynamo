using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Collections;

namespace Dynamo.TypeSystem
{
    internal struct TypeCheckResult
    {
        public IDynamoType Type;
        public FSharpMap<GuessType, IDynamoType> GuessEnv;
    }
}
