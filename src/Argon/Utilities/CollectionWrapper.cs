// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#nullable disable

namespace Argon;

class CollectionWrapper<T> : ICollection<T>, IWrappedCollection
{
    readonly IList list;
    readonly ICollection<T> genericCollection;

    public CollectionWrapper(IList list)
    {
        if (list is ICollection<T> collection)
        {
            genericCollection = collection;
        }
        else
        {
            this.list = list;
        }
    }

    public CollectionWrapper(ICollection<T> list) =>
        genericCollection = list;

    public virtual void Add(T item)
    {
        if (genericCollection == null)
        {
            list!.Add(item);
        }
        else
        {
            genericCollection.Add(item);
        }
    }

    public virtual void Clear()
    {
        if (genericCollection == null)
        {
            list!.Clear();
        }
        else
        {
            genericCollection.Clear();
        }
    }

    public virtual bool Contains(T item)
    {
        if (genericCollection != null)
        {
            return genericCollection.Contains(item);
        }

        return list!.Contains(item);
    }

    public virtual void CopyTo(T[] array, int arrayIndex)
    {
        if (genericCollection == null)
        {
            list!.CopyTo(array, arrayIndex);
        }
        else
        {
            genericCollection.CopyTo(array, arrayIndex);
        }
    }

    public virtual int Count
    {
        get
        {
            if (genericCollection == null)
            {
                return list!.Count;
            }

            return genericCollection.Count;
        }
    }

    public virtual bool IsReadOnly
    {
        get
        {
            if (genericCollection == null)
            {
                return list!.IsReadOnly;
            }

            return genericCollection.IsReadOnly;
        }
    }

    public virtual bool Remove(T item)
    {
        if (genericCollection != null)
        {
            return genericCollection.Remove(item);
        }

        var contains = list!.Contains(item);

        if (contains)
        {
            list!.Remove(item);
        }

        return contains;
    }

    public virtual IEnumerator<T> GetEnumerator() =>
        (genericCollection ?? list.Cast<T>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable) genericCollection! ?? list!).GetEnumerator();

    int IList.Add(object value)
    {
        VerifyValueType(value);
        Add((T) value);

        return Count - 1;
    }

    bool IList.Contains(object value) =>
        IsCompatibleObject(value) &&
        Contains((T) value);

    int IList.IndexOf(object value)
    {
        if (genericCollection != null)
        {
            throw new InvalidOperationException("Wrapped ICollection<T> does not support IndexOf.");
        }

        if (IsCompatibleObject(value))
        {
            return list!.IndexOf((T) value);
        }

        return -1;
    }

    void IList.RemoveAt(int index)
    {
        if (genericCollection != null)
        {
            throw new InvalidOperationException("Wrapped ICollection<T> does not support RemoveAt.");
        }

        list!.RemoveAt(index);
    }

    void IList.Insert(int index, object value)
    {
        if (genericCollection != null)
        {
            throw new InvalidOperationException("Wrapped ICollection<T> does not support Insert.");
        }

        VerifyValueType(value);
        list!.Insert(index, (T) value);
    }

    bool IList.IsFixedSize
    {
        get
        {
            if (genericCollection != null)
            {
                // ICollection<T> only has IsReadOnly
                return genericCollection.IsReadOnly;
            }

            return list!.IsFixedSize;
        }
    }

    void IList.Remove(object value)
    {
        if (IsCompatibleObject(value))
        {
            Remove((T) value);
        }
    }

    object IList.this[int index]
    {
        get
        {
            if (genericCollection != null)
            {
                throw new InvalidOperationException("Wrapped ICollection<T> does not support indexer.");
            }

            return list![index];
        }
        set
        {
            if (genericCollection != null)
            {
                throw new InvalidOperationException("Wrapped ICollection<T> does not support indexer.");
            }

            VerifyValueType(value);
            list![index] = (T) value;
        }
    }

    void ICollection.CopyTo(Array array, int arrayIndex) =>
        CopyTo((T[]) array, arrayIndex);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot
    {
        get
        {
            if (field == null)
            {
                Interlocked.CompareExchange(ref field, new(), null);
            }

            return field;
        }
    }

    static void VerifyValueType(object value)
    {
        if (!IsCompatibleObject(value))
        {
            throw new ArgumentException($"The value '{value}' is not of type '{typeof(T)}' and cannot be used in this generic collection.", nameof(value));
        }
    }

    static bool IsCompatibleObject(object value) =>
        value is T ||
        (value == null && (!typeof(T).IsValueType || typeof(T).IsNullableType()));

    public object UnderlyingCollection => (object) genericCollection! ?? list!;
}