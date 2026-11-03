/*
 * %CopyrightBegin%
 *
 * SPDX-License-Identifier: Apache-2.0
 *
 * Copyright Ericsson AB 2014-2025. All Rights Reserved.
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
 *
 * hashmaps are an adaption of Rich Hickeys Persistent HashMaps
 *   which were an adaption of Phil Bagwells - Hash Array Mapped Tries
 *
 * Author: Björn-Egil Dahlberg
 */
// Modified CLR adaptation of the pinned OTP-29.1.1 source routines.
namespace Erlang;

internal sealed partial class HamtIndexOwner429
{
    internal unsafe MapTerm BuildSourceBulk(
        IReadOnlyList<KeyValuePair<Term, Term>> pairs,
        Action<RuntimeMapSourceSortEvent>? observer
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pairs.Count, HamtViewLayout.ReleaseFlatMapLimit);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);
            builders = checked(builders + 1);
        }

        HamtHashLeases429? unpublished = null;
        try
        {
            // The existing owner protects Close, while hashing, key comparison
            // and all diagnostic callbacks execute outside its admission gate.
            using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
            var nodes = new RuntimeMapSourceNode[pairs.Count];
            for (int index = 0; index < nodes.Length; index++)
            {
                nodes[index].Hash = RuntimeMapSourceSorter.Swizzle(hashes.Hash(pairs[index].Key));
                nodes[index].Skip = 1;
                nodes[index].Ordinal = (ulong)index;
                nodes[index].PairIndex = (ulong)index;
            }

            int count;
            fixed (RuntimeMapSourceNode* buffer = nodes)
            {
                RuntimeMapSourceSorter.Sort(
                    buffer,
                    0,
                    nodes.Length,
                    pairs,
                    RuntimeMapSourceSortKind.Hash,
                    observer
                );
                count = CompactSourceBulk(
                    buffer,
                    nodes.Length,
                    pairs,
                    observer
                );
            }
            HamtViewEntry[] compacted = nodes.Take(count).Select(node => new HamtViewEntry(
                pairs[(int)node.PairIndex],
                RuntimeMapSourceSorter.Swizzle(node.Hash)
            )).ToArray();

            if (count <= HamtViewLayout.ReleaseFlatMapLimit)
            {
                // Even a bulk input that shrinks to a flat map first traverses
                // its constructed HAMT. The validating flat sorter stops at
                // the first CMP-zero pair; this caller ignores its false result.
                HamtViewNode temporary = HamtCursorView.Branch(
                    compacted,
                    0,
                    null,
                    null,
                    true
                );
                var physical = new List<KeyValuePair<Term, Term>>();
                CollectSourceRows(temporary, physical);
                ValidateSourceFlat(physical);
                MapTerm flat = MapTerm.FromSourceFlatPairs(physical);
                lock (gate) ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);

                return flat;
            }

            unpublished = hashes.TransferHamtLeases429(this);
            HamtViewNode root = HamtCursorView.Branch(
                compacted,
                0,
                Domain,
                unpublished,
                true
            );
            var rows = new List<KeyValuePair<Term, Term>>();
            CollectSourceRows(root, rows);
            MapTerm result = NativeMapMaterialization505.BuildSourceBulk(
                runtime,
                rows,
                root,
                unpublished
            );
            lock (gate) ObjectDisposedException.ThrowIf(closing, HamtIndexNames429.Closed);
            unpublished = null;

            return result;
        }
        finally
        {
            try
            {
                unpublished?.Dispose();
            }
            finally
            {
                lock (gate) builders--;
                TrySettle();
            }
        }
    }

    private static unsafe int CompactSourceBulk(
        RuntimeMapSourceNode* nodes,
        int count,
        IReadOnlyList<KeyValuePair<Term, Term>> pairs,
        Action<RuntimeMapSourceSortEvent>? observer
    )
    {
        int read = 0;
        int write = 0;
        while (read < count - 1)
        {
            if (nodes[read].Hash == nodes[read + 1].Hash)
            {
                int end = read + 2;
                while (end < count && nodes[read].Hash == nodes[end].Hash)
                    end++;
                RuntimeMapSourceSorter.Sort(
                    nodes,
                    read,
                    end - read,
                    pairs,
                    RuntimeMapSourceSortKind.Key,
                    observer
                );
                while (read < end)
                {
                    int selected = read;
                    while (++read < end && pairs[(int)nodes[read].PairIndex].Key.Equals(pairs[(int)nodes[selected].PairIndex].Key))
                        if (nodes[read].Ordinal > nodes[selected].Ordinal)
                            selected = read;
                    // The C routine copies only hx and val. Ordinal is consulted
                    // solely in the original region, before that region retires.
                    nodes[write].Hash = nodes[selected].Hash;
                    nodes[write].PairIndex = nodes[selected].PairIndex;
                    write++;
                }
                read = end;
                continue;
            }
            if (read > write)
            {
                nodes[write].Hash = nodes[read].Hash;
                nodes[write].PairIndex = nodes[read].PairIndex;
            }
            write++;
            read++;
        }
        if (read < count)
        {
            nodes[write].Hash = nodes[read].Hash;
            nodes[write].PairIndex = nodes[read].PairIndex;
            write++;
        }

        return write;
    }

    private static void CollectSourceRows(HamtViewNode node, List<KeyValuePair<Term, Term>> rows)
    {
        if (node.Leaf is { } pair)
        {
            rows.Add(pair);

            return;
        }
        foreach (HamtViewNode child in node.Children)
            CollectSourceRows(child, rows);
    }

    private static void ValidateSourceFlat(List<KeyValuePair<Term, Term>> pairs)
    {
        for (int index = 1; index < pairs.Count; index++)
        {
            int position = index;
            while (position > 0)
            {
                Term current = pairs[position].Key;
                Term previous = pairs[position - 1].Key;
                int comparison = current is Atom first && previous is Atom second
                    ? AtomNameRegistry.IndexOf(first.Name).CompareTo(AtomNameRegistry.IndexOf(second.Name))
                    : TermOrder.Compare(current, previous, true);
                if (comparison > 0)
                    break;
                if (comparison == 0)
                    return;
                (pairs[position], pairs[position - 1]) = (pairs[position - 1], pairs[position]);
                position--;
            }
        }
    }
}
