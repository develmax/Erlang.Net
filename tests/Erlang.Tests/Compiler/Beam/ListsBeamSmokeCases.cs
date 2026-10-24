using Erlang;
using System.Numerics;

namespace Erlang.Tests;

internal static class ListsBeamSmokeCases
{
    internal static IReadOnlyList<ListsBeamSmokeCase> Create() =>
    [
        new(
            ListsBeamSmokeNames.Reverse,
            1,
            false,
            [L(1, 2, 3)],
            L(3, 2, 1)
        ),
        new(
            ListsBeamSmokeNames.Reverse,
            2,
            true,
            [L(1, 2), L(3)],
            L(2, 1, 3)
        ),
        new(
            ListsBeamSmokeNames.Append,
            2,
            false,
            [L(1, 2), L(3)],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Append,
            1,
            false,
            [Term.List(L(1), L(2, 3))],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Duplicate,
            2,
            false,
            [I(3), I(7)],
            L(7, 7, 7)
        ),
        new(
            ListsBeamSmokeNames.Flatten,
            1,
            false,
            [Term.List(Term.List(I(1), L(2)), Nil.Value, I(3))],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Flatten,
            2,
            false,
            [Term.List(I(1), L(2)), L(3)],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Member,
            2,
            true,
            [I(2), L(1, 2, 3)],
            A(ListsBeamSmokeNames.True)
        ),
        new(
            ListsBeamSmokeNames.Sum,
            1,
            false,
            [L(3, 1, 2)],
            I(6)
        ),
        new(
            ListsBeamSmokeNames.Max,
            1,
            false,
            [L(3, 1, 2)],
            I(3)
        ),
        new(
            ListsBeamSmokeNames.Min,
            1,
            false,
            [L(3, 1, 2)],
            I(1)
        ),
        new(
            ListsBeamSmokeNames.Keyfind,
            3,
            true,
            [A(ListsBeamSmokeNames.B), I(1), Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(2)))],
            Term.Tuple(A(ListsBeamSmokeNames.B), I(2))
        ),
        new(
            ListsBeamSmokeNames.Keymember,
            3,
            true,
            [A(ListsBeamSmokeNames.B), I(1), Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(2)))],
            A(ListsBeamSmokeNames.True)
        ),
        new(
            ListsBeamSmokeNames.Keysearch,
            3,
            true,
            [A(ListsBeamSmokeNames.B), I(1), Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(2)))],
            Term.Tuple(A(ListsBeamSmokeNames.Value), Term.Tuple(A(ListsBeamSmokeNames.B), I(2)))
        ),
        new(
            ListsBeamSmokeNames.Nth,
            2,
            false,
            [I(2), L(10, 20, 30)],
            I(20)
        ),
        new(
            ListsBeamSmokeNames.Nthtail,
            2,
            false,
            [I(1), L(10, 20, 30)],
            L(20, 30)
        ),
        new(
            ListsBeamSmokeNames.Last,
            1,
            false,
            [L(10, 20, 30)],
            I(30)
        ),
        new(
            ListsBeamSmokeNames.Split,
            2,
            false,
            [I(2), L(1, 2, 3)],
            Term.Tuple(L(1, 2), L(3))
        ),
        new(
            ListsBeamSmokeNames.Seq,
            2,
            false,
            [I(2), I(4)],
            L(2, 3, 4)
        ),
        new(
            ListsBeamSmokeNames.Seq,
            3,
            false,
            [I(5), I(1), I(-2)],
            L(5, 3, 1)
        ),
        new(
            ListsBeamSmokeNames.Map,
            2,
            false,
            [Double(), L(1, 2, 3)],
            L(2, 4, 6)
        ),
        new(
            ListsBeamSmokeNames.Sort,
            1,
            false,
            [L(
                3,
                1,
                2,
                1
            )],
            L(
                1,
                1,
                2,
                3
            )
        ),
        new(
            ListsBeamSmokeNames.Sort,
            2,
            false,
            [LessOrEqual(), L(
                3,
                1,
                2,
                1
            )],
            L(
                1,
                1,
                2,
                3
            )
        ),
        new(
            ListsBeamSmokeNames.Usort,
            1,
            false,
            [L(
                3,
                1,
                2,
                1
            )],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Usort,
            2,
            false,
            [LessOrEqual(), L(
                3,
                1,
                2,
                1
            )],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.All,
            2,
            false,
            [Even(), L(2, 4)],
            A(ListsBeamSmokeNames.True)
        ),
        new(
            ListsBeamSmokeNames.Any,
            2,
            false,
            [Even(), L(1, 2, 3)],
            A(ListsBeamSmokeNames.True)
        ),
        new(
            ListsBeamSmokeNames.Partition,
            2,
            false,
            [Even(), L(1, 2, 3)],
            Term.Tuple(L(2), L(1, 3))
        ),
        new(
            ListsBeamSmokeNames.Filtermap,
            2,
            false,
            [FilterTimesTen(), L(
                1,
                2,
                3,
                4
            )],
            L(20, 40)
        ),
        new(
            ListsBeamSmokeNames.Zf,
            2,
            false,
            [FilterTimesTen(), L(
                1,
                2,
                3,
                4
            )],
            L(20, 40)
        ),
        new(
            ListsBeamSmokeNames.Foreach,
            2,
            false,
            [Identity(), L(1, 2, 3)],
            A(ListsBeamSmokeNames.Ok)
        ),
        new(
            ListsBeamSmokeNames.Takewhile,
            2,
            false,
            [LessThanThree(), L(
                1,
                2,
                3,
                1
            )],
            L(1, 2)
        ),
        new(
            ListsBeamSmokeNames.Dropwhile,
            2,
            false,
            [LessThanThree(), L(
                1,
                2,
                3,
                1
            )],
            L(3, 1)
        ),
        new(
            ListsBeamSmokeNames.Foldl,
            3,
            false,
            [Digits(), I(0), L(1, 2, 3)],
            I(123)
        ),
        new(
            ListsBeamSmokeNames.Foldr,
            3,
            false,
            [Digits(), I(0), L(1, 2, 3)],
            I(321)
        ),
        new(
            ListsBeamSmokeNames.Mapfoldl,
            3,
            false,
            [MapDigits(), I(0), L(1, 2, 3)],
            Term.Tuple(L(2, 4, 6), I(123))
        ),
        new(
            ListsBeamSmokeNames.Mapfoldr,
            3,
            false,
            [MapDigits(), I(0), L(1, 2, 3)],
            Term.Tuple(L(2, 4, 6), I(321))
        ),
        new(
            ListsBeamSmokeNames.Concat,
            1,
            false,
            [Term.List(A(ListsBeamSmokeNames.A), I(12), Term.String(ListsBeamSmokeNames.Z))],
            Term.String(ListsBeamSmokeNames.ConcatExpected)
        ),
        new(
            ListsBeamSmokeNames.Delete,
            2,
            false,
            [I(2), L(
                1,
                2,
                2,
                3
            )],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Droplast,
            1,
            false,
            [L(1, 2, 3)],
            L(1, 2)
        ),
        new(
            ListsBeamSmokeNames.Enumerate,
            1,
            false,
            [Term.List(A(ListsBeamSmokeNames.A), A(ListsBeamSmokeNames.B))],
            Term.List(Term.Tuple(I(1), A(ListsBeamSmokeNames.A)), Term.Tuple(I(2), A(ListsBeamSmokeNames.B)))
        ),
        new(
            ListsBeamSmokeNames.Enumerate,
            2,
            false,
            [I(5), Term.List(A(ListsBeamSmokeNames.A), A(ListsBeamSmokeNames.B))],
            Term.List(Term.Tuple(I(5), A(ListsBeamSmokeNames.A)), Term.Tuple(I(6), A(ListsBeamSmokeNames.B)))
        ),
        new(
            ListsBeamSmokeNames.Enumerate,
            3,
            false,
            [I(5), I(-2), Term.List(A(ListsBeamSmokeNames.A), A(ListsBeamSmokeNames.B))],
            Term.List(Term.Tuple(I(5), A(ListsBeamSmokeNames.A)), Term.Tuple(I(3), A(ListsBeamSmokeNames.B)))
        ),
        new(
            ListsBeamSmokeNames.Flatlength,
            1,
            false,
            [Term.List(Term.List(I(1), L(2)), I(3))],
            I(3)
        ),
        new(
            ListsBeamSmokeNames.Join,
            2,
            false,
            [I(0), L(1, 2, 3)],
            L(
                1,
                0,
                2,
                0,
                3
            )
        ),
        new(
            ListsBeamSmokeNames.Prefix,
            2,
            false,
            [L(1, 2), L(1, 2, 3)],
            A(ListsBeamSmokeNames.True)
        ),
        new(
            ListsBeamSmokeNames.Suffix,
            2,
            false,
            [L(2, 3), L(1, 2, 3)],
            A(ListsBeamSmokeNames.True)
        ),
        new(
            ListsBeamSmokeNames.Sublist,
            2,
            false,
            [L(
                1,
                2,
                3,
                4
            ), I(2)],
            L(1, 2)
        ),
        new(
            ListsBeamSmokeNames.Sublist,
            3,
            false,
            [L(
                1,
                2,
                3,
                4
            ), I(2), I(2)],
            L(2, 3)
        ),
        new(
            ListsBeamSmokeNames.Subtract,
            2,
            false,
            [L(
                1,
                2,
                1,
                3
            ), L(1, 3)],
            L(2, 1)
        ),
        new(
            ListsBeamSmokeNames.Uniq,
            1,
            false,
            [L(
                2,
                1,
                2,
                3,
                1
            )],
            L(2, 1, 3)
        ),
        new(
            ListsBeamSmokeNames.Uniq,
            2,
            false,
            [Absolute(), L(
                1,
                -1,
                2,
                -2
            )],
            L(1, 2)
        ),
        new(
            ListsBeamSmokeNames.Unzip,
            1,
            false,
            [Term.List(Term.Tuple(I(1), I(10)), Term.Tuple(I(2), I(20)))],
            Term.Tuple(L(1, 2), L(10, 20))
        ),
        new(
            ListsBeamSmokeNames.Unzip3,
            1,
            false,
            [Term.List(Term.Tuple(I(1), I(10), I(100)), Term.Tuple(I(2), I(20), I(200)))],
            Term.Tuple(L(1, 2), L(10, 20), L(100, 200))
        ),
        new(
            ListsBeamSmokeNames.Keydelete,
            3,
            false,
            [A(ListsBeamSmokeNames.B), I(1), Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            )],
            Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(3)))
        ),
        new(
            ListsBeamSmokeNames.Keytake,
            3,
            false,
            [A(ListsBeamSmokeNames.B), I(1), Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            )],
            Term.Tuple(
                A(ListsBeamSmokeNames.Value),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(3)))
            )
        ),
        new(
            ListsBeamSmokeNames.Keyreplace,
            4,
            false,
            [A(ListsBeamSmokeNames.B), I(1), Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            ), Term.Tuple(A(ListsBeamSmokeNames.B), I(9))],
            Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(9)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            )
        ),
        new(
            ListsBeamSmokeNames.Keystore,
            4,
            false,
            [A(ListsBeamSmokeNames.C), I(1), Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            ), Term.Tuple(A(ListsBeamSmokeNames.C), I(9))],
            Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3)),
                Term.Tuple(A(ListsBeamSmokeNames.C), I(9))
            )
        ),
        new(
            ListsBeamSmokeNames.Keymap,
            3,
            false,
            [Double(), I(2), Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(2)))],
            Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(2)), Term.Tuple(A(ListsBeamSmokeNames.B), I(4)))
        ),
        new(
            ListsBeamSmokeNames.Filter,
            2,
            false,
            [Even(), L(
                1,
                2,
                3,
                4
            )],
            L(2, 4)
        ),
        new(
            ListsBeamSmokeNames.Flatmap,
            2,
            false,
            [Repeat(), L(1, 2)],
            L(
                1,
                1,
                2,
                2
            )
        ),
        new(
            ListsBeamSmokeNames.Search,
            2,
            false,
            [Even(), L(1, 2, 3)],
            Term.Tuple(A(ListsBeamSmokeNames.Value), I(2))
        ),
        new(
            ListsBeamSmokeNames.Splitwith,
            2,
            false,
            [LessThanThree(), L(
                1,
                2,
                3,
                1
            )],
            Term.Tuple(L(1, 2), L(3, 1))
        ),
        new(
            ListsBeamSmokeNames.Keysort,
            2,
            false,
            [I(1), Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            )],
            Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            )
        ),
        new(
            ListsBeamSmokeNames.Ukeysort,
            2,
            false,
            [I(1), Term.List(
                Term.Tuple(A(ListsBeamSmokeNames.B), I(2)),
                Term.Tuple(A(ListsBeamSmokeNames.A), I(1)),
                Term.Tuple(A(ListsBeamSmokeNames.B), I(3))
            )],
            Term.List(Term.Tuple(A(ListsBeamSmokeNames.A), I(1)), Term.Tuple(A(ListsBeamSmokeNames.B), I(2)))
        ),
        new(
            ListsBeamSmokeNames.Merge,
            1,
            false,
            [Term.List(L(1, 3), L(2, 3))],
            L(
                1,
                2,
                3,
                3
            )
        ),
        new(
            ListsBeamSmokeNames.Merge,
            2,
            false,
            [L(1, 3), L(2, 3)],
            L(
                1,
                2,
                3,
                3
            )
        ),
        new(
            ListsBeamSmokeNames.Merge,
            3,
            false,
            [LessOrEqual(), L(1, 3), L(2, 3)],
            L(
                1,
                2,
                3,
                3
            )
        ),
        new(
            ListsBeamSmokeNames.Umerge,
            1,
            false,
            [Term.List(L(1, 3), L(2, 3))],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Umerge,
            2,
            false,
            [L(1, 3), L(2, 3)],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Umerge,
            3,
            false,
            [LessOrEqual(), L(1, 3), L(2, 3)],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Rmerge,
            2,
            false,
            [L(3, 1), L(3, 2)],
            L(
                3,
                3,
                2,
                1
            )
        ),
        new(
            ListsBeamSmokeNames.Rmerge,
            3,
            false,
            [LessOrEqual(), L(3, 1), L(3, 2)],
            L(
                3,
                3,
                2,
                1
            )
        ),
        new(
            ListsBeamSmokeNames.Rumerge,
            2,
            false,
            [L(3, 1), L(3, 2)],
            L(3, 2, 1)
        ),
        new(
            ListsBeamSmokeNames.Rumerge,
            3,
            false,
            [LessOrEqual(), L(3, 1), L(3, 2)],
            L(3, 2, 1)
        ),
        new(
            ListsBeamSmokeNames.Keymerge,
            3,
            false,
            [I(1), Term.List(Term.Tuple(I(1), I(10)), Term.Tuple(I(3), I(30))), Term.List(Term.Tuple(I(2), I(20)), Term.Tuple(I(4), I(40)))],
            Term.List(
                Term.Tuple(I(1), I(10)),
                Term.Tuple(I(2), I(20)),
                Term.Tuple(I(3), I(30)),
                Term.Tuple(I(4), I(40))
            )
        ),
        new(
            ListsBeamSmokeNames.Ukeymerge,
            3,
            false,
            [I(1), Term.List(Term.Tuple(I(1), I(10)), Term.Tuple(I(3), I(30))), Term.List(Term.Tuple(I(2), I(20)), Term.Tuple(I(4), I(40)))],
            Term.List(
                Term.Tuple(I(1), I(10)),
                Term.Tuple(I(2), I(20)),
                Term.Tuple(I(3), I(30)),
                Term.Tuple(I(4), I(40))
            )
        ),
        new(
            ListsBeamSmokeNames.Rkeymerge,
            3,
            false,
            [I(1), Term.List(Term.Tuple(I(3), I(30)), Term.Tuple(I(1), I(10))), Term.List(Term.Tuple(I(4), I(40)), Term.Tuple(I(2), I(20)))],
            Term.List(
                Term.Tuple(I(4), I(40)),
                Term.Tuple(I(3), I(30)),
                Term.Tuple(I(2), I(20)),
                Term.Tuple(I(1), I(10))
            )
        ),
        new(
            ListsBeamSmokeNames.Rukeymerge,
            3,
            false,
            [I(1), Term.List(Term.Tuple(I(3), I(30)), Term.Tuple(I(1), I(10))), Term.List(Term.Tuple(I(4), I(40)), Term.Tuple(I(2), I(20)))],
            Term.List(
                Term.Tuple(I(4), I(40)),
                Term.Tuple(I(3), I(30)),
                Term.Tuple(I(2), I(20)),
                Term.Tuple(I(1), I(10))
            )
        ),
        new(
            ListsBeamSmokeNames.Merge3,
            3,
            false,
            [L(1, 4), L(2, 5), L(3, 6)],
            L(
                1,
                2,
                3,
                4,
                5,
                6
            )
        ),
        new(
            ListsBeamSmokeNames.Rmerge3,
            3,
            false,
            [L(6, 3), L(5, 2), L(4, 1)],
            L(
                6,
                5,
                4,
                3,
                2,
                1
            )
        ),
        new(
            ListsBeamSmokeNames.Umerge3,
            3,
            false,
            [L(1, 3), L(1, 2), L(2, 3)],
            L(1, 2, 3)
        ),
        new(
            ListsBeamSmokeNames.Rumerge3,
            3,
            false,
            [L(3, 1), L(2, 1), L(3, 2)],
            L(3, 2, 1)
        ),
        new(
            ListsBeamSmokeNames.Zip,
            2,
            false,
            [L(1, 2), L(10, 20)],
            Term.List(Term.Tuple(I(1), I(10)), Term.Tuple(I(2), I(20)))
        ),
        new(
            ListsBeamSmokeNames.Zip,
            3,
            false,
            [L(1, 2, 3), L(10, 20), A(ListsBeamSmokeNames.Trim)],
            Term.List(Term.Tuple(I(1), I(10)), Term.Tuple(I(2), I(20)))
        ),
        new(
            ListsBeamSmokeNames.Zip3,
            3,
            false,
            [L(1, 2), L(10, 20), L(100, 200)],
            Term.List(Term.Tuple(I(1), I(10), I(100)), Term.Tuple(I(2), I(20), I(200)))
        ),
        new(
            ListsBeamSmokeNames.Zip3,
            4,
            false,
            [L(1), L(10, 20), L(100), PadThree()],
            Term.List(Term.Tuple(I(1), I(10), I(100)), Term.Tuple(I(0), I(20), I(0)))
        ),
        new(
            ListsBeamSmokeNames.Zipwith,
            3,
            false,
            [ZipSum(), L(1, 2), L(10, 20)],
            L(11, 22)
        ),
        new(
            ListsBeamSmokeNames.Zipwith,
            4,
            false,
            [ZipSum(), L(1, 2), L(10), PadTwo()],
            L(11, 2)
        ),
        new(
            ListsBeamSmokeNames.Zipwith3,
            4,
            false,
            [ZipSumThree(), L(1, 2), L(10, 20), L(100, 200)],
            L(111, 222)
        ),
        new(
            ListsBeamSmokeNames.Zipwith3,
            5,
            false,
            [ZipSumThree(), L(1), L(10, 20), L(100), PadThree()],
            L(111, 20)
        )
    ];

    private static Term I(long value) => Term.I(value);

    private static Term A(string value) => Term.A(value);

    private static Term L(params long[] values) => Cons.From(values.Select(value => I(value)));

    private static BigInteger Number(Term value) => ((Integer)value).Value;

    private static Term Boolean(bool value) => A(value ? ListsBeamSmokeNames.True : ListsBeamSmokeNames.False);

    private static FunctionTerm Double() => new(1, (_, args) => ValueTask.FromResult<Term>(new Integer(Number(args[0]) * 2)));

    private static FunctionTerm Even() => new(1, (_, args) => ValueTask.FromResult(Boolean(Number(args[0]).IsEven)));

    private static FunctionTerm LessThanThree() => new(1, (_, args) => ValueTask.FromResult(Boolean(Number(args[0]) < 3)));

    private static FunctionTerm Identity() => new(1, (_, args) => ValueTask.FromResult(args[0]));

    private static FunctionTerm Repeat() => new(1, (_, args) => ValueTask.FromResult<Term>(Term.List(args[0], args[0])));

    private static FunctionTerm Absolute() => new(1, (_, args) => ValueTask.FromResult<Term>(new Integer(BigInteger.Abs(Number(args[0])))));

    private static FunctionTerm LessOrEqual() => new(2, (_, args) => ValueTask.FromResult(Boolean(Number(args[0]) <= Number(args[1]))));

    private static FunctionTerm Digits() => new(2, (_, args) => ValueTask.FromResult<Term>(new Integer(Number(args[1]) * 10 + Number(args[0]))));

    private static FunctionTerm MapDigits() => new(
        2,
        (_, args) => ValueTask.FromResult<Term>(Term.Tuple(new Integer(Number(args[0]) * 2), new Integer(Number(args[1]) * 10 + Number(args[0]))))
    );

    private static FunctionTerm FilterTimesTen() => new(
        1,
        (_, args) => ValueTask.FromResult<Term>(Number(args[0]).IsEven
        ? Term.Tuple(A(ListsBeamSmokeNames.True), new Integer(Number(args[0]) * 10))
        : A(ListsBeamSmokeNames.False))
    );

    private static FunctionTerm ZipSum() => new(2, (_, args) => ValueTask.FromResult<Term>(new Integer(Number(args[0]) + Number(args[1]))));

    private static FunctionTerm ZipSumThree() => new(3, (_, args) => ValueTask.FromResult<Term>(new Integer(Number(args[0]) + Number(args[1]) + Number(args[2]))));

    private static Term PadTwo() => Term.Tuple(A(ListsBeamSmokeNames.Pad), Term.Tuple(I(0), I(0)));

    private static Term PadThree() => Term.Tuple(A(ListsBeamSmokeNames.Pad), Term.Tuple(I(0), I(0), I(0)));
}
