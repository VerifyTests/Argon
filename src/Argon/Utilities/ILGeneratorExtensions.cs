// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class ILGeneratorExtensions
{
    public static void PushInstance(this ILGenerator generator, Type type)
    {
        generator.Emit(OpCodes.Ldarg_0);
        if (type.IsValueType)
        {
            generator.Emit(OpCodes.Unbox, type);
        }
        else
        {
            generator.Emit(OpCodes.Castclass, type);
        }
    }

    public static void PushArrayInstance(this ILGenerator generator, int argsIndex, int arrayIndex)
    {
        generator.Emit(OpCodes.Ldarg, argsIndex);
        generator.Emit(OpCodes.Ldc_I4, arrayIndex);
        generator.Emit(OpCodes.Ldelem_Ref);
    }

    public static void BoxIfNeeded(this ILGenerator generator, Type type)
    {
        if (type.IsValueType)
        {
            generator.Emit(OpCodes.Box, type);
        }
        else
        {
            generator.Emit(OpCodes.Castclass, type);
        }
    }

    public static void UnboxIfNeeded(this ILGenerator generator, Type type)
    {
        if (!type.IsValueType)
        {
            generator.Emit(OpCodes.Castclass, type);
            return;
        }

        var nonNull = generator.DefineLabel();
        var done = generator.DefineLabel();
        var local = generator.DeclareLocal(type);

        // stack: [obj]
        generator.Emit(OpCodes.Dup);
        generator.Emit(OpCodes.Brtrue_S, nonNull);

        // null becomes default(T), matching what reflection does when setting a value type to null
        generator.Emit(OpCodes.Pop);
        generator.Emit(OpCodes.Ldloca, local);
        generator.Emit(OpCodes.Initobj, type);
        generator.Emit(OpCodes.Ldloc, local);
        generator.Emit(OpCodes.Br_S, done);

        generator.MarkLabel(nonNull);
        generator.Emit(OpCodes.Unbox_Any, type);

        generator.MarkLabel(done);
    }

    public static void CallMethod(this ILGenerator generator, MethodInfo methodInfo)
    {
        if (methodInfo.IsFinal ||
            !methodInfo.IsVirtual)
        {
            generator.Emit(OpCodes.Call, methodInfo);
        }
        else
        {
            generator.Emit(OpCodes.Callvirt, methodInfo);
        }
    }

    public static void Return(this ILGenerator generator) =>
        generator.Emit(OpCodes.Ret);
}