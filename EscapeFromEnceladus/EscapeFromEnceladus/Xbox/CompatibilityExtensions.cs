using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Common;

namespace Arena.Xbox {

    // Just here to make this namespace non-empty on windows
    public static class Dummy {
    }

#if XBOX
    public static class CompatibilityExtensions {
        public static void RemoveAll<T>(this List<T> theList, Func<T, bool> predicate) {
            List<T> entitiesToRemove = theList.Where(predicate).ToList();
            entitiesToRemove.ForEach(e => theList.Remove(e));
        }

        public static void UnionWith<T>(this HashSet<T> set, IEnumerable<T> other) {
            foreach (T o in other) {
                set.Add(o);
            }
        }
    }

#endif
}
