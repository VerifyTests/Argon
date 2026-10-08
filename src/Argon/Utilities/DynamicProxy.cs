// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

class DynamicProxy<T>
{
    public virtual IEnumerable<string> GetDynamicMemberNames(T instance) => [];

    public virtual bool TryBinaryOperation(T instance, BinaryOperationBinder binder, object arg, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TryConvert(T instance, ConvertBinder binder, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TryCreateInstance(T instance, CreateInstanceBinder binder, object[] args, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TryDeleteIndex(T instance, DeleteIndexBinder binder, object[] indexes) =>
        false;

    public virtual bool TryDeleteMember(T instance, DeleteMemberBinder binder) =>
        false;

    public virtual bool TryGetIndex(T instance, GetIndexBinder binder, object[] indexes, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TryGetMember(T instance, GetMemberBinder binder, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TryInvoke(T instance, InvokeBinder binder, object[] args, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TryInvokeMember(T instance, InvokeMemberBinder binder, object[] args, out object? result)
    {
        result = null;
        return false;
    }

    public virtual bool TrySetIndex(T instance, SetIndexBinder binder, object[] indexes, object value) =>
        false;

    public virtual bool TrySetMember(T instance, SetMemberBinder binder, object value) =>
        false;

    public virtual bool TryUnaryOperation(T instance, UnaryOperationBinder binder, out object? result)
    {
        result = null;
        return false;
    }
}