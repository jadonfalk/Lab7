#pragma once

// Export an unmangled function name that C# can find with DllImport.
#if defined(_WIN32)
#define SORT_API __declspec(dllexport)
#define SORT_CALL __cdecl
#else
#define SORT_API __attribute__((visibility("default")))
#define SORT_CALL
#endif

extern "C" SORT_API void SORT_CALL TestSort(int* values, int length);
