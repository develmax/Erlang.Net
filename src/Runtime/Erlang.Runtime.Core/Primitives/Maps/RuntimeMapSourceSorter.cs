/*
 * %CopyrightBegin%
 *
 * SPDX-License-Identifier: Apache-2.0
 *
 * Copyright Ericsson AB 1996-2026. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * %CopyrightEnd%
 */
// Modified CLR adaptation of the pinned OTP-29.1.1 source routines.
using System.Buffers.Binary;

namespace Erlang;

internal static unsafe class RuntimeMapSourceSorter
{
    internal static ulong Swizzle(ulong hash)
    {
        ulong swapped = BinaryPrimitives.ReverseEndianness(hash);

        return ((swapped & RuntimeMapSourceSortNames.HighNibbles) >> RuntimeMapSourceSortNames.NibbleBits)
            | ((swapped & (RuntimeMapSourceSortNames.HighNibbles >> RuntimeMapSourceSortNames.NibbleBits)) << RuntimeMapSourceSortNames.NibbleBits);
    }

    internal static uint Mix(ulong key)
    {
        unchecked
        {
            key = ~key + (key << RuntimeMapSourceSortNames.ComplementShift);
            key ^= key >> RuntimeMapSourceSortNames.FirstXorShift;
            key = key + (key << RuntimeMapSourceSortNames.FoldLowShift) + (key << RuntimeMapSourceSortNames.FoldHighShift);
            key ^= key >> RuntimeMapSourceSortNames.SecondXorShift;
            key += key << RuntimeMapSourceSortNames.FinalFoldShift;
            key ^= key >> RuntimeMapSourceSortNames.FinalXorShift;

            return (uint)key;
        }
    }

    internal static void Sort(
        RuntimeMapSourceNode* buffer,
        int start,
        int count,
        IReadOnlyList<KeyValuePair<Term, Term>> pairs,
        RuntimeMapSourceSortKind kind,
        Action<RuntimeMapSourceSortEvent>? observer
    )
    {
        delegate* managed<RuntimeMapSourceNode, RuntimeMapSourceNode, IReadOnlyList<KeyValuePair<Term, Term>>, int> compare =
            kind == RuntimeMapSourceSortKind.Hash ? &CompareHash : &CompareKey;
        uint seed = count > RuntimeMapSourceSortNames.ImprovedSeedThreshold
            ? unchecked((uint)Random.Shared.NextInt64((long)uint.MaxValue + 1))
            : RuntimeMapSourceSortNames.InitialSeed;
        var pending = new Stack<RuntimeMapSourceSortFrame>();
        pending.Push(new(start, count, seed));
        while (pending.TryPop(out var frame))
        {
            int first = frame.Start;
            int end = first + frame.Count;
            if (frame.Count <= RuntimeMapSourceSortNames.SelectionCutoff)
            {
                // The upstream helper named insertion_sort actually scans for
                // the minimum and swaps it into each successive first slot.
                for (int position = first; position + 1 < end; position++)
                {
                    int minimum = position;
                    for (int current = position + 1; current < end; current++)
                        if (compare(buffer[current], buffer[minimum], pairs) < 0)
                            minimum = current;
                    Swap(buffer, position, minimum);
                }
                continue;
            }

            ulong address = (ulong)(nuint)(buffer + first);
            ulong comparer = (ulong)(nuint)compare;
            int pivot = (int)(Mix(unchecked(frame.Seed + comparer + address - (ulong)frame.Count)) % (uint)frame.Count);
            observer?.Invoke(new(
                kind,
                address,
                comparer,
                first,
                frame.Count,
                frame.Seed,
                pivot
            ));
            Swap(buffer, first, first + pivot);
            int currentIndex = first + 1;
            int secondStart = end;
            bool duplicatePivot = false;
            while (currentIndex != secondStart)
            {
                int result = compare(buffer[currentIndex], buffer[first], pairs);
                if (result < 0)
                    currentIndex++;
                else if (result == 0)
                {
                    duplicatePivot = true;
                    break;
                }
                else
                    Swap(buffer, currentIndex, --secondStart);
            }

            int pivotStart;
            int pivotEnd;
            if (!duplicatePivot)
            {
                pivotStart = secondStart - 1;
                pivotEnd = secondStart;
                Swap(buffer, first, pivotStart);
            }
            else
            {
                pivotStart = currentIndex - 1;
                pivotEnd = currentIndex + 1;
                int lastStart = secondStart;
                Swap(buffer, first, pivotStart);
                while (pivotEnd != lastStart)
                {
                    int result = compare(buffer[pivotEnd], buffer[pivotEnd - 1], pairs);
                    if (result == 0)
                        pivotEnd++;
                    else if (result < 0)
                    {
                        Swap(buffer, pivotStart, pivotEnd);
                        pivotStart++;
                        pivotEnd++;
                    }
                    else
                        Swap(buffer, pivotEnd, --lastStart);
                }
            }

            uint nextSeed = unchecked(frame.Seed + 1);
            // Preserve source left-then-right traversal without native recursion.
            pending.Push(new(pivotEnd, end - pivotEnd, nextSeed));
            pending.Push(new(first, pivotStart - first, nextSeed));
        }
    }

    private static int CompareHash(
        RuntimeMapSourceNode left,
        RuntimeMapSourceNode right,
        IReadOnlyList<KeyValuePair<Term, Term>> pairs
    ) => right.Hash.CompareTo(left.Hash);

    private static int CompareKey(
        RuntimeMapSourceNode left,
        RuntimeMapSourceNode right,
        IReadOnlyList<KeyValuePair<Term, Term>> pairs
    ) => TermOrder.Compare(pairs[(int)left.PairIndex].Key, pairs[(int)right.PairIndex].Key, true);

    private static void Swap(RuntimeMapSourceNode* buffer, int first, int second)
    {
        if (first == second)
            return;
        RuntimeMapSourceNode value = buffer[first];
        buffer[first] = buffer[second];
        buffer[second] = value;
    }
}
