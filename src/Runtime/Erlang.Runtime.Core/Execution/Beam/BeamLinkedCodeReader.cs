using System.Collections.ObjectModel;

namespace Erlang;

internal static class BeamLinkedCodeReader
{
    internal static BeamLinkedCode Read(BeamStructure structure)
    {
        BeamTypeTable types = BeamTypeTableReader.Read(structure);
        IReadOnlyList<BeamLambdaEntry> lambdas = BeamLambdaReader.Read(structure);
        IReadOnlyList<BeamInstruction> instructions = BeamInstructionReader.Read(structure);
        Require(structure.Atoms.Count > 1 && structure.Atoms[1] is not null, BeamLinkDiagnostics.ModuleIdentity);
        string module = structure.Atoms[1]!;
        var labels = new Dictionary<int, int>();
        for (int index = 0; index < instructions.Count; index++)
        {
            BeamInstruction instruction = instructions[index];
            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.TypeIndex is { } typeIndex && !types.Fallback)
                    Require(typeIndex < types.Entries.Count, BeamLinkDiagnostics.TypeIndex);
            }
            if (instruction.Definition.Name == BeamLinkNames.Label)
            {
                int label = Unsigned(instruction.Operands[0]);
                Require(label < structure.Code.LabelCount && labels.TryAdd(label, index), BeamLinkDiagnostics.Label);
            }
        }
        var functions = new List<BeamLinkedFunction>();
        var beginnings = new List<int>();
        for (int index = 0; index < instructions.Count; index++)
        {
            BeamInstruction instruction = instructions[index];
            if (instruction.Definition.Name != BeamLinkNames.FunctionInfo)
                continue;
            int infoIndex = index - 1;
            if (infoIndex >= 0 && instructions[infoIndex].Definition.Name == BeamLinkNames.Line)
                infoIndex--;
            Require(
                infoIndex >= 0 && instructions[infoIndex].Definition.Name == BeamLinkNames.Label
                && index + 1 < instructions.Count && instructions[index + 1].Definition.Name == BeamLinkNames.Label,
                BeamLinkDiagnostics.FunctionBoundary
            );
            Require(
                instruction.Operands[0].Kind == BeamOperandParameters.Atom
                && instruction.Operands[1].Kind == BeamOperandParameters.Atom,
                BeamLinkDiagnostics.FunctionBoundary
            );
            string functionModule = structure.Atoms[(int)instruction.Operands[0].Value]!;
            string name = structure.Atoms[(int)instruction.Operands[1].Value]!;
            Require(functionModule == module, BeamLinkDiagnostics.ModuleIdentity);
            int arity = Unsigned(instruction.Operands[2]);
            Require(arity <= BeamLinkParameters.MaximumFunctionArity, BeamLinkDiagnostics.FunctionBoundary);
            beginnings.Add(infoIndex);
            functions.Add(new(
                module,
                name,
                arity,
                Unsigned(instructions[infoIndex].Operands[0]),
                Unsigned(instructions[index + 1].Operands[0]),
                index + 2,
                0
            ));
        }
        Require(functions.Count == structure.Code.FunctionCount && functions.Count != 0, BeamLinkDiagnostics.FunctionCount);
        Require(beginnings[0] == 0, BeamLinkDiagnostics.FunctionBoundary);
        for (int index = 0; index < functions.Count; index++)
            functions[index] = functions[index] with
            {
                EndInstructionIndex = index + 1 < functions.Count ? beginnings[index + 1] : instructions.Count - 1
            };
        var byEntry = new Dictionary<int, BeamLinkedFunction>();
        foreach (BeamLinkedFunction function in functions)
            Require(byEntry.TryAdd(function.EntryLabel, function), BeamLinkDiagnostics.FunctionBoundary);
        foreach (BeamLambdaEntry lambda in lambdas)
        {
            Require(
                byEntry.TryGetValue(lambda.Label, out BeamLinkedFunction? target)
                && target.Name == structure.Atoms[lambda.FunctionAtomIndex]
                && target.Arity == lambda.TotalArity
                && lambda.FreeVariables >= 0 && lambda.FreeVariables <= lambda.TotalArity,
                BeamLambdaDiagnostics.Target
            );
        }
        int? onLoad = null;
        for (int index = 0; index < instructions.Count; index++)
        {
            BeamInstruction instruction = instructions[index];
            foreach (BeamOperand operand in instruction.Operands)
                if (operand.Kind == BeamOperandParameters.Label)
                    Require(labels.ContainsKey((int)operand.Value), BeamLinkDiagnostics.LabelReference);
            ValidateRoles(
                instruction,
                structure,
                byEntry,
                lambdas
            );
            if (instruction.Definition.Name == BeamLinkNames.OnLoad)
            {
                BeamLinkedFunction? owner = functions.SingleOrDefault(function => index >= function.FirstInstructionIndex && index < function.EndInstructionIndex);
                Require(onLoad is null && owner is { Arity: 0 }, BeamLinkDiagnostics.FunctionBoundary);
                onLoad = owner!.EntryLabel;
            }
        }
        var exports = new List<BeamLinkedFunction>();
        foreach (BeamExport export in structure.Exports)
        {
            Require(byEntry.TryGetValue(export.Label, out BeamLinkedFunction? function), BeamLinkDiagnostics.Export);
            BeamLinkedFunction linked = function!;
            Require(linked.Name == structure.Atoms[export.FunctionAtomIndex] && linked.Arity == export.Arity, BeamLinkDiagnostics.Export);
            Require(!exports.Any(value => value.Name == linked.Name && value.Arity == linked.Arity), BeamLinkDiagnostics.Export);
            exports.Add(linked);
        }

        return new(
            structure,
            types,
            instructions,
            functions.AsReadOnly(),
            new ReadOnlyDictionary<int, int>(labels),
            exports.AsReadOnly(),
            onLoad,
            lambdas
        )
        {
            SourceLocations = BeamLineTableReader.Read(structure)
        };
    }

    private static void ValidateRoles(
        BeamInstruction instruction,
        BeamStructure structure,
        IReadOnlyDictionary<int, BeamLinkedFunction> functions,
        IReadOnlyList<BeamLambdaEntry> lambdas
    )
    {
        var operands = instruction.Operands;
        switch (instruction.Definition.Name)
        {
            case BeamRecordConstructionNames.Opcode:
                Require(
                    operands[BeamRecordConstructionNames.Failure].Kind == BeamOperandParameters.Failure
                    && operands[BeamRecordConstructionNames.Name].Kind == BeamOperandParameters.Atom
                    && operands[BeamRecordConstructionNames.Source].Kind == BeamOperandParameters.Nil,
                    BeamLinkDiagnostics.OperandRole
                );
                Destination(operands[BeamRecordConstructionNames.Destination]);
                _ = Unsigned(operands[BeamRecordConstructionNames.Live]);
                Require(
                    operands[BeamRecordConstructionNames.Fields].Kind == BeamOperandParameters.Count
                    && operands[BeamRecordConstructionNames.Fields].Value == operands.Count - BeamRecordConstructionNames.FirstField
                    && operands[BeamRecordConstructionNames.Fields].Value % BeamRecordConstructionNames.FieldWidth == 0,
                    BeamLinkDiagnostics.OperandRole
                );
                var recordFields = new HashSet<int>();
                for (int field = BeamRecordConstructionNames.FirstField; field < operands.Count; field += BeamRecordConstructionNames.FieldWidth)
                {
                    Require(
                        operands[field].Kind == BeamOperandParameters.Atom && recordFields.Add((int)operands[field].Value),
                        BeamLinkDiagnostics.OperandRole
                    );
                    Source(operands[field + 1]);
                }
                break;
            case BeamCollectionOpcodeNames.UpdateRecord:
                Require(operands[0].Kind == BeamOperandParameters.Atom, BeamLinkDiagnostics.OperandRole);
                int recordSize = Unsigned(operands[1]);
                Require(recordSize > 0 && recordSize < BeamOperandParameters.MaximumRegister, BeamLinkDiagnostics.OperandRole);
                Source(operands[2]);
                Destination(operands[3]);
                Require(
                    operands[4].Kind == BeamOperandParameters.Count && operands[4].Value == operands.Count - 5
                    && operands[4].Value > 0 && operands[4].Value % 2 == 0
                    && operands[4].Value / 2 <= recordSize,
                    BeamLinkDiagnostics.OperandRole
                );
                for (int field = 5; field < operands.Count; field += 2)
                {
                    int position = Unsigned(operands[field]);
                    Require(position >= 1 && position <= recordSize, BeamLinkDiagnostics.OperandRole);
                    Source(operands[field + 1]);
                }
                break;
            case BeamCollectionOpcodeNames.BadRecord:
                Source(operands[0]);
                break;
            case BeamCollectionOpcodeNames.FloatMove:
                Require(
                    (operands[0].Kind == BeamOperandParameters.Float) != (operands[1].Kind == BeamOperandParameters.Float),
                    BeamLinkDiagnostics.OperandRole
                );
                if (operands[0].Kind == BeamOperandParameters.Float)
                    Destination(operands[1]);
                else
                    Source(operands[0]);
                break;
            case BeamCollectionOpcodeNames.FloatConvert:
                Source(operands[0]);
                Require(operands[1].Kind == BeamOperandParameters.Float, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamCollectionOpcodeNames.FloatNegate:
                Require(
                    operands[0].Kind == BeamOperandParameters.Failure
                    && operands[1].Kind == BeamOperandParameters.Float
                    && operands[2].Kind == BeamOperandParameters.Float,
                    BeamLinkDiagnostics.OperandRole
                );
                break;
            case BeamCollectionOpcodeNames.FloatMultiply:
            case BeamCollectionOpcodeNames.FloatAdd:
            case BeamCollectionOpcodeNames.FloatSubtract:
            case BeamCollectionOpcodeNames.FloatDivide:
                Require(
                    operands[0].Kind == BeamOperandParameters.Failure
                    && operands[1].Kind == BeamOperandParameters.Float
                    && operands[2].Kind == BeamOperandParameters.Float
                    && operands[3].Kind == BeamOperandParameters.Float,
                    BeamLinkDiagnostics.OperandRole
                );
                break;
            case BeamCatchNames.Install:
            case BeamTryNames.Install:
                Require(operands[0].Kind == BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);
                Require(operands[1].Kind == BeamOperandParameters.Label, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamTryNames.Case:
            case BeamCatchNames.End:
            case BeamTryNames.End:
                Require(operands[0].Kind == BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamTryNames.BuildStacktrace:
            case BeamTryNames.RawRaise:
                break;
            case BeamTryNames.CaseEnd:
                Source(operands[0]);
                break;
            case BeamTryNames.Raise:
                Source(operands[0]);
                Source(operands[1]);
                break;
            case BeamGeneralOpcodeNames.NilTest:
            case BeamGeneralOpcodeNames.NonemptyListTest:
            case BeamGeneralOpcodeNames.ListTest:
            case BeamGeneralOpcodeNames.TupleTest:
            case BeamGeneralOpcodeNames.FloatTest:
            case BeamGeneralOpcodeNames.MapTest:
            case BeamGeneralOpcodeNames.NumberTest:
            case BeamGeneralOpcodeNames.BinaryTest:
            case BeamGeneralOpcodeNames.AtomTest:
            case BeamGeneralOpcodeNames.BooleanTest:
            case BeamGeneralOpcodeNames.ReferenceTest:
            case BeamGeneralOpcodeNames.AnyFunctionTest:
            case BeamGeneralOpcodeNames.BitStringTest:
            case BeamGeneralOpcodeNames.PortTest:
            case BeamGeneralOpcodeNames.NativeRecordTest:
                Branch(operands[0]);
                Source(operands[1]);
                break;
            case BeamGeneralOpcodeNames.TupleArity:
                Branch(operands[0]);
                Source(operands[1]);
                _ = Unsigned(operands[2]);
                break;
            case BeamGeneralOpcodeNames.FunctionTest:
            case BeamGeneralOpcodeNames.ArithmeticEqual:
            case BeamGeneralOpcodeNames.ArithmeticNotEqual:
            case BeamGeneralOpcodeNames.ExactNotEqual:
            case BeamGeneralOpcodeNames.GreaterOrEqual:
            case BeamGeneralOpcodeNames.Less:
                Branch(operands[0]);
                Source(operands[1]);
                Source(operands[2]);
                break;
            case BeamGeneralOpcodeNames.GetList:
                Source(operands[0]);
                Destination(operands[1]);
                Destination(operands[2]);
                break;
            case BeamGeneralOpcodeNames.GetHead:
            case BeamGeneralOpcodeNames.GetTail:
                Source(operands[0]);
                Destination(operands[1]);
                break;
            case BeamGeneralOpcodeNames.Swap:
                Destination(operands[0]);
                Destination(operands[1]);
                break;
            case BeamGeneralOpcodeNames.Trim:
                _ = Unsigned(operands[0]);
                _ = Unsigned(operands[1]);
                break;
            case BeamGeneralOpcodeNames.Jump:
                Require(operands[0].Kind == BeamOperandParameters.Label, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamBinaryNames.Create:
            case BeamBinaryNames.Start:
            case BeamBinaryNames.StartLegacy:
            case BeamBinaryNames.ExtractBinary:
            case BeamBinaryNames.ExtractInteger:
            case BeamFloatStringNames.ExtractFloat:
            case BeamBinaryNames.ExtractUtf8:
            case BeamBinaryNames.ExtractUtf16:
            case BeamBinaryNames.ExtractUtf32:
            case BeamBinaryNames.SkipUtf16:
            case BeamBinaryNames.SkipUtf32:
            case BeamBinaryNames.SkipUtf8:
            case BeamBinaryNames.SkipBits:
            case BeamBinaryNames.InitializeWritable:
            case BeamBinaryNames.ExtractTail:
            case BeamBinaryNames.SavePosition:
            case BeamBinaryNames.RestorePosition:
            case BeamFloatStringNames.MatchString:
            case BeamBinaryNames.Match:
                BeamBinaryInstructionValidation.Validate(instruction, structure);
                break;
            case BeamReceiveMarkerNames.Reserve:
                Destination(operands[0]);
                break;
            case BeamReceiveMarkerNames.Bind:
                Require(operands[0].Kind is BeamOperandParameters.X or BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);
                Require(operands[1].Kind is BeamOperandParameters.X or BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamReceiveMarkerNames.Clear:
            case BeamReceiveMarkerNames.Use:
                Require(operands[0].Kind is BeamOperandParameters.X or BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamGeneralOpcodeNames.SelectValue:
                Source(operands[0]);
                Branch(operands[1]);
                Require(
                    operands[2].Kind == BeamOperandParameters.Count && operands[2].Value == operands.Count - 3
                    && operands[2].Value % 2 == 0,
                    BeamLinkDiagnostics.OperandRole
                );
                for (int index = 3; index < operands.Count; index += 2)
                {
                    Source(operands[index]);
                    Require(operands[index].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y), BeamLinkDiagnostics.OperandRole);
                    Require(operands[index + 1].Kind == BeamOperandParameters.Label, BeamLinkDiagnostics.OperandRole);
                }
                BeamSelectValueValidation.Validate(structure, operands);
                break;
            case BeamGeneralOpcodeNames.SelectTupleArity:
                Destination(operands[0]);
                Branch(operands[1]);
                Require(
                    operands[2].Kind == BeamOperandParameters.Count
                    && operands[2].Value == operands.Count - 3 && operands[2].Value % 2 == 0,
                    BeamLinkDiagnostics.OperandRole
                );
                var arities = new HashSet<int>();
                for (int index = 3; index < operands.Count; index += 2)
                {
                    Require(arities.Add(Unsigned(operands[index])), BeamLinkDiagnostics.OperandRole);
                    Require(operands[index + 1].Kind == BeamOperandParameters.Label, BeamLinkDiagnostics.OperandRole);
                }
                break;
            case BeamGeneralOpcodeNames.BinaryBif:
                Branch(operands[0]);
                CheckImport(structure, Unsigned(operands[1]), 2);
                Source(operands[2]);
                Source(operands[3]);
                Destination(operands[4]);
                break;
            case BeamApplyNames.Opcode:
                Require(Unsigned(operands[0]) <= BeamLinkParameters.MaximumFunctionArity, BeamLambdaDiagnostics.Call);
                break;
            case BeamApplyNames.Last:
                Require(Unsigned(operands[0]) <= BeamLinkParameters.MaximumFunctionArity, BeamLambdaDiagnostics.Call);
                _ = Unsigned(operands[1]);
                break;
            case BeamGeneralOpcodeNames.DynamicCall:
                Require(Unsigned(operands[0]) <= BeamLinkParameters.MaximumFunctionArity, BeamLambdaDiagnostics.Call);
                break;
            case BeamGeneralOpcodeNames.MapFields:
                Branch(operands[0]);
                Source(operands[1]);
                Require(operands[2].Kind == BeamOperandParameters.Count && operands[2].Value == operands.Count - 3, BeamLinkDiagnostics.OperandRole);
                BeamMapOperands.ValidateKeys(
                    structure,
                    operands,
                    3,
                    1
                );
                foreach (BeamOperand key in operands.Skip(3))
                    Source(key);
                break;
            case BeamGeneralOpcodeNames.MapElements:
                Branch(operands[0]);
                Source(operands[1]);
                Require(
                    operands[2].Kind == BeamOperandParameters.Count && operands[2].Value == operands.Count - 3
                    && operands[2].Value % 2 == 0,
                    BeamLinkDiagnostics.OperandRole
                );
                BeamMapOperands.ValidateKeys(
                    structure,
                    operands,
                    3,
                    2
                );
                for (int pair = 3; pair < operands.Count; pair += 2)
                {
                    Source(operands[pair]);
                    Destination(operands[pair + 1]);
                }
                break;
            case BeamGeneralOpcodeNames.MapExact:
            case BeamGeneralOpcodeNames.MapAssociate:
                if (instruction.Definition.Name == BeamGeneralOpcodeNames.MapAssociate)
                    Require(operands[0].Kind == BeamOperandParameters.Failure, BeamLinkDiagnostics.OperandRole);
                else
                    Branch(operands[0]);
                Source(operands[1]);
                Destination(operands[2]);
                _ = Unsigned(operands[3]);
                Require(
                    operands[4].Kind == BeamOperandParameters.Count && operands[4].Value == operands.Count - 5 && operands[4].Value % 2 == 0,
                    BeamLinkDiagnostics.OperandRole
                );
                BeamMapOperands.ValidateKeys(
                    structure,
                    operands,
                    5,
                    2
                );
                foreach (BeamOperand value in operands.Skip(5))
                    Source(value);
                break;
            case BeamGeneralOpcodeNames.UnaryBif:
                Branch(operands[0]);
                CheckImport(structure, Unsigned(operands[1]), 1);
                Source(operands[2]);
                Destination(operands[3]);
                break;
            case BeamGeneralOpcodeNames.UnaryGcBif:
                Branch(operands[0]);
                _ = Unsigned(operands[1]);
                CheckImport(structure, Unsigned(operands[2]), 1);
                Source(operands[3]);
                Destination(operands[4]);
                break;
            case BeamGeneralOpcodeNames.TernaryBif:
                Branch(operands[0]);
                CheckImport(structure, Unsigned(operands[1]), 3);
                foreach (BeamOperand source in operands.Skip(2).Take(3))
                    Source(source);
                Destination(operands[5]);
                break;
            case BeamGeneralOpcodeNames.TernaryGcBif:
                Branch(operands[0]);
                _ = Unsigned(operands[1]);
                CheckImport(structure, Unsigned(operands[2]), 3);
                foreach (BeamOperand source in operands.Skip(3).Take(3))
                    Source(source);
                Destination(operands[6]);
                break;
            case BeamGeneralOpcodeNames.IfEnd:
                break;
            case BeamGeneralOpcodeNames.LocalLast:
                int lastArity = Unsigned(operands[0]);
                Require(
                    operands[1].Kind == BeamOperandParameters.Label
                    && functions.TryGetValue((int)operands[1].Value, out BeamLinkedFunction? lastTarget)
                    && lastTarget.Arity == lastArity,
                    BeamLinkDiagnostics.LocalCall
                );
                _ = Unsigned(operands[2]);
                break;
            case BeamGeneralOpcodeNames.CaseEnd:
                Source(operands[0]);
                break;
            case BeamLinkNames.Label:
            case BeamLinkNames.Line:
                _ = Unsigned(operands[0]);
                break;
            case BeamLinkNames.FunctionInfo:
            case BeamLinkNames.CodeEnd:
            case BeamLinkNames.Return:
            case BeamLinkNames.Send:
            case BeamLinkNames.RemoveMessage:
            case BeamLinkNames.Timeout:
            case BeamNifNames.Start:
            case BeamLinkNames.OnLoad:
                break;
            case BeamOnLoadNames.InitY:
                Require(
                    operands[0].Kind == BeamOperandParameters.Count && operands[0].Value == operands.Count - BeamFrameParameters.TupleCountPrefix,
                    BeamLinkDiagnostics.OperandRole
                );
                foreach (BeamOperand operand in operands.Skip(BeamFrameParameters.TupleCountPrefix))
                    Require(operand.Kind == BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamOnLoadNames.PidTest:
                Branch(operands[0]);
                Source(operands[1]);
                break;
            case BeamOnLoadNames.TaggedTupleTest:
                Branch(operands[0]);
                Source(operands[1]);
                _ = Unsigned(operands[2]);
                Require(operands[3].Kind == BeamOperandParameters.Atom, BeamLinkDiagnostics.OperandRole);
                break;
            case BeamOnLoadNames.TupleElement:
                Source(operands[0]);
                _ = Unsigned(operands[1]);
                Destination(operands[2]);
                break;
            case BeamOnLoadNames.BadMatch:
                Source(operands[0]);
                break;
            case BeamOnLoadNames.ExternalLast:
                CheckImport(structure, Unsigned(operands[1]), Unsigned(operands[0]));
                _ = Unsigned(operands[2]);
                break;
            case BeamLinkNames.Move:
                Source(operands[0]);
                Destination(operands[1]);
                break;
            case BeamLinkNames.IntegerTest:
                Branch(operands[0]);
                Source(operands[1]);
                break;
            case BeamLinkNames.EqualityTest:
                Branch(operands[0]);
                Source(operands[1]);
                Source(operands[2]);
                break;
            case BeamLinkNames.AllocateHeap:
                _ = Unsigned(operands[0]);
                _ = Unsigned(operands[1]);
                _ = Unsigned(operands[2]);
                break;
            case BeamLinkNames.HeapTest:
            case BeamLinkNames.Allocate:
                _ = Unsigned(operands[0]);
                _ = Unsigned(operands[1]);
                break;
            case BeamLinkNames.Deallocate:
                _ = Unsigned(operands[0]);
                break;
            case BeamLinkNames.PutList:
                Source(operands[0]);
                Source(operands[1]);
                Destination(operands[2]);
                break;
            case BeamLinkNames.PutTuple:
                Destination(operands[0]);
                Require(
                    operands[1].Kind == BeamOperandParameters.Count
                    && operands[1].Value == operands.Count - BeamLinkParameters.TupleListPrefixOperands,
                    BeamLinkDiagnostics.OperandRole
                );
                foreach (BeamOperand operand in operands.Skip(BeamLinkParameters.TupleListPrefixOperands))
                    Source(operand);
                break;
            case BeamLinkNames.LoopReceive:
                Branch(operands[0]);
                Destination(operands[1]);
                break;
            case BeamLinkNames.LoopReceiveEnd:
                Branch(operands[0]);
                break;
            case BeamLinkNames.WaitTimeout:
                Branch(operands[0]);
                Source(operands[1]);
                break;
            case BeamLinkNames.Wait:
                Branch(operands[0]);
                break;
            case BeamRegisterNames.Call:
            case BeamLinkNames.LocalCall:
                int localArity = Unsigned(operands[0]);
                Require(
                    operands[1].Kind == BeamOperandParameters.Label
                    && functions.TryGetValue((int)operands[1].Value, out BeamLinkedFunction? target)
                    && target.Arity == localArity,
                    BeamLinkDiagnostics.LocalCall
                );
                break;
            case BeamLinkNames.ExternalCall:
            case BeamLinkNames.ExternalTailCall:
                CheckImport(structure, Unsigned(operands[1]), Unsigned(operands[0]));
                break;
            case BeamLinkNames.GuardBif:
                Branch(operands[0]);
                _ = Unsigned(operands[1]);
                CheckImport(structure, Unsigned(operands[2]), BeamLinkParameters.GuardBifArity);
                Source(operands[3]);
                Source(operands[4]);
                Destination(operands[5]);
                break;
            case BeamLinkNames.Bif:
                CheckImport(structure, Unsigned(operands[0]), 0);
                Destination(operands[1]);
                break;
            case BeamLambdaNames.Create:
                int creationIndex = Unsigned(operands[0]);
                Require(creationIndex < lambdas.Count, BeamLambdaDiagnostics.Creation);
                Destination(operands[1]);
                Require(
                    operands[2].Kind == BeamOperandParameters.Count
                    && operands[2].Value == operands.Count - BeamLambdaNames.EnvironmentPrefixOperands
                    && operands[2].Value == lambdas[creationIndex].FreeVariables,
                    BeamLambdaDiagnostics.Creation
                );
                foreach (BeamOperand operand in operands.Skip(BeamLambdaNames.EnvironmentPrefixOperands))
                    Source(operand);
                break;
            case BeamLambdaNames.Call:
                int funArity = Unsigned(operands[1]);
                Require(funArity <= BeamLinkParameters.MaximumFunctionArity, BeamLambdaDiagnostics.Call);
                if (operands[0].Kind == BeamOperandParameters.Unsigned)
                {
                    int lambdaIndex = Unsigned(operands[0]);
                    Require(
                        lambdaIndex < lambdas.Count
                        && funArity == lambdas[lambdaIndex].TotalArity - lambdas[lambdaIndex].FreeVariables,
                        BeamLambdaDiagnostics.Call
                    );
                }
                else
                {
                    Require(
                        operands[0].Kind == BeamOperandParameters.Atom
                        && structure.Atoms[(int)operands[0].Value] is BeamLambdaNames.Safe or BeamLambdaNames.Unsafe,
                        BeamLambdaDiagnostics.Call
                    );
                }
                Destination(operands[2]);
                break;
            default:
                throw new NotSupportedException(BeamLinkDiagnostics.OpcodeRole);
        }
    }

    private static void CheckImport(BeamStructure structure, int index, int arity)
    {
        Require(index < structure.Imports.Count && structure.Imports[index].Arity == arity, BeamLinkDiagnostics.Import);
    }

    private static void Branch(BeamOperand operand) => Require(operand.Kind is BeamOperandParameters.Label or BeamOperandParameters.Failure, BeamLinkDiagnostics.OperandRole);

    private static void Destination(BeamOperand operand) => Require(operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y, BeamLinkDiagnostics.OperandRole);

    private static void Source(BeamOperand operand) => Require(
        operand.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
        or BeamOperandParameters.Atom or BeamOperandParameters.Nil or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue,
        BeamLinkDiagnostics.OperandRole
    );

    private static int Unsigned(BeamOperand operand)
    {
        Require(operand.Kind == BeamOperandParameters.Unsigned, BeamLinkDiagnostics.FunctionBoundary);

        return (int)operand.Value;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidDataException(message);
    }
}
