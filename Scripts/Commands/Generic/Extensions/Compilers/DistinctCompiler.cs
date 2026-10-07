using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Server.Commands.Generic
{
    public static class DistinctCompiler
    {
        public static IComparer Compile(AssemblyEmitter assembly, Type objectType, Property[] props)
        {
            var typeBuilder = assembly.DefineType(
                "__distinct",
                TypeAttributes.Public,
                typeof(object));

            #region Constructor

            {
                var ctor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes);

                var il = ctor.GetILGenerator();

                // : base()
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

                // return;
                il.Emit(OpCodes.Ret);
            }

            #endregion

            #region IComparer

            typeBuilder.AddInterfaceImplementation(typeof(IComparer));

            MethodBuilder compareMethod;

            #region Compare

            {
                var emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "Compare",
                    /*  attr  */ MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */ typeof(int),
                    /* params */ new[] { typeof(object), typeof(object) });

                var a = emitter.CreateLocal(objectType);
                var b = emitter.CreateLocal(objectType);

                var v = emitter.CreateLocal(typeof(int));

                emitter.LoadArgument(1);
                emitter.CastAs(objectType);
                emitter.StoreLocal(a);

                emitter.LoadArgument(2);
                emitter.CastAs(objectType);
                emitter.StoreLocal(b);

                emitter.Load(0);
                emitter.StoreLocal(v);

                var end = emitter.CreateLabel();

                for (var i = 0; i < props.Length; ++i)
                {
                    if (i > 0)
                    {
                        emitter.LoadLocal(v);
                        emitter.BranchIfTrue(end); // if ( v != 0 ) return v;
                    }

                    var prop = props[i];

                    emitter.LoadLocal(a);
                    emitter.Chain(prop);

                    var couldCompare =
                        emitter.CompareTo(1, delegate
                        {
                            emitter.LoadLocal(b);
                            emitter.Chain(prop);
                        });

                    if (!couldCompare)
                        throw new InvalidOperationException("Property is not comparable.");

                    emitter.StoreLocal(v);
                }

                emitter.MarkLabel(end);

                emitter.LoadLocal(v);
                emitter.Return();

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IComparer).GetMethod(
                        "Compare",
                        new[]
                        {
                            typeof(object),
                            typeof(object)
                        }));

                compareMethod = emitter.Method;
            }

            #endregion

            #endregion

            #region IEqualityComparer

            typeBuilder.AddInterfaceImplementation(typeof(IEqualityComparer<object>));

            #region Equals

            {
                var emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "Equals",
                    /*  attr  */ MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */ typeof(bool),
                    /* params */ new[] { typeof(object), typeof(object) });

                emitter.Generator.Emit(OpCodes.Ldarg_0);
                emitter.Generator.Emit(OpCodes.Ldarg_1);
                emitter.Generator.Emit(OpCodes.Ldarg_2);

                emitter.Generator.Emit(OpCodes.Call, compareMethod);

                emitter.Generator.Emit(OpCodes.Ldc_I4_0);

                emitter.Generator.Emit(OpCodes.Ceq);

                emitter.Generator.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IEqualityComparer<object>).GetMethod(
                        "Equals",
                        new[]
                        {
                            typeof(object),
                            typeof(object)
                        }));
            }

            #endregion

            #region GetHashCode

            {
                var emitter = new MethodEmitter(typeBuilder);

                emitter.Define(
                    /*  name  */ "GetHashCode",
                    /*  attr  */ MethodAttributes.Public | MethodAttributes.Virtual,
                    /* return */ typeof(int),
                    /* params */ new[] { typeof(object) });

                var obj = emitter.CreateLocal(objectType);

                emitter.LoadArgument(1);
                emitter.CastAs(objectType);
                emitter.StoreLocal(obj);

                for (var i = 0; i < props.Length; ++i)
                {
                    var prop = props[i];

                    emitter.LoadLocal(obj);
                    emitter.Chain(prop);

                    var active = emitter.Active;

                    var getHashCode = active.GetMethod("GetHashCode", Type.EmptyTypes);

                    if (getHashCode == null)
                        getHashCode = typeof(object).GetMethod("GetHashCode", Type.EmptyTypes);

                    if (active != typeof(int))
                    {
                        if (!active.IsValueType)
                        {
                            var value = emitter.AcquireTemp(active);

                            var valueNotNull = emitter.CreateLabel();
                            var done = emitter.CreateLabel();

                            emitter.StoreLocal(value);
                            emitter.LoadLocal(value);

                            emitter.BranchIfTrue(valueNotNull);

                            emitter.Load(0);
                            emitter.Pop(typeof(int));

                            emitter.Branch(done);

                            emitter.MarkLabel(valueNotNull);

                            emitter.LoadLocal(value);
                            emitter.Call(getHashCode);

                            emitter.ReleaseTemp(value);

                            emitter.MarkLabel(done);
                        }
                        else
                        {
                            emitter.Call(getHashCode);
                        }
                    }

                    if (i > 0)
                        emitter.Xor();
                }

                emitter.Return();

                typeBuilder.DefineMethodOverride(
                    emitter.Method,
                    typeof(IEqualityComparer<object>).GetMethod(
                        "GetHashCode",
                        new[]
                        {
                            typeof(object)
                        }));
            }

            #endregion

            #endregion

            var comparerType = typeBuilder.CreateType();

            return (IComparer)Activator.CreateInstance(comparerType);
        }
    }
}
