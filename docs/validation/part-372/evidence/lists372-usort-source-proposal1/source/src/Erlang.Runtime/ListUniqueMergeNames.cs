namespace Erlang;

internal static class ListUniqueMergeNames
{
    internal const string Asc = "asc";
    internal const string Desc = "desc";
    internal const string File = "lists.erl";
    internal const string InvalidState = "Unique merge entered an invalid source state.";
    internal const int UniqueSortLine = 2075;
    internal const string SourceFileKey = "file";
    internal const string SourceLineKey = "line";
    internal static readonly Atom AscTerm = Term.A(Asc);
    internal static readonly Atom DescTerm = Term.A(Desc);
    internal const string Usort = "usort";
    internal const string Usort_1 = "usort_1";
    internal const string Usplit_1 = "usplit_1";
    internal const string Usplit_1_1 = "usplit_1_1";
    internal const string Usplit_2 = "usplit_2";
    internal const string Usplit_2_1 = "usplit_2_1";
    internal const string Umergel = "umergel";
    internal const string Rumergel = "rumergel";
    internal const string Umerge3_1 = "umerge3_1";
    internal const string Umerge3_2 = "umerge3_2";
    internal const string Umerge3_12 = "umerge3_12";
    internal const string Umerge3_12_3 = "umerge3_12_3";
    internal const string Umerge3_21 = "umerge3_21";
    internal const string Umerge3_21_3 = "umerge3_21_3";
    internal const string Rumerge3_1 = "rumerge3_1";
    internal const string Rumerge3_12a = "rumerge3_12a";
    internal const string Rumerge3_2 = "rumerge3_2";
    internal const string Rumerge3_12b = "rumerge3_12b";
    internal const string Rumerge3_12_3 = "rumerge3_12_3";
    internal const string Rumerge3_21_3 = "rumerge3_21_3";
    internal const string Umerge2_1 = "umerge2_1";
    internal const string Umerge2_2 = "umerge2_2";
    internal const string Rumerge2_1 = "rumerge2_1";
    internal const string Rumerge2_2 = "rumerge2_2";
    internal const int CallLine3294 = 3294;
    internal const int CallLine3302 = 3302;
    internal const int CallLine3304 = 3304;
    internal const int CallLine3306 = 3306;
    internal const int CallLine3308 = 3308;
    internal const int CallLine3311 = 3311;
    internal const int CallLine3313 = 3313;
    internal const int CallLine3315 = 3315;
    internal const int CallLine3317 = 3317;
    internal const int CallLine3319 = 3319;

    internal static string Function(ListUniqueMergeState state) => state switch
    {
        ListUniqueMergeState.Usort1 => Usort,
        ListUniqueMergeState.Usort_12 => Usort_1,
        ListUniqueMergeState.Usplit_15 => Usplit_1,
        ListUniqueMergeState.Usplit_1_16 => Usplit_1_1,
        ListUniqueMergeState.Usplit_25 => Usplit_2,
        ListUniqueMergeState.Usplit_2_16 => Usplit_2_1,
        ListUniqueMergeState.Umergel1 => Umergel,
        ListUniqueMergeState.Umergel3 => Umergel,
        ListUniqueMergeState.Rumergel3 => Rumergel,
        ListUniqueMergeState.Umerge3_17 => Umerge3_1,
        ListUniqueMergeState.Umerge3_27 => Umerge3_2,
        ListUniqueMergeState.Umerge3_128 => Umerge3_12,
        ListUniqueMergeState.Umerge3_12_36 => Umerge3_12_3,
        ListUniqueMergeState.Umerge3_218 => Umerge3_21,
        ListUniqueMergeState.Umerge3_21_36 => Umerge3_21_3,
        ListUniqueMergeState.Rumerge3_16 => Rumerge3_1,
        ListUniqueMergeState.Rumerge3_12a7 => Rumerge3_12a,
        ListUniqueMergeState.Rumerge3_27 => Rumerge3_2,
        ListUniqueMergeState.Rumerge3_12b8 => Rumerge3_12b,
        ListUniqueMergeState.Rumerge3_12_37 => Rumerge3_12_3,
        ListUniqueMergeState.Rumerge3_21_37 => Rumerge3_21_3,
        ListUniqueMergeState.Umerge2_15 => Umerge2_1,
        ListUniqueMergeState.Umerge2_24 => Umerge2_2,
        ListUniqueMergeState.Rumerge2_14 => Rumerge2_1,
        ListUniqueMergeState.Rumerge2_25 => Rumerge2_2,
        _ => throw new InvalidOperationException(InvalidState)
    };

