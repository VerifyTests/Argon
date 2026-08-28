// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#if !NET6_0_OR_GREATER
using System.Reflection.Emit;
using PropertyAttributes = System.Reflection.PropertyAttributes;

public class DynamicConcreteTests : TestFixtureBase
{
    public class DynamicConcreteContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type type)
        {
            var contract = base.CreateContract(type);

            // create a dynamic mock object for interfaces or abstract classes
            if (contract.CreatedType.IsInterface || contract.CreatedType.IsAbstract)
            {
                contract.DefaultCreator = () => DynamicConcrete.GetInstanceFor(contract.CreatedType);
            }

            return contract;
        }
    }

    [Fact]
    public void UseDynamicConcreteIfTargetObjectTypeIsAnInterfaceWithNoBackingClass()
    {
        var json = """{Name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<IInterfaceWithNoConcrete>(json, new JsonSerializerSettings
        {
            ContractResolver = new DynamicConcreteContractResolver()
        });

        Assert.Equal("Name!", c.Name);
    }

    [Fact]
    public void UseDynamicConcreteIfTargetObjectTypeIsAnAbstractClassWithNoConcrete()
    {
        var json = """{Name:"Name!", Game:"Same"}""";

        var c = JsonConvert.DeserializeObject<AbstractWithNoConcrete>(json, new JsonSerializerSettings
        {
            ContractResolver = new DynamicConcreteContractResolver()
        });

        Assert.Equal("Name!", c.Name);
        Assert.Equal("Same", c.Game);
    }

    [Fact]
    public void AnyMethodsExposedByDynamicConcreteAreHarmless()
    {
        var json = """{Name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<IInterfaceWithNoConcrete>(
            json,
            new JsonSerializerSettings
        {
            ContractResolver = new DynamicConcreteContractResolver()
        });

        c.FuncWithRefType(10, null);
        c.FuncWithValType_1();
        c.FuncWithValType_2();
    }
}

public abstract class AbstractWithNoConcrete
{
    public string Name { get; set; }
    public abstract string Game { get; set; }
}

public interface IInterfaceWithNoConcrete
{
    string Name { get; set; }
    object FuncWithRefType(int a, object b);
    int FuncWithValType_1();
    bool FuncWithValType_2();
}

/// <summary>
/// Creates run-time backing types for abstract classes and interfaces
/// </summary>
public static class DynamicConcrete
{
    /// <summary>
    /// Get an empty instance of a dynamic proxy for type T.
    /// All public fields are writable and all properties have both getters and setters.
    /// </summary>
    public static T GetInstanceFor<T>() =>
        (T) GetInstanceFor(typeof(T));

    static readonly ModuleBuilder moduleBuilder;
    static readonly AssemblyBuilder dynamicAssembly;

    /// <summary>
    /// Get an empty instance of a dynamic proxy for the given type.
    /// All public fields are writable and all properties have both getters and setters.
    /// </summary>
    public static object GetInstanceFor(Type targetType)
    {
        lock (dynamicAssembly)
        {
            var constructedType = dynamicAssembly.GetType(ProxyName(targetType)) ?? GetConstructedType(targetType);
            var instance = Activator.CreateInstance(constructedType);
            return instance;
        }
    }

    static string ProxyName(Type targetType) =>
        $"{targetType.Name}Proxy";

    static DynamicConcrete()
    {
        var assemblyName = new AssemblyName("DynImpl");
        dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
        moduleBuilder = dynamicAssembly.DefineDynamicModule("DynImplModule");
    }

    static Type GetConstructedType(Type targetType)
    {
        var typeBuilder = moduleBuilder.DefineType($"{targetType.Name}Proxy", TypeAttributes.Public);

        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            []);
        var ilGenerator = ctorBuilder.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ret);

        IncludeType(targetType, typeBuilder);

        foreach (var face in targetType.GetInterfaces())
        {
            IncludeType(face, typeBuilder);
        }

        return typeBuilder.CreateType();
    }

    static void IncludeType(Type typeOfT, TypeBuilder typeBuilder)
    {
        var methodInfos = typeOfT.GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            if (methodInfo.Name.StartsWith("set_"))
            {
                continue; // we always add a set for a get.
            }

            if (methodInfo.Name.StartsWith("get_"))
            {
                BindProperty(typeBuilder, methodInfo);
            }
            else
            {
                if (methodInfo.IsAbstract)
                {
                    BindMethod(typeBuilder, methodInfo);
                }
            }
        }

        if (typeOfT.IsInterface)
        {
            typeBuilder.AddInterfaceImplementation(typeOfT);
        }
        else if (typeOfT.IsAbstract)
        {
            typeBuilder.SetParent(typeOfT);
        }
    }

    static void BindMethod(TypeBuilder typeBuilder, MethodInfo methodInfo)
    {
        var args = methodInfo.GetParameters().Select(_ => _.ParameterType).ToArray();
        var methodBuilder = typeBuilder.DefineMethod(
            methodInfo.Name,
            MethodAttributes.Public | MethodAttributes.Virtual,
            CallingConventions.HasThis,
            methodInfo.ReturnType,
            args
        );

        var methodILGen = methodBuilder.GetILGenerator();
        if (methodInfo.ReturnType == typeof(void))
        {
            methodILGen.Emit(OpCodes.Ret);
        }
        else
        {
            if (methodInfo.ReturnType.IsPrimitive)
            {
                methodILGen.Emit(OpCodes.Ldc_I4_0);
            }
            else if (methodInfo.ReturnType.IsValueType || methodInfo.ReturnType.IsEnum)
            {
                var getMethod = typeof(Activator).GetMethod("CreateInstance", [typeof(Type)]);
                var lb = methodILGen.DeclareLocal(methodInfo.ReturnType);
                if (lb.LocalType != null)
                {
                    methodILGen.Emit(OpCodes.Ldtoken, lb.LocalType);
                    methodILGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                    methodILGen.Emit(OpCodes.Callvirt, getMethod);
                    methodILGen.Emit(OpCodes.Unbox_Any, lb.LocalType);
                }
            }
            else
            {
                methodILGen.Emit(OpCodes.Ldnull);
            }

            methodILGen.Emit(OpCodes.Ret);
        }

        typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
    }

    /// <summary>
    /// Bind a new property into a type builder with getters and setters.
    /// </summary>
    public static void BindProperty(TypeBuilder typeBuilder, MethodInfo methodInfo)
    {
        // Backing Field
        var propertyName = methodInfo.Name.Replace("get_", "");
        var propertyType = methodInfo.ReturnType;
        var backingField = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

        //Getter
        var backingGet = typeBuilder.DefineMethod($"get_{propertyName}", MethodAttributes.Public |
                                                                         MethodAttributes.SpecialName | MethodAttributes.Virtual |
                                                                         MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
        var getIl = backingGet.GetILGenerator();

        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, backingField);
        getIl.Emit(OpCodes.Ret);

        //Setter
        var backingSet = typeBuilder.DefineMethod(
            $"set_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            null,
            [propertyType]);

        var setIl = backingSet.GetILGenerator();

        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, backingField);
        setIl.Emit(OpCodes.Ret);

        // Property
        var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
        propertyBuilder.SetGetMethod(backingGet);
        propertyBuilder.SetSetMethod(backingSet);
    }
}

#endif
