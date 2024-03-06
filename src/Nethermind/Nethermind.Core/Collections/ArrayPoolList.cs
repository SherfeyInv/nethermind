// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Nethermind.Core.Collections;

public sealed class ArrayPoolList<T> : IList<T>, IList, IOwnedReadOnlyList<T>
{
    private readonly ArrayPool<T> _arrayPool;
    private T[] _array;
    private int _count = 0;
    private int _capacity;
    private bool _disposed;

    public ArrayPoolList(int capacity) : this(ArrayPool<T>.Shared, capacity) { }

    public ArrayPoolList(int capacity, int count) : this(ArrayPool<T>.Shared, capacity, count) { }

    public ArrayPoolList(int capacity, IEnumerable<T> enumerable) : this(capacity) => this.AddRange(enumerable);

    public ArrayPoolList(ArrayPool<T> arrayPool, int capacity, int startingCount = 0)
    {
        _arrayPool = arrayPool;

        if (capacity != 0)
        {
            _array = arrayPool.Rent(capacity);
            _array.AsSpan(0, startingCount).Clear();
        }
        else
        {
            _array = Array.Empty<T>();
        }
        _capacity = _array.Length;

        _count = startingCount;
    }

    ReadOnlySpan<T> IOwnedReadOnlyList<T>.AsSpan()
    {
        return AsSpan();
    }

    public IEnumerator<T> GetEnumerator()
    {
        GuardDispose();
        return new ArrayPoolListEnumerator(_array, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GuardDispose()
    {
        if (_disposed)
        {
            ThrowObjectDisposed();
        }

        [DoesNotReturn]
        static void ThrowObjectDisposed()
        {
            throw new ObjectDisposedException(nameof(ArrayPoolList<T>));
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(T item)
    {
        GuardResize();
        _array[_count++] = item;
    }

    int IList.Add(object? value)
    {
        ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, nameof(value));

        Add((T)value!);

        return Count - 1;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        GuardResize(items.Length);
        items.CopyTo(_array.AsSpan(_count, items.Length));
        _count += items.Length;
    }

    public void Clear() => _count = 0;

    public bool Contains(T item)
    {
        GuardDispose();
        int indexOf = Array.IndexOf(_array, item);
        return indexOf >= 0 && indexOf < _count;
    }

    bool IList.Contains(object? value) => IsCompatibleObject(value) && Contains((T)value!);

    public void CopyTo(T[] array, int arrayIndex)
    {
        GuardDispose();
        _array.AsMemory(0, _count).CopyTo(array.AsMemory(arrayIndex));
    }

    void ICollection.CopyTo(Array array, int index)
    {
        if ((array is not null) && (array.Rank != 1))
            throw new ArgumentException("Only single dimensional arrays are supported.", nameof(array));

        GuardDispose();

        Array.Copy(_array, 0, array!, index, _count);
    }

    public int Count
    {
        get
        {
            GuardDispose();
            return _count;
        }
    }

    public int Capacity => _capacity;

    bool IList.IsFixedSize => false;

    bool ICollection<T>.IsReadOnly => false;

    bool IList.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    public int IndexOf(T item)
    {
        GuardDispose();
        int indexOf = Array.IndexOf(_array, item);
        return indexOf < _count ? indexOf : -1;
    }

    int IList.IndexOf(object? value) => IsCompatibleObject(value) ? IndexOf((T)value!) : -1;

    public void Insert(int index, T item)
    {
        GuardResize();
        GuardIndex(index, allowEqualToCount: true);
        _array.AsMemory(index, _count - index).CopyTo(_array.AsMemory(index + 1));
        _array[index] = item;
        _count++;
    }

    void IList.Insert(int index, object? value)
    {
        ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, nameof(value));

        Insert(index, (T)value!);
    }

    private void GuardResize(int itemsToAdd = 1)
    {
        GuardDispose();
        int newCount = _count + itemsToAdd;
        if (_capacity == 0)
        {
            _array = _arrayPool.Rent(newCount);
            _capacity = _array.Length;
        }
        else if (newCount > _capacity)
        {
            int newCapacity = _capacity * 2;
            if (newCapacity == 0) newCapacity = 1;
            while (newCount > newCapacity)
            {
                newCapacity *= 2;
            }
            T[] newArray = _arrayPool.Rent(newCapacity);
            _array.CopyTo(newArray, 0);
            T[] oldArray = Interlocked.Exchange(ref _array, newArray);
            _capacity = newArray.Length;
            _arrayPool.Return(oldArray);
        }
    }

    public bool Remove(T item) => RemoveAtInternal(IndexOf(item), false);

    void IList.Remove(object? value)
    {
        if (IsCompatibleObject(value))
            Remove((T)value!);
    }

    public void RemoveAt(int index) => RemoveAtInternal(index, true);

    private bool RemoveAtInternal(int index, bool shouldThrow)
    {
        bool isValid = GuardIndex(index, shouldThrow);
        if (isValid)
        {
            int start = index + 1;
            if (start < _count)
            {
                _array.AsMemory(start, _count - index).CopyTo(_array.AsMemory(index));
            }

            _count--;
        }

        return isValid;
    }

    public void Truncate(int newLength)
    {
        GuardDispose();
        GuardIndex(newLength, allowEqualToCount: true);
        _count = newLength;
    }

    public T this[int index]
    {
        get
        {
            GuardIndex(index);
            return _array[index];
        }
        set
        {
            GuardIndex(index);
            _array[index] = value;
        }
    }

    object? IList.this[int index]
    {
        get => this[index];
        set
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, nameof(value));

            this[index] = (T)value!;
        }
    }

    private bool GuardIndex(int index, bool shouldThrow = true, bool allowEqualToCount = false)
    {
        GuardDispose();
        int count = _count;
        if ((uint)index > (uint)count || (!allowEqualToCount && index == count))
        {
            if (shouldThrow)
            {
                ThrowArgumentOutOfRangeException();
            }
            return false;
        }

        return true;

        [DoesNotReturn]
        static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private static bool IsCompatibleObject(object? value) => value is T || value is null && default(T) is null;

    public static ArrayPoolList<T> Empty() => new(0);

    private struct ArrayPoolListEnumerator(T[] array, int count) : IEnumerator<T>
    {
        private int _index = -1;

        public bool MoveNext() => ++_index < count;

        public void Reset() => _index = -1;

        public readonly T Current => array[_index];

        readonly object IEnumerator.Current => Current!;

        public readonly void Dispose() { }
    }

    public void Dispose()
    {
        // Noop for empty array as sometimes this is used as part of an empty shared response.
        if (_capacity == 0) return;

        if (!_disposed)
        {
            _disposed = true;
            T[]? array = _array;
            if (array is not null)
            {
                _arrayPool.Return(_array);
                _array = null!;
            }
        }

#if DEBUG
        GC.SuppressFinalize(this);
#endif
    }

#if DEBUG
    private readonly StackTrace _creationStackTrace = new();

    ~ArrayPoolList()
    {
        if (_capacity != 0 && !_disposed)
        {
            throw new InvalidOperationException($"{nameof(ArrayPoolList<T>)} hasn't been disposed. Created {_creationStackTrace}");
        }
    }
#endif

    public Span<T> AsSpan() => _array.AsSpan(0, _count);
}
