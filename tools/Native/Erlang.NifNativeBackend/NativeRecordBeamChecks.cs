using System.Reflection;

namespace Erlang.NifNativeBackend;

public static class NativeRecordBeamChecks
{
    public static async Task OriginalWhole()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = NativeOwned516Profile.Create(owned);
            await runtime.InitializeModulesAsync(registry =>
            {
                var dependencies = new BeamDependencySelection([
                    NativeRecordBeamCaseNames.Module,
                    NativeRecordBeamCaseNames.Probe,
                    NativeRecordBeamCaseNames.Observer,
                    ErlangModuleNames.Module
                ]);
                foreach (string image in new[] { NativeRecordBeamImages.NativeRecordPhysical, NativeRecordBeamImages.NativeRecordPhysicalProbe, NativeRecordBeamImages.NativeRecordPhysicalObserver })
                    Require(registry.PublishPreparedBeam(registry.PrepareBeam(Convert.FromBase64String(image), dependencies)).Status == BeamPublicationStatus.Published);
            });
            Term actual = await CpuStatisticsAccess.Actor<Term>(
                runtime,
                async context => await runtime.Modules.Call(context, NativeRecordBeamCaseNames.Observer, NativeRecordBeamCaseNames.Run)
            );
            byte[] expectedBytes = Convert.FromBase64String(NativeRecordBeamImages.Original);
            Term expected = ExternalTermFormat.Decode(expectedBytes);
            Require(actual.Equals(expected) && ExternalTermFormat.Encode(actual).AsSpan().SequenceEqual(expectedBytes));
        }
    }

    public static void OperandGuards()
    {
        BeamLinkedCode code = BeamLinkedCodeReader.Read(BeamStructureReader.Read(Convert.FromBase64String(NativeRecordBeamImages.NativeRecordPhysical)));
        BeamInstruction get = code.Instructions.Single(instruction => instruction.Definition.Name == BeamNativeRecordAccessNames.Elements);
        BeamInstruction update = code.Instructions.Single(instruction => instruction.Definition.Name == BeamRecordConstructionNames.Opcode
            && instruction.Operands[BeamRecordConstructionNames.Source].Kind == BeamOperandParameters.Y);
        MethodInfo validate = typeof(BeamLinkedCodeReader).GetMethod(NativeRecordBeamCaseNames.ValidateRoles, BindingFlags.Static | BindingFlags.NonPublic)!;
        var functions = code.Functions.ToDictionary(function => function.EntryLabel);
        Accept(get);
        Accept(update);
        Reject(get, 1, new(BeamOperandParameters.Atom, 1));
        Reject(get, 2, new(BeamOperandParameters.Count, 1));
        Reject(get, 3, new(BeamOperandParameters.X, 0));
        Reject(get, 4, new(BeamOperandParameters.Integer, 0));
        Reject(update, BeamRecordConstructionNames.Source, new(BeamOperandParameters.Atom, 1));
        var repeated = get.Operands.ToList();
        repeated[2] = new(BeamOperandParameters.Count, 4);
        repeated.Add(get.Operands[3]);
        repeated.Add(new(BeamOperandParameters.Y, 1));
        RejectInstruction(get with { Operands = repeated });

        void Accept(BeamInstruction instruction) => validate.Invoke(null, [instruction, code.Structure, functions, code.Lambdas]);

        void Reject(BeamInstruction instruction, int index, BeamOperand value)
        {
            var changed = instruction.Operands.ToArray();
            changed[index] = value;
            RejectInstruction(instruction with { Operands = changed });
        }

        void RejectInstruction(BeamInstruction instruction)
        {
            try
            {
                Accept(instruction);
            }
            catch (TargetInvocationException error) when (error.InnerException is InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException(NativeRecordBeamCaseNames.Failure);
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NativeRecordBeamCaseNames.Failure);
    }
}
