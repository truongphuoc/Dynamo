using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo.TypeSystem
{
    public interface IDynamoType
    {
        IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs);
        FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator);
        IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict);
    }

    public struct UnitType : IDynamoType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> a)
        {
            return a;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public override string ToString()
        {
            return "Unit";
        }
    }

    public struct FunctionType : IDynamoType
    {
        public List<IDynamoType> Inputs { get; private set; }
        public IDynamoType Output;

        public FunctionType(params IDynamoType[] types) : this()
        {
            Output = types.Last();
            Inputs = types.Take(types.Length - 1).ToList();
        }

        public FunctionType(IEnumerable<IDynamoType> inputs, IDynamoType output) : this()
        {
            Output = output;
            Inputs = inputs.ToList();
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return new FunctionType(
                Inputs.Select(x => x.Subst(vsAndTs)),
                Output.Subst(vsAndTs));
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return Inputs.Aggregate(
                Output.GatherGuesses(accumulator),
                (a, x) => x.GatherGuesses(a));
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new FunctionType(
                Inputs.Select(x => x.InstantiatePolymorphicTypes(polyDict)), 
                Output.InstantiatePolymorphicTypes(polyDict));
        }

        public override string ToString()
        {
            return "(" + string.Join(" ", Inputs.Select(x => x.ToString())) + " -> " + Output
                   + ")";
        }

        internal FSharpOption<TypeCheckResult> UnifyFunction(FunctionType expected, FSharpMap<GuessType, IDynamoType> guessEnv)
        {
            UnificationResult uR;

            var rInputs = new List<TypeCheckResult>();

            foreach (var inputPair in Inputs.Zip(expected.Inputs, Tuple.Create))
            {
                uR = new UnificationResult();
                var unification = inputPair.Item2.Unify(inputPair.Item1, guessEnv, uR);

                if (FSharpOption<TypeCheckResult>.get_IsNone(unification) || uR.ReductionAmount != 0)
                    return FSharpOption<TypeCheckResult>.None;

                rInputs.Add(unification.Value);

                guessEnv = unification.Value.GuessEnv;
            }

            uR = new UnificationResult();

            var rOutput = Output.Unify(expected.Output, guessEnv, uR);

            if (FSharpOption<TypeCheckResult>.get_IsNone(rOutput) || uR.ReductionAmount != 0)
                return FSharpOption<TypeCheckResult>.None;

            return FSharpOption<TypeCheckResult>.Some(
                new TypeCheckResult
                {
                    Type = new FunctionType(rInputs.Select(x => x.Type), rOutput.Value.Type),
                    GuessEnv = rOutput.Value.GuessEnv
                });
        }
    }

    public class FunctionOverloadType : IDynamoType
    {
        public List<FunctionType> Overloads;

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return new FunctionOverloadType
            {
                Overloads = Overloads.Select(x => (FunctionType)x.Subst(vsAndTs)).ToList()
            };
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return Overloads.Aggregate(accumulator, (set, type) => type.GatherGuesses(set));
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new FunctionOverloadType
            {
                Overloads =
                    Overloads.Select(x => (FunctionType)x.InstantiatePolymorphicTypes(polyDict))
                             .ToList()
            };
        }
    }

    public struct ListType : IDynamoType
    {
        public IDynamoType InnerType { get; private set; }

        public ListType(IDynamoType inner) : this()
        {
            InnerType = inner;
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return new ListType(InnerType.Subst(vsAndTs));
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return InnerType.GatherGuesses(accumulator);
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new ListType(InnerType.InstantiatePolymorphicTypes(polyDict));
        }

        public override string ToString()
        {
            return InnerType + " list";
        }
    }

    public struct NumberType : IDynamoType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public override string ToString()
        {
            return "Number";
        }
    }

    public struct StringType : IDynamoType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public override string ToString()
        {
            return "String";
        }
    }

    public struct AnyType : IDynamoType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public override string ToString()
        {
            return "Any";
        }
    }

    public struct ObjectType : IDynamoType
    {
        public bool Equals(ObjectType other)
        {
            return Type == other.Type;
        }

        public override int GetHashCode()
        {
            return (Type != null ? Type.GetHashCode() : 0);
        }

        public ObjectType(Type t)
        {
            Type = t;
        }

        public readonly Type Type;

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectType && ((ObjectType)obj).Type == Type;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }

    public class PolymorphicType : IDynamoType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            throw new NotImplementedException();
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            throw new NotImplementedException();
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            if (!polyDict.ContainsKey(this))
                polyDict[this] = new GuessType();

            return polyDict[this];
        }
    }

    internal class GuessType : IDynamoType, IComparable
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator.Add(this);
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            return GetHashCode() - obj.GetHashCode();
        }
    }

    internal struct TypeVar : IDynamoType
    {
        private readonly Guid _guid;

        public TypeVar(Guid guid)
        {
            _guid = guid;
        }

        public static TypeVar Create()
        {
            return new TypeVar(Guid.NewGuid());
        }

        public override bool Equals(object obj)
        {
            return obj is TypeVar && ((TypeVar)obj)._guid.Equals(_guid);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return vsAndTs[_guid];
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }
    }

    public class TypeUnion : IDynamoType
    {
        public HashSet<IDynamoType> Types;

        public static IDynamoType MakeUnion(params IDynamoType[] types)
        {
            return MakeUnion(types as IEnumerable<IDynamoType>);
        }

        public static IDynamoType MakeUnion(IEnumerable<IDynamoType> types)
        {
            var dynamoTypes = types as HashSet<IDynamoType> ?? new HashSet<IDynamoType>(types);

            return dynamoTypes.Count == 1 
                ? dynamoTypes.First() 
                : new TypeUnion(
                    new HashSet<IDynamoType>(
                        dynamoTypes.Where(x => !(x is TypeUnion))
                                   .Concat(dynamoTypes.OfType<TypeUnion>().SelectMany(x => x.Types))));
        }

        private TypeUnion(HashSet<IDynamoType> types)
        {
            Types = types;
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return new TypeUnion(new HashSet<IDynamoType>(Types.Select(x => x.Subst(vsAndTs))));
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return Types.Aggregate(accumulator, (set, type) => type.GatherGuesses(set));
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return
                new TypeUnion(
                    new HashSet<IDynamoType>(
                        Types.Select(x => x.InstantiatePolymorphicTypes(polyDict))));
        }
    }

    public struct NotType : IDynamoType
    {
        public IDynamoType Type;

        public NotType(IDynamoType t)
        {
            Type = t;
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return new NotType(Type.Subst(vsAndTs));
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return Type.GatherGuesses(accumulator);
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new NotType(Type.InstantiatePolymorphicTypes(polyDict));
        }
    }
}