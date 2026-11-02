using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

public static partial class SourceLoadNifHostChecks
{
    private static async Task ActualOpcodeCallers()
    {
        Assembly assembly = typeof(SourceLoadNifHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ActualOpcodeLoadNifHostNames.Resource, StringComparison.Ordinal));
        using Stream resource = assembly.GetManifestResourceStream(name)!;
        using var contents = new MemoryStream();
        resource.CopyTo(contents);
        byte[] bytes = contents.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == ActualOpcodeLoadNifHostNames.Sha256);
        ActualOpcodeMetadata(bytes);
        foreach (string function in new[] { ActualOpcodeLoadNifHostNames.NonTail, ActualOpcodeLoadNifHostNames.TailLast, ActualOpcodeLoadNifHostNames.TailOnly })
            foreach (bool registered in new[] { false, true })
                await ActualOpcodeCaller(bytes, function, registered);
    }

    private static void ActualOpcodeMetadata(byte[] bytes)
    {
        object Property(object value, string name) => value.GetType().GetProperty(name, ActualOpcodeLoadNifHostNames.Members)!.GetValue(value)!;

        void Set(object value, string name, object replacement) => value.GetType().GetProperty(name, ActualOpcodeLoadNifHostNames.Members)!.SetValue(value, replacement);

        string InstructionName(object instruction) => (string)Property(Property(instruction, ActualOpcodeLoadNifHostNames.Definition), ActualOpcodeLoadNifHostNames.DefinitionName);
        foreach (int arity in new[] { 0, 1 })
        {
            BeamModule standalone = BeamModule.Prepare(bytes);
            object code = typeof(BeamModule).GetField(ActualOpcodeLoadNifHostNames.Code, ActualOpcodeLoadNifHostNames.Members)!.GetValue(standalone)!;
            object function = ((System.Collections.IEnumerable)Property(code, ActualOpcodeLoadNifHostNames.Functions)).Cast<object>()
                .Single(value => (string)Property(value, ActualOpcodeLoadNifHostNames.FunctionName) == ActualOpcodeLoadNifHostNames.ModuleInfo
                    && (int)Property(value, ActualOpcodeLoadNifHostNames.FunctionArity) == arity);
            object[] body = ((System.Collections.IEnumerable)Property(code, ActualOpcodeLoadNifHostNames.Instructions)).Cast<object>()
                .Skip((int)Property(function, ActualOpcodeLoadNifHostNames.FirstInstruction))
                .Take((int)Property(function, ActualOpcodeLoadNifHostNames.EndInstruction) - (int)Property(function, ActualOpcodeLoadNifHostNames.FirstInstruction))
                .Where(instruction => InstructionName(instruction) is not (ActualOpcodeLoadNifHostNames.Label or ActualOpcodeLoadNifHostNames.Line)).ToArray();
            Require(InstructionName(body[0]) == ActualOpcodeLoadNifHostNames.Start);
            MethodInfo classify = typeof(BeamModule).Assembly.GetType(ActualOpcodeLoadNifHostNames.MetadataType, true)!
                .GetMethod(ActualOpcodeLoadNifHostNames.IsGenerated, ActualOpcodeLoadNifHostNames.StaticMembers)!;

            bool Classify(object[] instructions)
            {
                Array typed = Array.CreateInstance(body[0].GetType(), instructions.Length);
                for (int index = 0; index < instructions.Length; index++)
                    typed.SetValue(instructions[index], index);
                Set(code, ActualOpcodeLoadNifHostNames.Instructions, typed);
                Set(function, ActualOpcodeLoadNifHostNames.FirstInstruction, 0);
                Set(function, ActualOpcodeLoadNifHostNames.EndInstruction, instructions.Length);

                return (bool)classify.Invoke(null, [code, function])!;
            }
            Require(Classify(body));
            Require(Classify(body.Skip(1).ToArray()));
            Require(!Classify([body[1], body[0], .. body.Skip(2)]));
            Require(!Classify([body[0], .. body]));
            Set(body[0], ActualOpcodeLoadNifHostNames.Operands, Property(body[1], ActualOpcodeLoadNifHostNames.Operands));
            Require(!Classify(body));
        }
    }

    private static async Task ActualOpcodeCaller(byte[] bytes, string function, bool registered)
    {
        await using var runtime = new ProcessRuntime();
        BeamModule standalone = BeamModule.Prepare(bytes);
        bool nonTail = function == ActualOpcodeLoadNifHostNames.NonTail;
        bool dictionaryMarker = function == ActualOpcodeLoadNifHostNames.TailLast;
        int arity = function == ActualOpcodeLoadNifHostNames.TailOnly ? SourceLoadNifHostNames.Arity : ActualOpcodeLoadNifHostNames.MarkerArity;
        Require(standalone.Supports(function, arity));
        PreparedBeamImage? prepared = null;
        if (registered)
        {
            prepared = runtime.Modules.PrepareBeam(bytes);
            Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        }
        object expectedOwner = (object?)prepared ?? standalone;
        object execution = typeof(BeamModule).GetField(ActualOpcodeLoadNifHostNames.Execution, ActualOpcodeLoadNifHostNames.Members)!.GetValue(standalone)!;
        ModuleCodeImage image = registered
            ? runtime.Modules.CurrentCodeImage(standalone.Name)!
            : (ModuleCodeImage)execution.GetType().GetProperty(ActualOpcodeLoadNifHostNames.Image, ActualOpcodeLoadNifHostNames.Members)!.GetValue(execution)!;
        Term path = Term.String(ActualOpcodeLoadNifHostNames.Path);
        Term info = Term.I(SourceLoadNifHostNames.Marker);
        Term marker = Term.A(ActualOpcodeLoadNifHostNames.Marker);
        Term key = Term.A(ActualOpcodeLoadNifHostNames.DictionaryKey);
        Term[] arguments = arity == SourceLoadNifHostNames.Arity ? [path, info] : [path, info, marker];

        ValueTask<Term> Invoke(ProcessContext context) => registered
            ? runtime.Modules.Call(
                context,
                standalone.Name,
                function,
                arguments
            )
            : standalone.Invoke(context, function, arguments);

        Term Outcome(Term result) => nonTail ? Term.Tuple(marker, result) : result;

        void CheckMarker(ProcessContext context)
        {
            Require(context.Dictionary.TryGetValue(key, out Term? saved) == dictionaryMarker);
            if (dictionaryMarker)
                Require(saved!.Equals(marker));
        }

        void CheckActive(ProcessContext context, Term[] evaluated)
        {
            Require(ReferenceEquals(context.ExecutingSourceImage(expectedOwner), image));
            Require(References(context, image) == SourceLoadNifHostNames.OwnedDirectReferences);
            Require(evaluated.Length == SourceLoadNifHostNames.Arity && evaluated[0].Equals(path) && evaluated[1].Equals(info));
        }
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            if (registered)
            {
                Term fallback = await Invoke(context);
                if (nonTail)
                {
                    Require(fallback is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(marker));
                    fallback = ((TupleTerm)fallback).Items[1];
                }
                RequireMissingLibraryFailure(fallback);
            }
            else
            {
                await NifAdmissionHostBridge.Failure(Invoke(context).AsTask(), SourceLoadNifHostNames.UnknownImage);
            }
            CheckMarker(context);
            Require(References(context, image) == 0);
        }
        );
        int calls = 0;
        bool fail = false;
        bool cancel = false;
        bool cancellationMarkerObserved = false;
        object? escaped = null;
        var failure = new InvalidOperationException(ActualOpcodeLoadNifHostNames.Failure);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finalizing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Install(
            runtime.Modules,
            async (context, capability, evaluated) =>
        {
            calls++;
            escaped = capability;
            SourceCodeEntry caller = Consume(capability, context);
            Refused(capability, context);
            Require(ReferenceEquals(caller.Owner, expectedOwner) && ReferenceEquals(caller.Image, image));
            CheckActive(context, evaluated);
            CheckMarker(context);
            if (cancel)
            {
                cancellationMarkerObserved = true;
                entered.TrySetResult(context);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, context.Cancellation);
                }
                finally
                {
                    CheckActive(context, evaluated);
                    finalizing.TrySetResult();
                    await release.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
                    CheckActive(context, evaluated);
                    Refused(capability, context);
                }
            }
            await Task.Yield();
            CheckActive(context, evaluated);
            if (fail)
                throw failure;

            return evaluated[1];
        }
        );
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            Require((await Invoke(context)).Equals(Outcome(info)) && calls == ActualOpcodeLoadNifHostNames.SuccessCalls);
            CheckMarker(context);
            Refused(escaped!, context);
            Require(References(context, image) == 0);
        }
        );
        fail = true;
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            bool failed = false;
            try
            {
                await Invoke(context);
            }
            catch (Exception caught) when (ReferenceEquals(caught, failure))
            {
                failed = true;
            }
            Require(failed && calls == ActualOpcodeLoadNifHostNames.FailureCalls);
            CheckMarker(context);
            Refused(escaped!, context);
            Require(References(context, image) == 0);
        }
        );
        fail = false;
        cancel = true;
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                bool canceled = false;
                try
                {
                    await Invoke(context);
                }
                catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
                {
                    canceled = true;
                }
                Require(canceled && calls == ActualOpcodeLoadNifHostNames.CancellationCalls);
                Require(cancellationMarkerObserved);
                Refused(escaped!, context);
                Require(References(context, image) == 0);
                completed.TrySetResult();
            }
            catch (Exception caught)
            {
                entered.TrySetException(caught);
                completed.TrySetException(caught);
            }

            return Nil.Value;
        });
        ProcessContext current = await entered.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
        try
        {
            ProcessHandle killer = runtime.Spawn(context =>
            {
                runtime.Exit(context, actor.Pid, Term.A(SourceLoadNifHostNames.Kill));

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            await killer.Completion.WaitAsync(SourceLoadNifHostNames.Deadline);
            await finalizing.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
            Require(!completed.Task.IsCompleted && References(current, image) == SourceLoadNifHostNames.OwnedDirectReferences);
        }
        finally
        {
            release.TrySetResult();
        }
        await completed.Task.WaitAsync(SourceLoadNifHostNames.Deadline);
        await actor.Completion.WaitAsync(SourceLoadNifHostNames.Deadline);
        Require(References(current, image) == 0);
    }
}
