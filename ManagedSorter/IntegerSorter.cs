using System;

namespace SortingPlugins
{
    // Compiled outside Unity and imported as ManagedSorter.dll.
    public static class IntegerSorter
    {
        public static void TestSort(int[] values, int length)
        {
            Array.Sort(values, 0, length);
        }
    }
}
