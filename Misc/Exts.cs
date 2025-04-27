using System;
using UnityEngine;

namespace BrandLab360
{
    public static class Exts
    {
        public static T OrDefaultWhen<T>(this T obj, Func<T, bool> predicate)
        {
            return predicate(obj) ? default(T) : obj;
        }


    }
}