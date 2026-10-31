using Erlang.Differential;

namespace Erlang.NifNativeBackend;

public static class UnchangedRegister550Checks
{
    public static async Task Physical()
    {
        foreach (bool owned in new[] { false, true })
        {
            using var compiled = GeneratedModuleCompiler.Compile(UnchangedRegister550CaseNames.Source);
            await using var runtime = NativeOwned516Profile.Create(owned);
            await runtime.InitializeModulesAsync(registry =>
            {
                compiled.Definition.Register(registry);
                registry.Register(
                    UnchangedRegister550CaseNames.Probe,
                    UnchangedRegister550CaseNames.Run,
                    0,
                    Check
                );
            });
            await CpuStatisticsAccess.Actor<Term>(runtime, context => runtime.Modules.Call(context, UnchangedRegister550CaseNames.Module, UnchangedRegister550CaseNames.Run));
        }
    }

    private static ValueTask<Term> Check(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var state = new BeamRegisterState(null, context);
        try
        {
            Require(state.HasOwnedRoots507);
            var x = new BeamOperand(BeamOperandParameters.X, 0);
            var y = new BeamOperand(BeamOperandParameters.Y, 0);
            state.Write(0, Term.Tuple(Term.I(1)));
            Term value = state.Read(0);
            state.Allocate(2);
            state.WriteY(0, value);
            state.WriteY(1, value);
            long before = context.Runtime.BinaryRegistry512.TraversalSnapshot514.RetainOperations;
            state.CopyRegister(x, x);
            state.CopyRegister(y, y);
            Require(ReferenceEquals(state.Read(0), value) && ReferenceEquals(state.ReadY(0), value));
            Require(context.Runtime.BinaryRegistry512.TraversalSnapshot514.RetainOperations == before);
            state.Write(0, value); // Adopt/Host-result entry still takes its original path.
            Require(context.Runtime.BinaryRegistry512.TraversalSnapshot514.RetainOperations > before);
            using (BeamRegisterState clone = state.Clone())
            {
                Require(ReferenceEquals(clone.Read(0), value) && ReferenceEquals(clone.ReadY(0), value));
            }
            state.Trim(1, 1);
            Require(ReferenceEquals(state.ReadY(0), value));
            state.Deallocate(1);
            var xb = new BeamOperand(BeamOperandParameters.X, 1);
            state.Write(1, new BitString(new byte[] { 1, 2 }));
            var binary = (BitString)state.Read(1);
            state.WriteBinaryMatchContext(xb, new(binary));
            state.WriteSlot(xb, BeamRegisterSlot.FromTerm(binary));
            Require(state.ReadSlot(xb).Kind == BeamRegisterSlotKind.Term && ReferenceEquals(state.Read(1), binary));
            using (var local = new BeamLocalFrame(2, null, context))
            {
                local.InitializeBlank(0);
                var function = new BeamLinkedFunction(
                    UnchangedRegister550CaseNames.Module,
                    UnchangedRegister550CaseNames.Run,
                    0,
                    0,
                    0,
                    0,
                    0
                );
                var checkpoint = new BeamHandlerCheckpoint(
                    function,
                    0,
                    0,
                    0,
                    0,
                    0
                );
                BeamHandlerTag handler = local.InstallHandler(0, checkpoint);
                bool rejected = false;
                try
                {
                    local.WriteSlot(0, BeamRegisterSlot.FromTerm(Nil.Value));
                }
                catch (InvalidDataException error) when (error.Message == BeamRegisterProtocolNames.Handler)
                {
                    rejected = true;
                }
                Require(rejected);
                local.RemoveHandler(0, handler);
                Require(!local.HasHandlers && local.ReadSlot(0).Kind == BeamRegisterSlotKind.Term && local.Read(0) is Nil);
                local.Write(1, binary);
                local.WriteSlot(1, BeamRegisterSlot.FromBinaryMatchContext(new(binary)));
                local.WriteSlot(1, BeamRegisterSlot.FromTerm(binary));
                Require(local.ReadSlot(1).Kind == BeamRegisterSlotKind.Term && ReferenceEquals(local.Read(1), binary));
            }
            state.Dispose();
            bool closed = false;
            try
            {
                state.WriteSlot(x, BeamRegisterSlot.FromTerm(value));
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            Require(closed);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        finally
        {
            state.Dispose();
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(UnchangedRegister550CaseNames.Failure);
    }
}
