namespace Erlang;

// Passive, fail-closed provenance of selected original BEAM error call sites.
// Never derives lines from observed outcomes or assumes helper function names.
internal static class RuntimeNativeErrorMetadata545
{
    internal static Term Inherited(BeamLinkedCode code)
    {
        Term selected = Select(
            code,
            RuntimeNativeErrorMetadata545Names.SpawnOpt,
            2,
            Shape.Inherited
        );
        foreach (int arity in new[] { 3, 5 })
            Require(selected.Equals(Select(
                code,
                RuntimeNativeErrorMetadata545Names.SpawnOpt,
                arity,
                Shape.Inherited
            )));

        return selected;
    }

    internal static Term BadargInfo(BeamLinkedCode code)
        => Select(
            code,
            RuntimeNativeErrorMetadata545Names.SpawnOpt,
            2,
            Shape.BadargInfo
        );

    internal static Term BadargCause(BeamLinkedCode code)
        => Select(
            code,
            RuntimeNativeErrorMetadata545Names.SpawnOpt,
            5,
            Shape.BadargCause
        );

    internal static Term ErrorInfo(BeamLinkedCode code)
        => Select(
            code,
            RuntimeNativeMetadataNames527.LoadModule,
            RuntimeNativeMetadataNames527.LoadModuleArity,
            Shape.ErrorInfo
        );

    // Narrow observed partition: only failures of the native option operation
    // inside a source-proven wrapper try clause. Other partitions remain open.
    internal static Term? SpawnOptionLocation(ProcessContext context, IReadOnlyList<Term> arguments, bool badOption)
    {
        RuntimeNativeMetadataImage? image = context.Runtime.NativeMetadataImage527;
        if (image is null || !badOption) return null;
        bool inherited = arguments.Count switch
        {
            2 => arguments[0] is FunctionTerm,
            3 => arguments[0] is Atom node && (node.Name == context.Runtime.Node || arguments[1] is FunctionTerm { Arity: 0 }),
            5 => arguments[0] is Atom node && node.Name == context.Runtime.Node && arguments[1] is Atom && arguments[2] is Atom
                && (arguments[3] is Cons or Nil) && (arguments[4] is Cons or Nil),
            _ => false
        };

        return inherited ? image.InheritedErrorLocation545 : null;

    }

    private enum Shape
    {
        Unknown,
        Inherited,
        BadargInfo,
        BadargCause,
        ErrorInfo,
        Nil,
        Badarg,
        InfoKey,
        One,
        KeyfindResult,
        ErrorInfoTuple,
        Literal
    }

    private readonly record struct Value(Shape Kind, Term? Literal = null);

