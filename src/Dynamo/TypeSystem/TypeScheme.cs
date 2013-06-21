using System;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo.TypeSystem
{
    internal struct TypeScheme
    {
        public FSharpList<Guid> Vs;
        public IDynamoType Type;

        private TypeScheme(FSharpList<Guid> vs, IDynamoType type)
        {
            Type = type;
            Vs = vs;
        }

        public static TypeScheme Empty()
        {
            return new TypeScheme(ListModule.Empty<Guid>(), new GuessType());
        }

        public static TypeScheme Generalize(FSharpMap<string, TypeScheme> env, IDynamoType t)
        {
            var empty = SetModule.Empty<IDynamoType>();
            var tGs = t.GatherGuesses(empty);
            var envListGs = env.Select(kvp => kvp.Value.Type.GatherGuesses(empty));
            var envGs = SetModule.OfSeq(envListGs.Aggregate(empty, SetModule.Union));
            var diff = SetModule.Difference(tGs, envGs);
            var gsVs = MapModule.OfSeq(
                SetModule.Map(
                    FSharpFunc<IDynamoType, Tuple<Guid, IDynamoType>>.FromConverter(
                        g => Tuple.Create(Guid.NewGuid(), g)),
                    diff));
            var tc = t.Subst(gsVs);
            return new TypeScheme(ListModule.OfSeq(gsVs.Select(kvp => kvp.Key)), tc);
        }

        public IDynamoType Instantiate()
        {
            var vsAndTs =
                MapModule.OfList(
                    ListModule.Map(
                        FSharpFunc<Guid, Tuple<Guid, IDynamoType>>.FromConverter(
                            x => Tuple.Create(x, TypeVar.Create() as IDynamoType)),
                        Vs));
            return Type.Subst(vsAndTs);
        }
    }
}