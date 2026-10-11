using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SortComparison : MonoBehaviour
{
    [Header("Drag your Random.org numbers.txt here")]
    [SerializeField] private TextAsset numbersFile;

    [Header("Repeated trials use fresh copies of the original numbers")]
    [SerializeField, Range(1, 1000)] private int trials = 100;

    [DllImport("NativeSorter", EntryPoint = "TestSort",
        CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void NativeTestSort([In, Out] int[] values, int length);

    private void Start()
    {
        RunComparison();
    }

    private void RunComparison()
    {
        try
        {
            int[] original = ReadNumbers();
            int[] expected = (int[])original.Clone();
            Array.Sort(expected);

            Action<int[], int>[] sorters =
            {
                NativeTestSort,
                SortingPlugins.IntegerSorter.TestSort,
                MonoBehaviourTestSort
            };
            string[] names = { "Native C++ DLL", "Managed C# DLL", "MonoBehaviour C#" };
            long[] totalTicks = new long[3];
            int runCount = Mathf.Clamp(trials, 1, 1000);
            var timer = new Stopwatch();

            // Warm up DLL loading/JIT before measuring steady-state sort times.
            for (int method = 0; method < sorters.Length; method++)
            {
                int[] warmup = (int[])original.Clone();
                sorters[method](warmup, warmup.Length);
                Verify(warmup, expected, names[method]);
            }

            // Reuse a buffer; copy, validation and logging are outside timing.
            int[] working = new int[original.Length];
            for (int trial = 0; trial < runCount; trial++)
            {
                // Rotate which method runs first to reduce order bias.
                for (int position = 0; position < sorters.Length; position++)
                {
                    int method = (trial + position) % sorters.Length;
                    Array.Copy(original, working, original.Length);
                    timer.Restart();
                    sorters[method](working, working.Length);
                    timer.Stop();
                    totalTicks[method] += timer.ElapsedTicks;
                    Verify(working, expected, names[method]);
                }
            }

            Debug.Log($"SORT COMPARISON | {original.Length:N0} unique Random.org integers | " +
                $"{runCount} trials per method | {Application.unityVersion} | " +
                $"{Application.platform} | {(IntPtr.Size * 8)}-bit");
            for (int method = 0; method < names.Length; method++)
            {
                double totalMs = totalTicks[method] * 1000.0 / Stopwatch.Frequency;
                Debug.Log($"{names[method]} took {totalMs / runCount:F6} ms per sort " +
                    $"(average); {totalMs:F3} ms total over {runCount} sorts. Validation: PASS.");
            }
            Debug.Log("All three methods returned identical, ascending results. " +
                "Copying, parsing, warm-up, validation and Console logging were excluded from timing.");
        }
        catch (DllNotFoundException)
        {
            Debug.LogError("NativeSorter.dll could not load. Build Release x64, copy it into " +
                "Assets/Plugins/x86_64, enable Windows Editor x86_64 in its import settings, " +
                "and restart Unity. This kit targets the Windows 64-bit Editor.");
        }
        catch (EntryPointNotFoundException)
        {
            Debug.LogError("NativeSorter.dll loaded but TestSort was not found. Build the supplied " +
                "C++ source with extern C and dllexport, replace the DLL, and restart Unity.");
        }
        catch (BadImageFormatException)
        {
            Debug.LogError("DLL architecture mismatch. Build NativeSorter as Release x64, not Win32.");
        }
        catch (Exception exception)
        {
            Debug.LogError("Sort comparison stopped: " + exception.Message);
        }
    }

    // Method 3: the sorting function lives directly in a MonoBehaviour.
    private void MonoBehaviourTestSort(int[] values, int length)
    {
        Array.Sort(values, 0, length);
    }

    private int[] ReadNumbers()
    {
        if (numbersFile == null)
            throw new InvalidOperationException("Assign numbers.txt to Numbers File in the Inspector.");

        // Accept comma-separated or whitespace-separated numbers, optionally in braces.
        char[] separators = { ',', ' ', '\t', '\r', '\n', '{', '}', '\uFEFF' };
        string[] tokens = numbersFile.text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 10000)
            throw new InvalidOperationException($"Expected exactly 10,000 integers; found {tokens.Length}. " +
                "Copy only the generated numbers, without the page title or set label.");

        int[] values = new int[tokens.Length];
        var unique = new HashSet<int>();
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]) ||
                values[i] < 1000000 || values[i] > 1000000000)
                throw new InvalidOperationException($"Invalid integer at position {i + 1}. " +
                    "Use values from 1,000,000 through 1,000,000,000 without thousands separators.");
            if (!unique.Add(values[i]))
                throw new InvalidOperationException($"Duplicate integer: {values[i]}. Generate unique integers.");
        }
        return values;
    }

    private static void Verify(int[] actual, int[] expected, string methodName)
    {
        for (int i = 0; i < expected.Length; i++)
            if (actual[i] != expected[i])
                throw new InvalidOperationException(methodName + " returned an incorrect result.");
    }
}
