#include "NativeSorter.h"
#include <algorithm>

static_assert(sizeof(int) == 4, "C# int requires a 32-bit native int.");

extern "C" SORT_API void SORT_CALL TestSort(int* values, int length)
{
    if (values == nullptr || length <= 1)
        return;

    std::sort(values, values + length);
}
