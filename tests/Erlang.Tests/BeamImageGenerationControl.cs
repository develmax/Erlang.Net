using Erlang;
using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal sealed class BeamImageGenerationControl
{
    private readonly List<ModuleCodeImage> images = [];
    internal List<object> Events { get; } = [];

    internal void Install(ProcessRuntime runtime, byte[][] generations)
    {
        string[] allowed = generations.Select(Hash).ToArray();
        runtime.Modules.Register(
            BeamImageGenerationNames.Controller,
            BeamImageGenerationNames.Load,
            1,
            (_, arguments) =>
        {
            if (arguments[0] is not BitString { IsBinary: true } binary)
                throw new InvalidDataException(BeamImageGenerationNames.ExactGenerationBinaryRequired);
            byte[] bytes = binary.ToArray();
            string hash = Hash(bytes);
            int generation = Array.IndexOf(allowed, hash);
            if (generation < 0)
                throw new InvalidDataException(BeamImageGenerationNames.UnboundGeneration);
            PreparedBeamImage prepared = runtime.Modules.PrepareBeam(bytes, new BeamDependencySelection([BeamImageGenerationNames.ErLang]));
            if (prepared.Name != BeamImageGenerationNames.Module || prepared.HasOnLoad)
                throw new InvalidDataException(BeamImageGenerationNames.InvalidGenerationModule);
            ModuleCodeImage? before = runtime.Modules.CurrentCodeImage(BeamImageGenerationNames.Module);
            BeamPublicationStatus status = runtime.Modules.PublishPreparedBeam([prepared]).Status;
            if (status == BeamPublicationStatus.Published)
            {
                ModuleCodeImage current = runtime.Modules.CurrentCodeImage(BeamImageGenerationNames.Module)!;
                images.Add(current);
                foreach (var entry in runtime.Modules.Exports.Where(value => value.Module == BeamImageGenerationNames.Module))
                    if (!ReferenceEquals(prepared, runtime.Modules.SourceOwner(entry.Module, entry.Function, entry.Arity)))
                        throw new InvalidDataException(BeamImageGenerationNames.GenerationOwnerMismatch);
                if (ReferenceEquals(current, before))
                    throw new InvalidDataException(BeamImageGenerationNames.ReusedImageIdentity);
            }
            else if (status != BeamPublicationStatus.NotPurged || !ReferenceEquals(before, runtime.Modules.CurrentCodeImage(BeamImageGenerationNames.Module)))
                throw new InvalidDataException(BeamImageGenerationNames.UnexpectedPublication);
            Events.Add(new { Kind = BeamImageGenerationNames.Load, Generation = generation + 1, Sha256 = hash, Status = status.ToString() });

            return ValueTask.FromResult<Term>(status == BeamPublicationStatus.Published
                ? Term.Tuple(Term.A(BeamImageGenerationNames.ModuleTag), Term.A(BeamImageGenerationNames.Module))
                : Term.Tuple(Term.A(BeamImageGenerationNames.Error), Term.A(BeamImageGenerationNames.NotPurged)));
        }
        );
        runtime.Modules.Register(
            BeamImageGenerationNames.Controller,
            BeamImageGenerationNames.SoftPurge,
            0,
            (context, _) => Soft(runtime, context)
        );
    }

    private async ValueTask<Term> Soft(ProcessRuntime runtime, ProcessContext requester)
    {
        var purger = (SemaphoreSlim)typeof(ProcessRuntime).GetField(BeamImageGenerationNames.Purger, BeamImageGenerationNames.Members)!.GetValue(runtime)!;
        await purger.WaitAsync(requester.Cancellation);
        try
        {
            object? session = await AwaitUnknown(Call(
                runtime.Modules,
                BeamImageGenerationNames.PreparePurge,
                requester,
                BeamImageGenerationNames.Module
            )!);
            if (session is null)
            {
                Events.Add(new { Kind = BeamImageGenerationNames.SoftPurge, HasOldImage = false, Users = 0, Completed = true });

                return Term.A(BeamImageGenerationNames.True);
            }
            bool settled = false;
            try
            {
                ModuleCodeImage image = (ModuleCodeImage)Property(session, BeamImageGenerationNames.Image)!;
                int users = 0;
                foreach (ProcessContext target in Snapshot(runtime))
                {
                    Term checkedCode = await (ValueTask<Term>)Call(
                        runtime,
                        BeamImageGenerationNames.CheckImage,
                        requester,
                        target.Self,
                        image,
                        false,
                        Nil.Value
                    )!;
                    if (checkedCode.Equals(Term.A(BeamImageGenerationNames.True)))
                        users++;
                    else if ((bool)Call(target, BeamImageGenerationNames.IsExecuting, image)!)
                        await Unwound(target).WaitAsync(requester.Cancellation);
                }
                bool complete = users == 0;
                bool result = await (ValueTask<bool>)Call(
                    runtime.Modules,
                    BeamImageGenerationNames.FinishPurge,
                    session,
                    complete
                )!;
                settled = true;
                if (result != complete || runtime.Modules.PendingPurge(image) is not null)
                    throw new InvalidDataException(BeamImageGenerationNames.PurgeFenceNotSettled);
                Events.Add(new { Kind = BeamImageGenerationNames.SoftPurge, HasOldImage = true, Users = users, Completed = result, PhysicalImageModule = image.Module });

                return Term.A(result ? BeamImageGenerationNames.True : BeamImageGenerationNames.False);
            }
            finally
            {
                if (!settled)
                    await (ValueTask<bool>)Call(
                        runtime.Modules,
                        BeamImageGenerationNames.FinishPurge,
                        session,
                        false
                    )!;
            }
        }
        finally
        {
            purger.Release();
        }
    }

    internal async Task Settle(ProcessRuntime runtime, ProcessContext actor, TimeSpan timeout)
    {
        await Unwound(actor).WaitAsync(timeout);
        foreach (ProcessContext context in Snapshot(runtime).Append(actor).Distinct())
            foreach (ModuleCodeImage image in images)
                if ((bool)Call(context, BeamImageGenerationNames.IsExecuting, image)!)
                    throw new InvalidDataException(BeamImageGenerationNames.RetainedGenerationLease);
        if (runtime.Modules.HasOldCode(BeamImageGenerationNames.Module))
            throw new InvalidDataException(BeamImageGenerationNames.OldGenerationNotPurged);
    }

    private static ProcessContext[] Snapshot(ProcessRuntime runtime)
    {
        object gate = typeof(ProcessRuntime).GetField(BeamImageGenerationNames.Gate, BeamImageGenerationNames.Members)!.GetValue(runtime)!;
        lock (gate)
        {
            var active = (Dictionary<Pid, ProcessContext>)typeof(ProcessRuntime).GetField(BeamImageGenerationNames.Processes, BeamImageGenerationNames.Members)!.GetValue(runtime)!;
            var executing = (HashSet<ProcessContext>)typeof(ProcessRuntime).GetField(BeamImageGenerationNames.Executing, BeamImageGenerationNames.Members)!.GetValue(runtime)!;

            return active.Values.Concat(executing).Distinct().ToArray();
        }
    }

    private static Task Unwound(ProcessContext context)
        => ((TaskCompletionSource)Property(context, BeamImageGenerationNames.Unwound)!).Task;

    private static object? Property(object value, string name)
        => value.GetType().GetProperty(name, BeamImageGenerationNames.Members)!.GetValue(value);

    private static object? Call(object value, string name, params object?[] arguments)
        => value.GetType().GetMethods(BeamImageGenerationNames.Members).Single(method => method.Name == name && method.GetParameters().Length == arguments.Length).Invoke(value, arguments);

    private static async Task<object?> AwaitUnknown(object value)
    {
        var task = (Task)value.GetType().GetMethod(BeamImageGenerationNames.AsTask)!.Invoke(value, null)!;
        await task;

        return Property(task, BeamImageGenerationNames.Result);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
