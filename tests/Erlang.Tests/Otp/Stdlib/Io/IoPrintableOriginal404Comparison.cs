using Erlang;

namespace Erlang.Tests;

internal static class IoPrintableOriginal404Comparison
{
    internal static int Rows(
        Term expected,
        Term actual,
        object scope,
        int requiredRows,
        List<object> differences,
        ref int examined
    )
    {
        Term[] left = IoPrintableOriginal404Terms.List(expected, out bool properLeft);
        Term[] right = IoPrintableOriginal404Terms.List(actual, out bool properRight);
        if (!properLeft || !properRight || left.Length != requiredRows || right.Length != requiredRows)
            differences.Add(new
            {
                Scope = scope,
                ProperOriginal = properLeft,
                ProperActual = properRight,
                OriginalRows = left.Length,
                ActualRows = right.Length,
                OriginalComplete = IoPrintableOriginal404Terms.Typed(expected),
                ActualComplete = IoPrintableOriginal404Terms.Typed(actual)
            });
        int equal = 0;
        for (int index = 0; index < Math.Max(left.Length, right.Length); index++)
        {
            examined++;
            Term? originalRow = index < left.Length ? left[index] : null;
            Term? actualRow = index < right.Length ? right[index] : null;
            bool shape = originalRow is TupleTerm { Items.Count: 2 } && actualRow is TupleTerm { Items.Count: 2 };
            bool match = shape && originalRow!.Equals(actualRow);
            if (match)
                equal++;
            else
                differences.Add(new
                {
                    Scope = scope,
                    Index = index,
                    RowShapeValid = shape,
                    Original = originalRow is null ? null : IoPrintableOriginal404Terms.Typed(originalRow),
                    Actual = actualRow is null ? null : IoPrintableOriginal404Terms.Typed(actualRow),
                    OriginalWholeEtf = originalRow is null ? null : Convert.ToBase64String(ExternalTermFormat.Encode(originalRow)),
                    ActualWholeEtf = actualRow is null ? null : Convert.ToBase64String(ExternalTermFormat.Encode(actualRow)),
                    ProjectedFields = false
                });
        }

        return equal;
    }

    internal static int Profiles(
        Term expected,
        Term actual,
        int requiredRows,
        List<object> differences,
        ref int examined
    )
    {
        Term[] left = IoPrintableOriginal404Terms.List(expected, out bool properLeft);
        Term[] right = IoPrintableOriginal404Terms.List(actual, out bool properRight);
        if (!properLeft || !properRight || left.Length != IoPrintableOriginal404Names.ProfileCount || right.Length != IoPrintableOriginal404Names.ProfileCount)
            differences.Add(new
            {
                Scope = IoPrintableOriginal404Names.StacksScope,
                ProperOriginal = properLeft,
                ProperActual = properRight,
                OriginalProfiles = left.Length,
                ActualProfiles = right.Length
            });
        int equal = 0;
        for (int index = 0; index < Math.Max(left.Length, right.Length); index++)
        {
            Term? originalProfile = index < left.Length ? left[index] : null;
            Term? actualProfile = index < right.Length ? right[index] : null;
            if (originalProfile is not TupleTerm { Items.Count: 3 } original || actualProfile is not TupleTerm { Items.Count: 3 } observed)
            {
                differences.Add(new
                {
                    ProfileIndex = index,
                    ProfileShapeValid = false,
                    Original = originalProfile is null ? null : IoPrintableOriginal404Terms.Typed(originalProfile),
                    Actual = actualProfile is null ? null : IoPrintableOriginal404Terms.Typed(actualProfile)
                });
                continue;
            }
            if (!original.Items[0].Equals(observed.Items[0]) || !original.Items[1].Equals(observed.Items[1]))
                differences.Add(new
                {
                    ProfileIndex = index,
                    HeaderEqual = false,
                    Original = IoPrintableOriginal404Terms.Typed(originalProfile),
                    Actual = IoPrintableOriginal404Terms.Typed(actualProfile)
                });
            equal += Rows(
                original.Items[2],
                observed.Items[2],
                new
                {
                    ProfileIndex = index,
                    Requested = IoPrintableOriginal404Terms.Typed(original.Items[0])
                },
                requiredRows,
                differences,
                ref examined
            );
        }

        return equal;
    }
}
