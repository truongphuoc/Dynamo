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
        static bool unify(GuessType t1, IDynamoType t2)
        {
            if (t1.HasType)
            {
                return t1.Type.Unify(t2);
            }

            t1.Type = t2;
            return true;
        }

        static bool unify(IDynamoType t1, GuessType t2)
        {
            return Unify(t2, t1);
        }

        static bool unify(FunctionType t1, FunctionType t2)
        {
            return t1.Inputs.Zip(t2.Inputs, Tuple.Create).All(x => x.Item1.Unify(x.Item2))
                && t1.Output.Unify(t2.Output);
        }

        static bool unify(ListType t1, ListType t2)
        {
            return Unify(t1.InnerType, t2.InnerType);
        }

        static bool unify(IDynamoType t1, ListType t2)
        {
            return unify(t2, t1);
        }

        static bool unify(ListType t1, IDynamoType t2)
        {
            return Unify(t1.InnerType, t2);
        }

        static bool unify(IDynamoType a, IDynamoType b)
        {
            return false;
        }

        public static bool Unify(this IDynamoType a, IDynamoType b)
        {
            return a.Equals(b) || unify(a as dynamic, b as dynamic);
        }
    }

    public interface IDynamoType
    {
        IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs);
        FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator);
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
    }

    public struct TypeIntersection : IDynamoType
    {
        private readonly HashSet<IDynamoType> _intersection;

        public IEnumerable<IDynamoType> GetTypesOfIntersection()
        {
            return _intersection;
        }

        public TypeIntersection(params IDynamoType[] types)
        {
            _intersection = new HashSet<IDynamoType>(types);
        }

        public TypeIntersection(IEnumerable<IDynamoType> types)
        {
            _intersection = new HashSet<IDynamoType>(types);
        }

        public void Add(IDynamoType t)
        {
            _intersection.Add(t);
        }

        public void Remove(IDynamoType t)
        {
            _intersection.Remove(t);
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return _intersection.Aggregate(accumulator, (a, x) => x.GatherGuesses(a));
        }
    }

    public struct TypeUnion : IDynamoType 
    {
        private readonly HashSet<IDynamoType> _union;

        public IEnumerable<IDynamoType> GetTypesOfUnion()
        {
            return _union;
        }

        public void Add(IDynamoType t)
        {
            _union.Add(t);
        }

        public void Remove(IDynamoType t)
        {
            _union.Remove(t);
        }

        public TypeUnion(params IDynamoType[] types)
        {
            _union = new HashSet<IDynamoType>(types);
        }

        public TypeUnion(IEnumerable<IDynamoType> types)
        {
            _union = new HashSet<IDynamoType>(types);
        }

        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return _union.Aggregate(accumulator, (a, x) => x.GatherGuesses(a));
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
    }

    public struct ObjectType : IAtomType
    {
        public Type Type;
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator;
        }
    }

    internal struct GuessType : IDynamoType
    {
        internal bool HasType { get { return Type != null; } }
        internal IDynamoType Type;
        public IDynamoType Subst(FSharpMap<Guid, IDynamoType> vsAndTs)
        {
            return this;
        }

        public FSharpSet<IDynamoType> GatherGuesses(FSharpSet<IDynamoType> accumulator)
        {
            return accumulator.Add(this);
        }
    }

    public struct TypeVar : IDynamoType
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
    }
    
}
