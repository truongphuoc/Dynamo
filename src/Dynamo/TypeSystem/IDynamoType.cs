using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo.Nodes.TypeSystem
{
    internal static class UnifyExtension
    {
        private static bool unify(GuessType t1, IDynamoType t2)
        {
            if (t1.HasType)
                return t1.Type.Unify(t2);

            t1.Type = t2;
            return true;
        }

        private static bool unify(ObjectType t1, ObjectType t2)
        {
            return t1.Type.IsAssignableFrom(t2.Type) || t2.Type.IsAssignableFrom(t1.Type);
        }

        private static bool unify(IDynamoType t1, GuessType t2)
        {
            return Unify(t2, t1);
        }

        private static bool unify(FunctionType t1, FunctionType t2)
        {
            return t1.Inputs.Zip(t2.Inputs, Tuple.Create).All(x => x.Item1.Unify(x.Item2))
                   && t1.Output.Unify(t2.Output);
        }

        private static bool unify(ListType t1, ListType t2)
        {
            return Unify(t1.InnerType, t2.InnerType);
        }

        private static bool unify(IDynamoType t1, ListType t2)
        {
            return unify(t2, t1);
        }

        private static bool unify(ListType t1, IDynamoType t2)
        {
            return Unify(t1.InnerType, t2);
        }

        private static bool unify(IDynamoType a, IDynamoType b)
        {
            return false;
        }

        public static bool Unify(this IDynamoType a, IDynamoType b)
        {
            return a.Equals(b) || unify(a as dynamic, b as dynamic);
        }
    }

    public interface IDynamoType : IComparable
    {
        IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs);
        FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator);
        IDynamoType Unwrap(IDynamoType noneCase);
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

        public static TypeScheme Generalize(FSharpMap<dynSymbol, TypeScheme> env, IDynamoType t)
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }
    }

    public struct ObjectType : IAtomType
    {
        public ObjectType(Type t)
        {
            Type = t;
        }

        public Type Type;

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }
    }

    public class GuessType : IDynamoType
    {
        internal bool HasType
        {
            get { return Type != null; }
        }

        internal IDynamoType Type;

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator.Add(this);
        }

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            if (HasType)
                return Type.Unwrap(noneCase);
            return Type = noneCase;
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

        public IDynamoType Unwrap(IDynamoType noneCase)
        {
            return this;
        }
    }
}