using System;
using System.Collections.Generic;

public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
{
    private readonly int equalReturn;

    public DuplicateKeyComparer(bool EqualValueAtEnd=false)
    {
        equalReturn=EqualValueAtEnd?-1:1;
    }

    public int Compare(TKey x, TKey y)
    {
        int result = x.CompareTo(y);
        return result == 0 ? equalReturn : result;
    }
}