    internal static int Line(ListUniqueMergeState state) => state switch
    {
        ListUniqueMergeState.Usort1 => 2075,
        ListUniqueMergeState.Usort_12 => 2116,
        ListUniqueMergeState.Usplit_15 => 3217,
        ListUniqueMergeState.Usplit_1_16 => 3232,
        ListUniqueMergeState.Usplit_25 => 3250,
        ListUniqueMergeState.Usplit_2_16 => 3265,
        ListUniqueMergeState.Umergel1 => 3284,
        ListUniqueMergeState.Umergel3 => 3287,
        ListUniqueMergeState.Rumergel3 => 3310,
        ListUniqueMergeState.Umerge3_17 => 3326,
        ListUniqueMergeState.Umerge3_27 => 3342,
        ListUniqueMergeState.Umerge3_128 => 3354,
        ListUniqueMergeState.Umerge3_12_36 => 3362,
        ListUniqueMergeState.Umerge3_218 => 3370,
        ListUniqueMergeState.Umerge3_21_36 => 3378,
        ListUniqueMergeState.Rumerge3_16 => 3386,
        ListUniqueMergeState.Rumerge3_12a7 => 3398,
        ListUniqueMergeState.Rumerge3_27 => 3404,
        ListUniqueMergeState.Rumerge3_12b8 => 3423,
        ListUniqueMergeState.Rumerge3_12_37 => 3429,
        ListUniqueMergeState.Rumerge3_21_37 => 3441,
        ListUniqueMergeState.Umerge2_15 => 3455,
        ListUniqueMergeState.Umerge2_24 => 3466,
        ListUniqueMergeState.Rumerge2_14 => 3476,
        ListUniqueMergeState.Rumerge2_25 => 3484,
        _ => throw new InvalidOperationException(InvalidState)
    };

    internal static int Arity(ListUniqueMergeState state) => state switch
    {
        ListUniqueMergeState.Usort1 => 1,
        ListUniqueMergeState.Usort_12 => 2,
        ListUniqueMergeState.Usplit_15 => 5,
        ListUniqueMergeState.Usplit_1_16 => 6,
        ListUniqueMergeState.Usplit_25 => 5,
        ListUniqueMergeState.Usplit_2_16 => 6,
        ListUniqueMergeState.Umergel1 => 1,
        ListUniqueMergeState.Umergel3 => 3,
        ListUniqueMergeState.Rumergel3 => 3,
        ListUniqueMergeState.Umerge3_17 => 7,
        ListUniqueMergeState.Umerge3_27 => 7,
        ListUniqueMergeState.Umerge3_128 => 8,
        ListUniqueMergeState.Umerge3_12_36 => 6,
        ListUniqueMergeState.Umerge3_218 => 8,
        ListUniqueMergeState.Umerge3_21_36 => 6,
        ListUniqueMergeState.Rumerge3_16 => 6,
        ListUniqueMergeState.Rumerge3_12a7 => 7,
        ListUniqueMergeState.Rumerge3_27 => 7,
        ListUniqueMergeState.Rumerge3_12b8 => 8,
        ListUniqueMergeState.Rumerge3_12_37 => 7,
        ListUniqueMergeState.Rumerge3_21_37 => 7,
        ListUniqueMergeState.Umerge2_15 => 5,
        ListUniqueMergeState.Umerge2_24 => 4,
        ListUniqueMergeState.Rumerge2_14 => 4,
        ListUniqueMergeState.Rumerge2_25 => 5,
        _ => throw new InvalidOperationException(InvalidState)
    };
}
