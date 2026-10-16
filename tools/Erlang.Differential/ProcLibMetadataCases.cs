namespace Erlang.Differential;

public static class ProcLibMetadataCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            ProcLibMetadataCaseNames.InitialDictionary,
            ProcLibMetadataCaseSources.InitialDictionary,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InitialSpecific,
            ProcLibMetadataCaseSources.InitialSpecific,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InitialZero,
            ProcLibMetadataCaseSources.InitialZero,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InitialNonAtoms,
            ProcLibMetadataCaseSources.InitialNonAtoms,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.TranslateEmpty,
            ProcLibMetadataCaseSources.TranslateEmpty,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InitialEmpty,
            ProcLibMetadataCaseSources.InitialEmpty,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.TranslateMalformedArity,
            ProcLibMetadataCaseSources.TranslateMalformedArity,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.SpecificPrecedence,
            ProcLibMetadataCaseSources.SpecificPrecedence,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InvalidSpecificPrecedence,
            ProcLibMetadataCaseSources.InvalidSpecificPrecedence,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.DuplicateSpecific,
            ProcLibMetadataCaseSources.DuplicateSpecific,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.MalformedOuterDictionary,
            ProcLibMetadataCaseSources.MalformedOuterDictionary,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.MalformedStoredMfa,
            ProcLibMetadataCaseSources.MalformedStoredMfa,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.MalformedInnerItem,
            ProcLibMetadataCaseSources.MalformedInnerItem,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.ImproperSpecific,
            ProcLibMetadataCaseSources.ImproperSpecific,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.ImproperNoSpecific,
            ProcLibMetadataCaseSources.ImproperNoSpecific,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InvalidDictionary,
            ProcLibMetadataCaseSources.InvalidDictionary,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.SpecificTupleOne,
            ProcLibMetadataCaseSources.SpecificTupleOne,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.SpecificTupleThree,
            ProcLibMetadataCaseSources.SpecificTupleThree,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InvalidInput,
            ProcLibMetadataCaseSources.InvalidInput,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InvalidPidTupleFields,
            ProcLibMetadataCaseSources.InvalidPidTupleFields,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.InvalidPidTupleValue,
            ProcLibMetadataCaseSources.InvalidPidTupleValue,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.PidTupleSelf,
            ProcLibMetadataCaseSources.PidTupleSelf,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.SelfDictionary,
            ProcLibMetadataCaseSources.SelfDictionary,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.FullSnapshot,
            ProcLibMetadataCaseSources.FullSnapshot,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.SpecificSnapshot,
            ProcLibMetadataCaseSources.SpecificSnapshot,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LiveChild,
            ProcLibMetadataCaseSources.LiveChild,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.PlainChild,
            ProcLibMetadataCaseSources.PlainChild,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.DeadChild,
            ProcLibMetadataCaseSources.DeadChild,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.ArityAtom,
            ProcLibMetadataCaseSources.ArityAtom,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.ArityFloatZero,
            ProcLibMetadataCaseSources.ArityFloatZero,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.ArityFloat,
            ProcLibMetadataCaseSources.ArityFloat,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelDefaults,
            ProcLibMetadataCaseSources.LabelDefaults,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelOverwrite,
            ProcLibMetadataCaseSources.LabelOverwrite,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelFalseAndNil,
            ProcLibMetadataCaseSources.LabelFalseAndNil,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelDictionary,
            ProcLibMetadataCaseSources.LabelDictionary,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelLiveChild,
            ProcLibMetadataCaseSources.LabelLiveChild,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelNoInheritance,
            ProcLibMetadataCaseSources.LabelNoInheritance,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelDeadChild,
            ProcLibMetadataCaseSources.LabelDeadChild,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelInvalidInputs,
            ProcLibMetadataCaseSources.LabelInvalidInputs,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelRegisteredName,
            ProcLibMetadataCaseSources.LabelRegisteredName,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.DisconnectedPid,
            ProcLibMetadataCaseSources.DisconnectedPid,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelPidValue,
            ProcLibMetadataCaseSources.LabelPidValue,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        ),
        new(
            ProcLibMetadataCaseNames.LabelBinary,
            ProcLibMetadataCaseSources.LabelBinary,
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ProcLibMetadataCaseNames.Ok))
        )
    ];
}
