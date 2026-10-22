namespace Erlang;

internal static class ListModuleRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<IReadOnlyList<Term>, Term> body) => registry.Register(
            ListModuleNames.Module,
            name,
            arity,
            (_, arguments) => ValueTask.FromResult(body(arguments))
        );
        Add(ListModuleNames.Concat, 1, a => ListBasicOperations.Concat(a[0]));
        Add(ListModuleNames.Delete, 2, a => ListBasicOperations.Delete(a[0], a[1]));
        Add(ListModuleNames.DropLast, 1, a => ListBasicOperations.DropLast(a[0]));
        Add(ListModuleNames.Enumerate, 1, a => ListBasicOperations.Enumerate(a[0]));
        Add(ListModuleNames.Enumerate, 2, a => ListBasicOperations.Enumerate(a[1], a[0]));
        Add(ListModuleNames.Enumerate, 3, a => ListBasicOperations.Enumerate(a[2], a[0], a[1]));
        Add(ListModuleNames.FlatLength, 1, a => ListBasicOperations.FlatLength(a[0]));
        Add(ListModuleNames.Join, 2, a => ListBasicOperations.Join(a[0], a[1]));
        Add(ListModuleNames.Maximum, 1, a => ListBasicOperations.Extreme(a[0], true));
        Add(ListModuleNames.Minimum, 1, a => ListBasicOperations.Extreme(a[0], false));
        Add(ListModuleNames.Prefix, 2, a => ListBasicOperations.Prefix(a[0], a[1]));
        Add(ListModuleNames.Suffix, 2, a => ListBasicOperations.Suffix(a[0], a[1]));
        Add(ListModuleNames.Sublist, 2, a => ListBasicOperations.Sublist(a[0], a[1]));
        Add(ListModuleNames.Sublist, 3, a => ListBasicOperations.Sublist(a[0], a[2], a[1]));
        Add(ListModuleNames.Subtract, 2, a => ListBasicOperations.Subtract(a[0], a[1]));
        Add(ListModuleNames.Unique, 1, a => ListExtraFunctions.Unique(a[0]));
        Add(ListModuleNames.Unzip, 1, a => ListZipOperations.Unzip(a[0], 2));
        Add(ListModuleNames.UnzipThree, 1, a => ListZipOperations.Unzip(a[0], 3));
        foreach (string operation in new[] { ListModuleNames.KeyDelete, ListModuleNames.KeyTake })
            Add(operation, 3, a => ListKeyOperations.Change(
                operation,
                a[0],
                a[1],
                a[2]
            ));
        foreach (string operation in new[] { ListModuleNames.KeyReplace, ListModuleNames.KeyStore })
            Add(operation, 4, a => ListKeyOperations.Change(
                operation,
                a[0],
                a[1],
                a[2],
                a[3]
            ));
        registry.Register(
            ListModuleNames.Module,
            ListModuleNames.KeyMap,
            3,
            (c, a) => ListKeyOperations.Map(
                c,
                a[0],
                a[1],
                a[2]
            )
        );
        foreach (string operation in new[] { ListModuleNames.Filter, ListModuleNames.FlatMap, ListModuleNames.Search, ListModuleNames.SplitWith, ListModuleNames.Unique })
            registry.Register(
                ListModuleNames.Module,
                operation,
                2,
                (c, a) => ListExtraFunctions.Run(
                    c,
                    operation,
                    a[0],
                    a[1]
                )
            );
        registry.Register(
            ListModuleNames.Module,
            ListModuleNames.FilterMapLegacy,
            2,
            (c, a) => ListCallbackSourceExecution.LegacyFilterMap(c, a[0], a[1])
        );
        Add(ListModuleNames.Sort, 1, a => ListSortOperations.Sort(a[0]));
        registry.Register(
            ListModuleNames.Module,
            ListModuleNames.UniqueSort,
            1,
            (context, arguments) => ListSortOperations.Unique(context, arguments[0])
        );
        registry.Register(
            ListModuleNames.Module,
            ListModuleNames.UniqueSort,
            2,
            (c, a) => ComparatorUniqueSort.Sort(c, a[0], a[1])
        );
        foreach (string operation in new[] { ListModuleNames.KeySort, ListModuleNames.UniqueKeySort })
            Add(operation, 2, a => ListSortOperations.Sort(a[1], operation == ListModuleNames.UniqueKeySort, a[0]));
        foreach (string operation in new[] { ListModuleNames.Merge, ListModuleNames.UniqueMerge })
            registry.Register(
                ListModuleNames.Module,
                operation,
                1,
                (c, a) => ListMergeOperations.MergeMany(c, a[0], operation == ListModuleNames.UniqueMerge)
            );
        foreach (string operation in new[] { ListModuleNames.Merge, ListModuleNames.ReverseMerge, ListModuleNames.UniqueMerge, ListModuleNames.ReverseUniqueMerge })
        {
            bool reverse = operation == ListModuleNames.ReverseMerge || operation == ListModuleNames.ReverseUniqueMerge;
            bool unique = operation == ListModuleNames.UniqueMerge || operation == ListModuleNames.ReverseUniqueMerge;
            registry.Register(
                ListModuleNames.Module,
                operation,
                2,
                (c, a) => ListMergeOperations.Merge(
                    c,
                    a[0],
                    a[1],
                    reverse,
                    unique
                )
            );
            registry.Register(
                ListModuleNames.Module,
                operation,
                3,
                (c, a) => ListMergeOperations.Merge(
                    c,
                    a[1],
                    a[2],
                    reverse,
                    unique,
                    a[0]
                )
            );
        }
        foreach (string operation in new[] { ListModuleNames.KeyMerge, ListModuleNames.ReverseKeyMerge, ListModuleNames.UniqueKeyMerge, ListModuleNames.ReverseUniqueKeyMerge })
        {
            bool reverse = operation == ListModuleNames.ReverseKeyMerge || operation == ListModuleNames.ReverseUniqueKeyMerge;
            bool unique = operation == ListModuleNames.UniqueKeyMerge || operation == ListModuleNames.ReverseUniqueKeyMerge;
            registry.Register(
                ListModuleNames.Module,
                operation,
                3,
                (c, a) => ListMergeOperations.Merge(
                    c,
                    a[1],
                    a[2],
                    reverse,
                    unique,
                    position: a[0]
                )
            );
        }
        foreach (string operation in new[] { ListModuleNames.MergeThree, ListModuleNames.ReverseMergeThree, ListModuleNames.UniqueMergeThree, ListModuleNames.ReverseUniqueMergeThree })
            registry.Register(
                ListModuleNames.Module,
                operation,
                3,
                (c, a) => ListMergeOperations.MergeThree(
                    c,
                    a[0],
                    a[1],
                    a[2],
                    operation == ListModuleNames.ReverseMergeThree || operation == ListModuleNames.ReverseUniqueMergeThree,
                    operation == ListModuleNames.UniqueMergeThree || operation == ListModuleNames.ReverseUniqueMergeThree
                )
            );
        foreach (string operation in new[] { ListModuleNames.Zip, ListModuleNames.ZipThree })
        {
            int count = operation == ListModuleNames.Zip ? 2 : 3;
            registry.Register(
                ListModuleNames.Module,
                operation,
                count,
                (c, a) => ListZipOperations.Zip(c, a)
            );
            registry.Register(
                ListModuleNames.Module,
                operation,
                count + 1,
                (c, a) => ListZipOperations.Zip(c, a.Take(count).ToArray(), a[count])
            );
        }
        foreach (string operation in new[] { ListModuleNames.ZipWith, ListModuleNames.ZipWithThree })
        {
            int count = operation == ListModuleNames.ZipWith ? 2 : 3;
            registry.Register(
                ListModuleNames.Module,
                operation,
                count + 1,
                (c, a) => ListZipOperations.Zip(c, a.Skip(1).ToArray(), function: a[0])
            );
            registry.Register(
                ListModuleNames.Module,
                operation,
                count + 2,
                (c, a) => ListZipOperations.Zip(
                    c,
                    a.Skip(1).Take(count).ToArray(),
                    a[count + 1],
                    a[0]
                )
            );
        }
    }
}