    private static Term Select(
        BeamLinkedCode code,
        string functionName,
        int arity,
        Shape shape
    )
    {
        BeamLinkedFunction function = code.Exports.Single(function => function.Name == functionName && function.Arity == arity);
        var registers = new Dictionary<(string, int), Value>();
        IReadOnlyList<Term> locations = BeamInstructionLocations.Read(code);
        Term? selected = null;
        for (int index = function.FirstInstructionIndex; index < function.EndInstructionIndex; index++)
        {
            BeamInstruction instruction = code.Instructions[index];
            IReadOnlyList<BeamOperand> operands = instruction.Operands;
            string name = instruction.Definition.Name;
            if (name == BeamLinkNames.Label)
            {
                registers.Clear();
                continue;
            }
            if (name is BeamLinkNames.Line or BeamLinkNames.HeapTest or BeamGeneralOpcodeNames.TupleTest or RuntimeNativeErrorMetadata545Names.TestArity)
                continue;
            if (name == BeamOnLoadNames.InitY)
            {
                foreach (BeamOperand register in operands.Skip(1)) Write(register, default);
                continue;
            }
            if (name == BeamLinkNames.Move)
            {
                Write(operands[1], Read(operands[0]));
                continue;
            }
            if (name == BeamGeneralOpcodeNames.Swap)
            {
                Value first = Read(operands[0]), second = Read(operands[1]);
                Write(operands[0], second);
                Write(operands[1], first);
                continue;
            }
            if (name == BeamOnLoadNames.TaggedTupleTest)
            {
                if (Read(operands[1]).Kind == Shape.KeyfindResult && operands[2].Value == RuntimeNativeErrorMetadata545Names.TupleArity
                    && operands[3].Kind == BeamOperandParameters.Atom && code.Structure.Atoms[(int)operands[3].Value] == RuntimeNativeErrorMetadata545Names.ErrorInfo)
                    Write(operands[1], new(Shape.ErrorInfoTuple));
                continue;
            }
            if (name == BeamLinkNames.PutList)
            {
                Value head = Read(operands[0]), tail = Read(operands[1]);
                Write(operands[2], head.Kind == Shape.ErrorInfoTuple && tail.Kind == Shape.Nil ? new(Shape.Inherited) : default);
                continue;
            }
            if (name is BeamLinkNames.ExternalCall or BeamLinkNames.ExternalTailCall or BeamOnLoadNames.ExternalLast)
            {
                BeamImport import = code.Structure.Imports[(int)operands[1].Value];
                string? module = code.Structure.Atoms[import.ModuleAtomIndex], called = code.Structure.Atoms[import.FunctionAtomIndex];
                if (module == RuntimeNativeMetadataNames527.Module && called == RuntimeNativeMetadataNames527.Error && import.Arity == RuntimeNativeErrorMetadata545Names.ErrorArity)
                {
                    Value extra = X(RuntimeNativeErrorMetadata545Names.ExtraArgumentRegister);
                    Shape actual = extra.Kind == Shape.Inherited ? Shape.Inherited : LiteralShape(extra, X(0));
                    if (actual == shape)
                    {
                        Term location = locations[index];
                        Require(selected is null || selected.Equals(location));
                        selected = location;
                    }
                }
                bool keyfind = module == RuntimeNativeErrorMetadata545Names.Lists && called == RuntimeNativeErrorMetadata545Names.Keyfind
                    && import.Arity == RuntimeNativeErrorMetadata545Names.ErrorArity && X(0).Kind == Shape.InfoKey && X(1).Kind == Shape.One;
                foreach (var register in registers.Keys.Where(key => key.Item1 == BeamOperandParameters.X).ToArray()) registers.Remove(register);
                if (keyfind && name == BeamLinkNames.ExternalCall) registers[(BeamOperandParameters.X, 0)] = new(Shape.KeyfindResult);
                continue;
            }
            // Unknown opcodes, local calls, control joins and writes invalidate
            // provenance. No guessed liveness/definition crosses a boundary.
            registers.Clear();
        }
        Require(selected is not null);

        return selected!;

        Value X(int register) => registers.GetValueOrDefault((BeamOperandParameters.X, register));

        Value Read(BeamOperand operand) => operand.Kind switch
        {
            BeamOperandParameters.X or BeamOperandParameters.Y => registers.GetValueOrDefault((operand.Kind, (int)operand.Value)),
            BeamOperandParameters.Nil => new(Shape.Nil),
            BeamOperandParameters.Integer when operand.Value == RuntimeNativeErrorMetadata545Names.ErrorInfoPosition => new(Shape.One),
            BeamOperandParameters.Atom when code.Structure.Atoms[(int)operand.Value] == RuntimeNativeErrorMetadata545Names.ErrorInfo => new(Shape.InfoKey),
            BeamOperandParameters.Atom when code.Structure.Atoms[(int)operand.Value] == ErlangErrorReasons.BadArgument => new(Shape.Badarg),
            BeamOperandParameters.LiteralValue => new(Shape.Literal, code.Structure.Literals[(int)operand.Value]),
            _ => default
        };

        void Write(BeamOperand destination, Value value)
        {
            Require(destination.Kind is BeamOperandParameters.X or BeamOperandParameters.Y);
            registers[(destination.Kind, (int)destination.Value)] = value;
        }
    }

    private static Shape LiteralShape(Value extra, Value reason)
    {
        if (extra is not { Kind: Shape.Literal, Literal: Cons { Head: TupleTerm { Items: [Atom { Name: RuntimeNativeErrorMetadata545Names.ErrorInfo }, MapTerm map] }, Tail: Nil } })
            return Shape.Unknown;
        bool formatter = map.Entries.Any(pair => pair.Key is Atom { Name: RuntimeNativeErrorMetadata545Names.Module }
            && pair.Value is Atom { Name: RuntimeNativeErrorMetadata545Names.ErrorFormatter });
        if (!formatter) return Shape.Unknown;
        bool cause = map.Entries.Any(pair => pair.Key is Atom { Name: RuntimeNativeErrorMetadata545Names.Cause });

        return cause ? reason.Kind == Shape.Badarg ? Shape.BadargCause : Shape.Unknown
            : reason.Kind == Shape.Badarg ? Shape.BadargInfo : Shape.ErrorInfo;
    }

    private static void Require(bool valid)
    {
        if (!valid) throw new InvalidDataException(RuntimeNativeMetadataNames527.Invalid);
    }
}
