using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Nodes;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo.TypeSystem
{
    public class NodeTypeInformation
    {
        //public List<IDynamoType> Inputs;
        public List<IDynamoType> Outputs;
        public List<int> MapPorts;
    }

    public class UnificationResult
    {
        public IDynamoType Defined { get; internal set; }
        public IDynamoType Expected { get; internal set; }
        public int ReductionAmount { get; internal set; }
    }

    internal static class UnifyExtension
    {
        public static bool Unify(this IDynamoType defined, IDynamoType expected, UnificationResult result)
        {
            //return defined.Equals(expected) || unify(defined as dynamic, expected as dynamic);

            if (defined.Equals(expected))
            {
                return true;
            }

            if (defined is GuessType)
            {
                var t1 = defined as GuessType;
                if (t1.HasType)
                    return t1.Type.Unify(expected, result);
                t1.Type = expected;
                return true;
            }

            if (expected is GuessType)
            {
                var t2 = expected as GuessType;
                if (t2.HasType)
                    return t2.Type.Unify(defined, result);
                t2.Type = defined;
                return true;
            }

            if (defined is FunctionType && expected is FunctionType)
            {
                var t1 = (FunctionType)defined;
                var t2 = (FunctionType)expected;
                var funResult = new UnificationResult();
                return t1.Inputs.Zip(t2.Inputs, Tuple.Create).All(inputPair => inputPair.Item1.Unify(inputPair.Item2, funResult))
                    && t1.Output.Unify(t2.Output, funResult)
                    && funResult.ReductionAmount == 0;
            }

            if (expected is ListType)
            {
                var t2 = (ListType)expected;
                if (defined is ListType)
                {
                    var t1 = (ListType)defined;
                    return t1.InnerType.Unify(t2.InnerType, result);
                }

                var reduced = defined.Unify(t2.InnerType, result);
                result.ReductionAmount++;
                return reduced;
            }

            return false;
        }
    }

    public interface IDynamoType : IComparable
    {
        IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs);
        FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator);
        IDynamoType Unwrap();
        IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict);
    }

    internal interface IAtomType : IDynamoType
    {
    }

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

    public struct UnitType : IAtomType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> a)
        {
            return a;
        }

        public IDynamoType Unwrap()
        {
            return this;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap()
        {
            return new FunctionType(Inputs.Select(x => x.Unwrap()), Output.Unwrap());
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new FunctionType(
                Inputs.Select(x => x.InstantiatePolymorphicTypes(polyDict)), 
                Output.InstantiatePolymorphicTypes(polyDict));
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "(" + string.Join(" ", Inputs.Select(x => x.ToString())) + " -> " + Output
                   + ")";
        }
    }

    public struct TypeIntersection : IDynamoType
    {
        public IDynamoType Type1 { get; private set; }
        public IDynamoType Type2 { get; private set; }

        public TypeIntersection(IDynamoType type, params IDynamoType[] types) 
            : this(new[] { type }.Concat(types))
        { }

        public TypeIntersection(IEnumerable<IDynamoType> types)
            : this(ListModule.OfSeq(types))
        { }

        private TypeIntersection(FSharpList<IDynamoType> types) : this()
        {
            Type1 = types.Head;
            var tail = types.Tail;
            Type2 = tail.Length == 1
                        ? tail.Head
                        : new TypeIntersection(tail);
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return Type2.GatherGuesses(Type1.GatherGuesses(accumulator));
        }

        public IDynamoType Unwrap()
        {
            return new TypeIntersection(Type1.Unwrap(), Type2.Unwrap());
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new TypeIntersection(
                Type1.InstantiatePolymorphicTypes(polyDict), 
                Type2.InstantiatePolymorphicTypes(polyDict));
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "(" + string.Join(" ∧ ", Type1, Type2) + ")";
        }
    }

    public struct TypeUnion : IDynamoType
    {
        public IDynamoType Type1 { get; private set; }
        public IDynamoType Type2 { get; private set; }

        public TypeUnion(IDynamoType type, params IDynamoType[] types) 
            : this(new[] { type }.Concat(types))
        { }

        public TypeUnion(IEnumerable<IDynamoType> types)
            : this(ListModule.OfSeq(types))
        { }

        private TypeUnion(FSharpList<IDynamoType> types) : this()
        {
            Type1 = types.Head;
            var tail = types.Tail;
            Type2 = tail.Length == 1
                        ? tail.Head
                        : new TypeUnion(tail);
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return Type2.GatherGuesses(Type1.GatherGuesses(accumulator));
        }

        public IDynamoType Unwrap()
        {
            return new TypeUnion(Type1.Unwrap(), Type2.Unwrap());
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new TypeUnion(
                Type1.InstantiatePolymorphicTypes(polyDict),
                Type2.InstantiatePolymorphicTypes(polyDict));
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "(" + string.Join(" ∨ ", Type1, Type2) + ")";
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

        public IDynamoType Unwrap()
        {
            return new ListType(InnerType.Unwrap());
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return new ListType(InnerType.InstantiatePolymorphicTypes(polyDict));
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return InnerType + " list";
        }
    }

    public struct NumberType : IAtomType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType Unwrap()
        {
            return this;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Number";
        }
    }

    public struct StringType : IAtomType
    {
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType Unwrap()
        {
            return this;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap()
        {
            return this;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Any";
        }
    }

    public struct ObjectType : IAtomType
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

        public IDynamoType Unwrap()
        {
            return this;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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
        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            throw new NotImplementedException();
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            throw new NotImplementedException();
        }

        public IDynamoType Unwrap()
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

    internal class GuessType : IDynamoType
    {
        private IDynamoType _type;

        internal bool HasType
        {
            get { return Type != null; }
        }

        internal IDynamoType Type
        {
            get { return _type; }
            set { _type = value.Unwrap(); }
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator.Add(this);
        }

        public IDynamoType Unwrap()
        {
            return HasType 
                ? Type.Unwrap() 
                : this;
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

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return vsAndTs[_guid];
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType Unwrap()
        {
            return this;
        }

        public IDynamoType InstantiatePolymorphicTypes(Dictionary<PolymorphicType, IDynamoType> polyDict)
        {
            return this;
        }
    }
}