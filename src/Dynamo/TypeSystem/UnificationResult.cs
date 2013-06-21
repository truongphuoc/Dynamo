using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Dynamo.TypeSystem
{
    public class UnificationResult
    {
        public IDynamoType Defined { get; internal set; }
        public IDynamoType Expected { get; internal set; }
        public int ReductionAmount { get; internal set; }
        public IDynamoType Generalized { get; internal set; }
    }

    internal static class UnifyExtension
    {
        public static FSharpOption<TypeCheckResult> Unify(this IDynamoType defined, IDynamoType expected, FSharpMap<GuessType, IDynamoType> guessEnv, UnificationResult result)
        {
            if (defined.Equals(expected))
            {
                return FSharpOption<TypeCheckResult>.Some(
                    new TypeCheckResult
                    {
                        Type = defined, 
                        GuessEnv = guessEnv
                    });
            }

            if (defined is GuessType)
            {
                var t1 = defined as GuessType;
                if (guessEnv.ContainsKey(t1))
                {
                    var unification = expected.Unify(guessEnv[t1], guessEnv, result);

                    if (FSharpOption<TypeCheckResult>.get_IsSome(unification))
                    {
                        return FSharpOption<TypeCheckResult>.Some(
                            new TypeCheckResult
                            {
                                Type = unification.Value.Type,
                                GuessEnv = MapModule.Add(t1, unification.Value.Type, unification.Value.GuessEnv)
                            });
                    }
                    return unification;
                }

                return FSharpOption<TypeCheckResult>.Some(
                    new TypeCheckResult
                    {
                        Type = expected,
                        GuessEnv = MapModule.Add(t1, expected, guessEnv)
                    });
            }

            if (expected is GuessType)
            {
                var t2 = expected as GuessType;
                if (guessEnv.ContainsKey(t2))
                {
                    var unification = guessEnv[t2].Unify(defined, guessEnv, result);

                    if (FSharpOption<TypeCheckResult>.get_IsSome(unification))
                    {
                        return FSharpOption<TypeCheckResult>.Some(
                            new TypeCheckResult
                            {
                                Type = unification.Value.Type,
                                GuessEnv = MapModule.Add(t2, unification.Value.Type, unification.Value.GuessEnv)
                            });
                    }
                    return unification;
                }

                return FSharpOption<TypeCheckResult>.Some(
                    new TypeCheckResult
                    {
                        Type = defined,
                        GuessEnv = MapModule.Add(t2, defined, guessEnv)
                    });
            }

            if (defined is FunctionType) 
            {
                var t1 = (FunctionType)defined;

                if (expected is FunctionType)
                {
                    var t2 = (FunctionType)expected;
                    return t1.UnifyFunction(t2, guessEnv);
                }

                if (expected is FunctionOverloadType)
                {
                    //TODO: Test ALL overloads and throw out ones that don't work.
                    //      handles to following case:
                    //      (map is-string? '(1 "hi")) ;; is-string is overloaded
                    
                    var t2 = expected as FunctionOverloadType;
                    var unification =
                        t2.Overloads.Select(x => t1.UnifyFunction(x, guessEnv))
                          .FirstOrDefault(FSharpOption<TypeCheckResult>.get_IsSome);

                    if (unification != null)
                        return unification;
                }
            }

            if (defined is FunctionOverloadType)
            {
                var t1 = defined as FunctionOverloadType;

                var rF = new List<FunctionType>();

                foreach (var f in t1.Overloads)
                {
                    var uR = new UnificationResult();
                    var unification = f.Unify(expected, guessEnv, uR);
                    if (FSharpOption<TypeCheckResult>.get_IsNone(unification)
                        || uR.ReductionAmount != 0)
                    {
                        return FSharpOption<TypeCheckResult>.None;
                    }
                    guessEnv = unification.Value.GuessEnv;
                    rF.Add((FunctionType)unification.Value.Type);
                }

                return FSharpOption<TypeCheckResult>.Some(
                    new TypeCheckResult
                    {
                        Type = new FunctionOverloadType { Overloads = rF },
                        GuessEnv = guessEnv
                    });
            }

            if (expected is TypeUnion)
            {
                var t2 = expected as TypeUnion;

                if (defined is TypeUnion)
                {
                    var t1 = defined as TypeUnion;

                    var rUnion = new HashSet<IDynamoType>();

                    var query =
                        t1.Types.Select(
                            type => t2.Types.Select(t => type.Unify(t, guessEnv, result))
                                      .FirstOrDefault(FSharpOption<TypeCheckResult>.get_IsSome));

                    foreach (var unification in query)
                    {
                        if (unification == null)
                            return FSharpOption<TypeCheckResult>.None;

                        guessEnv = unification.Value.GuessEnv;
                        rUnion.Add(unification.Value.Type);
                    }

                    return FSharpOption<TypeCheckResult>.Some(
                        new TypeCheckResult
                        {
                            Type = TypeUnion.MakeUnion(rUnion),
                            GuessEnv = guessEnv
                        });
                }
            }

            if (defined is TypeUnion)
            {
                var t1 = defined as TypeUnion;

                var unification = 
                    t1.Types.Select(t => t.Unify(expected, guessEnv, new UnificationResult()))
                            .FirstOrDefault(FSharpOption<TypeCheckResult>.get_IsSome);

                if (unification != null)
                {
                    return unification;
                }
            }

            if (expected is ListType)
            {
                var t2 = (ListType)expected;
                if (defined is ListType)
                {
                    var t1 = (ListType)defined;
                    return t1.InnerType.Unify(t2.InnerType, guessEnv, result);
                }

                var reduced = defined.Unify(t2.InnerType, guessEnv, result);
                result.ReductionAmount++;
                return reduced;
            }

            if (defined is ObjectType && expected is ObjectType)
            {
                var t1 = (ObjectType)defined;
                var t2 = (ObjectType)expected;
                if (t1.Type.IsAssignableFrom(t2.Type))
                {
                    return FSharpOption<TypeCheckResult>.Some(
                        new TypeCheckResult
                        {
                            Type = t2,
                            GuessEnv = guessEnv
                        });
                }
            }

            return FSharpOption<TypeCheckResult>.None;
        }
    }
}