using System.Linq;
using System.Collections.Generic;

namespace Dynamo.TypeSystem
{
    public class NodeTypeInformation
    {
        public IEnumerable<IEnumerable<IDynamoType>> OutputTypes 
        { 
            get 
            { 
                return Outputs.Select(x => x.Select(y => y.Type));
            } 
        }

        internal List<List<TypeCheckResult>> Outputs;
        public List<int> MapPorts;
    }
}