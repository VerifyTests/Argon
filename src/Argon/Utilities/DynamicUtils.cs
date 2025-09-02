// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

static class DynamicUtils
{
    internal static class BinderWrapper
    {
        public const string CSharpAssemblyName = "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        const string BinderTypeName = $"Microsoft.CSharp.RuntimeBinder.Binder, {CSharpAssemblyName}";
        const string CSharpArgumentInfoTypeName = $"Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo, {CSharpAssemblyName}";
        const string CSharpArgumentInfoFlagsTypeName = $"Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, {CSharpAssemblyName}";
        const string CSharpBinderFlagsTypeName = $"Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, {CSharpAssemblyName}";

        static object? getCSharpArgumentInfoArray;
        static object? setCSharpArgumentInfoArray;
        static MethodCall<object?, object?>? getMemberCall;
        static MethodCall<object?, object?>? setMemberCall;
        static bool init;

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        static void Init()
        {
            if (init)
            {
                return;
            }

            var binderType = Type.GetType(BinderTypeName, false);
            if (binderType == null)
            {
                throw new InvalidOperationException($"Could not resolve type '{BinderTypeName}'. You may need to add a reference to Microsoft.CSharp.dll to work with dynamic types.");
            }

            // None
            getCSharpArgumentInfoArray = CreateSharpArgumentInfoArray(0);
            // None, Constant | UseCompileTimeType
            setCSharpArgumentInfoArray = CreateSharpArgumentInfoArray(0, 3);
            CreateMemberCalls();

            init = true;
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        static object CreateSharpArgumentInfoArray(params int[] values)
        {
            var csharpArgumentInfoType = Type.GetType(CSharpArgumentInfoTypeName)!;
            var csharpArgumentInfoFlags = Type.GetType(CSharpArgumentInfoFlagsTypeName)!;

            var a = Array.CreateInstance(csharpArgumentInfoType, values.Length);

            for (var i = 0; i < values.Length; i++)
            {
                var createArgumentInfoMethod = csharpArgumentInfoType.GetMethod("Create", [csharpArgumentInfoFlags, typeof(string)])!;
                var arg = createArgumentInfoMethod.Invoke(null, [0, null]);
                a.SetValue(arg, i);
            }

            return a;
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        static void CreateMemberCalls()
        {
            var csharpArgumentInfoType = Type.GetType(CSharpArgumentInfoTypeName, true)!;
            var csharpBinderFlagsType = Type.GetType(CSharpBinderFlagsTypeName, true)!;
            var binderType = Type.GetType(BinderTypeName, true)!;

            var csharpArgumentInfoTypeEnumerableType = typeof(IEnumerable<>).MakeGenericType(csharpArgumentInfoType);

            var getMemberMethod = binderType.GetMethod("GetMember", [csharpBinderFlagsType, typeof(string), typeof(Type), csharpArgumentInfoTypeEnumerableType])!;
            getMemberCall = DelegateFactory.CreateMethodCall<object?>(getMemberMethod);

            var setMemberMethod = binderType.GetMethod("SetMember", [csharpBinderFlagsType, typeof(string), typeof(Type), csharpArgumentInfoTypeEnumerableType])!;
            setMemberCall = DelegateFactory.CreateMethodCall<object?>(setMemberMethod);
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static CallSiteBinder GetMember(string name, Type context)
        {
            Init();
            MiscellaneousUtils.Assert(getMemberCall != null);
            MiscellaneousUtils.Assert(getCSharpArgumentInfoArray != null);
            return (CallSiteBinder) getMemberCall(null, 0, name, context, getCSharpArgumentInfoArray)!;
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static CallSiteBinder SetMember(string name, Type context)
        {
            Init();
            MiscellaneousUtils.Assert(setMemberCall != null);
            MiscellaneousUtils.Assert(setCSharpArgumentInfoArray != null);
            return (CallSiteBinder) setMemberCall(null, 0, name, context, setCSharpArgumentInfoArray)!;
        }
    }

    public static IEnumerable<string> GetDynamicMemberNames(this IDynamicMetaObjectProvider dynamicProvider)
    {
        var metaObject = dynamicProvider.GetMetaObject(Expression.Constant(dynamicProvider));
        return metaObject.GetDynamicMemberNames();
    }
}

[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
class NoThrowGetBinderMember(GetMemberBinder innerBinder) :
    GetMemberBinder(innerBinder.Name, innerBinder.IgnoreCase)
{
    public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
    {
        var retMetaObject = innerBinder.Bind(target, []);

        var noThrowVisitor = new NoThrowExpressionVisitor();
        var resultExpression = noThrowVisitor.Visit(retMetaObject.Expression);

        return new(resultExpression, retMetaObject.Restrictions);
    }
}

[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
class NoThrowSetBinderMember(SetMemberBinder innerBinder) :
    SetMemberBinder(innerBinder.Name, innerBinder.IgnoreCase)
{
    public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject? errorSuggestion)
    {
        var retMetaObject = innerBinder.Bind(target, [value]);

        var noThrowVisitor = new NoThrowExpressionVisitor();
        var resultExpression = noThrowVisitor.Visit(retMetaObject.Expression);

        return new(resultExpression, retMetaObject.Restrictions);
    }
}

class NoThrowExpressionVisitor : ExpressionVisitor
{
    internal static readonly object ErrorResult = new();

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        // if the result of a test is to throw an error, rewrite to result an error result value
        if (node.IfFalse.NodeType == ExpressionType.Throw)
        {
            return Expression.Condition(node.Test, node.IfTrue, Expression.Constant(ErrorResult));
        }

        return base.VisitConditional(node);
    }
}