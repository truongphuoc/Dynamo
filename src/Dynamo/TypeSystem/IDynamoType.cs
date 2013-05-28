using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dynamo.Nodes.TypeSystem
{
    internal static class UnifyUtility
    {
        static bool Unify(PolymorphicType t1, IDynamoType t2)
        {
            if (t1.HasType)
            {
                return t1.Type.Unify(t2);
            }
            else
            {
                t1.Type = t2;
                return true;
            }
        }

        static bool Unify(IDynamoType t1, PolymorphicType t2)
        {
            return Unify(t2, t1);
        }

        static bool Unify(FunctionType t1, FunctionType t2)
        {
            return t1.Inputs.Zip(t2.Inputs, Tuple.Create).All(x => x.Item1.Unify(x.Item2))
                && t1.Output.Unify(t2.Output);
        }

        public static bool Unify(this IDynamoType a, IDynamoType b)
        {
            if (a.Equals(b))
                return true;

            return Unify(a as dynamic, b as dynamic);
        }
    }

    public interface IDynamoType
    {
    }

    public interface IAtomType : IDynamoType
    {
        
    }

    public struct UnitType : IAtomType
    {

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
    } 
    
    public struct ListType : IDynamoType 
    {
        public IDynamoType InnerType { get; private set; }

        public ListType(IDynamoType inner) : this()
        {
            InnerType = inner;
        }

        //public bool DimensionDifference(IDynamoType t, Dictionary<Guid, IDynamoType> polyMap, out int diff)
        //{
        //    diff = 0;
        //    return dimDifference(t, polyMap, ref diff);
        //}

        //private bool dimDifference(IDynamoType t, Dictionary<Guid, IDynamoType> polyMap, ref int diff)
        //{
        //    if (t.AcceptsType(this, polyMap))
        //        return true;
        //    else if (InnerType is ListType)
        //    {
        //        diff += 1;
        //        return ((ListType)InnerType).dimDifference(t, polyMap, ref diff);
        //    }
        //    else
        //        return false;
        //}
    }

    public struct NumberType : IAtomType 
    {
    }

    public struct StringType : IAtomType 
    {
    }
    
    public struct AnyType : IDynamoType 
    {
    }

    public struct ObjectType : IAtomType
    {
        public Type Type;
    }
    
    public struct PolymorphicType : IDynamoType
    {
        public static PolymorphicType Create()
        {
            return new PolymorphicType(Guid.NewGuid());
        }

        public Guid Parameter;

        public PolymorphicType(Guid p)
        {
            Parameter = p;
            Type = null;
        }

        internal bool HasType { get { return Type != null; } }
        internal IDynamoType Type;
    }
}
