using System.Numerics;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<(string Module, string Function, int Arity), ErlangFunction> functions = new();
    private readonly Dictionary<string, Atom> loadedModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Atom> preloadedModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleCodeDescriptor> codeDescriptors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleInformationSnapshot> informationSnapshots = new(StringComparer.Ordinal);
    private readonly HashSet<string> knownModules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModuleCodeSnapshot> oldCode = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Module, string Function, int Arity), object> sourceOwners = new();
    private readonly Dictionary<string, ModuleCodeImage> codeImages = new(StringComparer.Ordinal);
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ModuleCodeImage, ModuleCodePurgeState> purgeStates = new();

    public SourceCodeLease ReserveSourceCode(ProcessContext context, ModuleCodeImage image)
    {
        lock (gate)
        {
            var state = purgeStates.GetOrCreateValue(image);
            if (state.Retired || state.Pending is not null && !context.IsExecutingCode(image))
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);

            return context.ReserveSourceCode(image);
        }
    }

    public ValueTask<SourceCodeLease> AcquireSourceCode(ProcessContext context, ModuleCodeImage image)
        => AcquireSourceEntry(context, image, SourceCodeAdmissionKind.Continuation);

    public async ValueTask<SourceFunctionAdmission> AdmitSourceFunction(
        ProcessContext context,
        string module,
        BitString fingerprint,
        object owner,
        ModuleCodeImage? capturedImage,
        FunctionTerm function
    )
    {
        var admission = await AdmitSourceFunction(
            context,
            module,
            fingerprint,
            owner,
            capturedImage,
            function,
            null
        );
        if (admission.Kind == SourceFunctionAdmissionKind.MissingCurrentLocalFunction)
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);

        return admission;
    }

    internal async ValueTask<SourceFunctionAdmission> AdmitSourceFunction(
        ProcessContext context,
        string module,
        BitString fingerprint,
        object owner,
        ModuleCodeImage? capturedImage,
        FunctionTerm function,
        LocalFunctionInvocation? invocation
    )
    {
        while (true)
        {
            Task? pending;
            lock (gate)
            {
                var current = SourceForCode(module, fingerprint);
                if (current is not null && current.Owner.GetType() != owner.GetType())
                    current = null;
                var image = current?.Image ?? capturedImage;
                var selectedOwner = current?.Owner ?? owner;
                if (image is null)
                {
                    if (invocation is not null && !invocation.IsCurrent)
                        return new(selectedOwner, null, null) { Kind = SourceFunctionAdmissionKind.Retry };

                    return new(selectedOwner, null, null);
                }
                var state = purgeStates.GetOrCreateValue(image);
                bool retired = state.Retired;
                bool currentModule = retired && loadedModules.ContainsKey(module);
                pending = retired ? null : state.Pending?.Task;
                if (invocation is not null && !invocation.IsCurrent)
                    return new(selectedOwner, null, null) { Kind = SourceFunctionAdmissionKind.Retry };
                if (retired)
                {
                    if (currentModule)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), function));

                    return new(selectedOwner, null, null) { Kind = SourceFunctionAdmissionKind.MissingCurrentLocalFunction };
                }
                if (pending is null)
                    return new(selectedOwner, image, context.ReserveSourceCode(image));
            }
            await context.Mailbox.WaitForControlCompletion(pending, context.Cancellation);
        }
    }

    public Task? PendingPurge(ModuleCodeImage image)
    {
        lock (gate)
            return purgeStates.GetOrCreateValue(image).Pending?.Task;
    }

    public void Register(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        using var modification = CodeModification.Enter();
        AtomNameRegistry.Observe(module);
        AtomNameRegistry.Observe(function);
        lock (gate)
        {
            functions.Add((module, function, arity), body);
            intrinsicBindings.Remove((module, function, arity));
            PublishModule(module);
            informationSnapshots.Remove(module);
        }
    }

    public void RegisterSource(
        string module,
        string function,
        int arity,
        ErlangFunction body,
        object sourceOwner
    )
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(sourceOwner);
        lock (gate)
        {
            ThrowIfSourceClosing();
            Register(
                module,
                function,
                arity,
                body
            );
            sourceOwners.Add((module, function, arity), sourceOwner);
        }
    }

    public object? SourceOwner(string module, string function, int arity)
    {
        lock (gate)
            return sourceOwners.GetValueOrDefault((module, function, arity));
    }

    public SourceCodeEntry? SourceEntry(string module, string function, int arity)
    {
        lock (gate)
        {
            if (!sourceOwners.TryGetValue((module, function, arity), out var owner))
                return null;

            return new SourceCodeEntry(owner, codeImages[module]);
        }
    }

    public ModuleCodeImage? CurrentCodeImage(string module)
    {
        lock (gate)
            return codeImages.GetValueOrDefault(module);
    }

    public SourceCodeEntry? SourceForCode(string module, BitString fingerprint)
    {
        lock (gate)
        {
            if (!codeDescriptors.TryGetValue(module, out var descriptor) || descriptor.SourceOwner is null || !descriptor.Fingerprint.Equals(fingerprint) ||
                !sourceOwners.Any(entry => entry.Key.Module == module && ReferenceEquals(entry.Value, descriptor.SourceOwner)))
                return null;

            return new SourceCodeEntry(descriptor.SourceOwner, codeImages[module]);
        }
    }

    public ModuleCodeImage? SourceImage(string module, object owner)
    {
        lock (gate)
        {
            if (sourceOwners.Any(entry => entry.Key.Module == module && ReferenceEquals(entry.Value, owner)))
                return codeImages[module];
            if (oldCode.TryGetValue(module, out var old) && old.SourceOwners.Any(value => ReferenceEquals(value, owner)))
                return old.Image;

            return null;
        }
    }

    public bool IsExecutingOldCode(ProcessContext context, string module)
    {
        lock (gate)
            return oldCode.TryGetValue(module, out var old) && context.IsExecutingCode(old.Image);
    }

    internal Term CallMatchSpecGuard(ProcessContext context, string function, Term[] arguments)
    {
        if (!MatchSpecOperations.Pure.Contains((function, arguments.Length)))
            throw MatchSpecNames.UnsupportedAction(function, arguments.Length);
        if (function == ErlangModuleNames.IsInteger && arguments.Length == 3)
        {
            if (arguments[1] is not Integer minimum || arguments[2] is not Integer maximum)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return CoreModules.Boolean(arguments[0] is Integer number && number.Value >= minimum.Value && number.Value <= maximum.Value);
        }
        ErlangFunction body;
        lock (gate)
        {
            if (!functions.TryGetValue((ErlangModuleNames.Module, function, arguments.Length), out body!))
                throw MatchSpecNames.UnsupportedAction(function, arguments.Length);
        }

        ValueTask<Term> pending = body(context, arguments);
        if (!pending.IsCompleted)
            throw MatchSpecNames.UnsupportedAction(function, arguments.Length);

        return pending.GetAwaiter().GetResult();
    }

    public ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    ) => CallWithDispatch(
        context,
        module,
        function,
        arguments,
        out _
    );

    internal ValueTask<Term> CallWithDispatch(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        out ModuleCallDispatchKind dispatch
    )
    {
        dispatch = ModuleCallDispatchKind.Direct;
        context.Runtime.ThrowIfHalted();
        foreach (Term argument in arguments)
            context.Runtime.ObserveIncomingTerm(argument);
        ErlangFunction? body;
        ModuleCodeImage? image;
        IDisposable? code;
        bool handler = false;
        Atom selectedHandler = context.ErrorHandler;
        lock (gate)
        {
            functions.TryGetValue((module, function, arguments.Length), out body);
            if (body is null)
            {
                if (!functions.TryGetValue((selectedHandler.Name, ProcessErrorHandlerNames.UndefinedFunction, ProcessErrorHandlerNames.Arity), out body))
                    throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
                arguments = [Term.A(module), Term.A(function), Cons.From(arguments)];
                module = selectedHandler.Name;
                function = ProcessErrorHandlerNames.UndefinedFunction;
                handler = true;
            }
            if (handler)
            {
                image = SelectErrorHandlerImage(module, function, body!);
                code = image is null ? null : context.EnterSourceCode(image, body!);
                dispatch = ModuleCallDispatchKind.UndefinedFunctionHandler;
            }
            else
            {
                var signature = (module, function, arguments.Length);
                ModuleCodeImage? currentImage = codeImages.GetValueOrDefault(module);
                image = sourceOwners.ContainsKey(signature) || IsIntrinsic(signature, body, currentImage) ? null : currentImage;
                code = image is null || body is null ? null : context.EnterSourceCode(image, body);
            }
        }

        return handler
            ? InvokeErrorHandler(
                context,
                module,
                function,
                arguments,
                body!,
                image,
                code
            )
            : InvokeSelected(
                context,
                module,
                function,
                arguments,
                body!,
                image,
                code
            );
    }

    private ValueTask<Term> InvokeSelected(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        ErlangFunction body,
        ModuleCodeImage? image,
        IDisposable? code
    )
    {
        ProcessTraceInvocation? invocation = null;
        if (image is null)
        {
            invocation = context.Runtime.ProcessTracing.Called(
                context,
                module,
                function,
                arguments
            );
            context.SavedCalls.Record(module, function, arguments.Length);
            try
            {
                return GuardHaltBoundary(context.Runtime.ProcessTracing.Complete(context, invocation, body(context, arguments)), context);
            }
            catch (ErlangException exception)
            {
                context.Runtime.ProcessTracing.Failed(context, invocation, exception);
                throw;
            }
        }
        try
        {
            invocation = context.Runtime.ProcessTracing.Called(
                context,
                module,
                function,
                arguments
            );
            context.SavedCalls.Record(module, function, arguments.Length);
            ValueTask<Term> pending = context.Runtime.ProcessTracing.Complete(context, invocation, body(context, arguments));
            if (pending.IsCompletedSuccessfully)
            {
                context.Runtime.ThrowIfHalted();
                context.Runtime.ObserveIncomingTerm(pending.Result);
                code!.Dispose();

                return pending;
            }

            return AwaitCallback(pending, code!, context);
        }
        catch (Exception exception)
        {
            code!.Dispose();
            if (exception is ErlangException failure)
                context.Runtime.ProcessTracing.Failed(context, invocation, failure);
            throw;
        }
        finally
        {
            context.RestoreSourceCodeFlow(code!);
        }
    }

    private static ValueTask<Term> GuardHaltBoundary(ValueTask<Term> pending, ProcessContext context)
    {
        if (pending.IsCompletedSuccessfully)
        {
            context.Runtime.ThrowIfHalted();
            context.Runtime.ObserveIncomingTerm(pending.Result);

            return pending;
        }

        return AwaitHaltBoundary(pending, context);
    }

    private static async ValueTask<Term> AwaitHaltBoundary(ValueTask<Term> pending, ProcessContext context)
    {
        Term result = await pending;
        context.Runtime.ThrowIfHalted();
        context.Runtime.ObserveIncomingTerm(result);

        return result;
    }

    private static async ValueTask<Term> AwaitCallback(ValueTask<Term> pending, IDisposable code, ProcessContext context)
    {
        using (code)
            return await AwaitHaltBoundary(pending, context);
    }

    public bool HasExport(string module, string function, int arity)
    {
        lock (gate)
            return functions.ContainsKey((module, function, arity));
    }

    public void PublishModule(string module)
    {
        using var modification = CodeModification.Enter();
        AtomNameRegistry.Observe(module);
        lock (gate)
        {
            knownModules.Add(module);
            if (!loadedModules.ContainsKey(module))
                loadedModules.Add(module, Term.A(module));
            if (!codeImages.ContainsKey(module))
                codeImages.Add(module, new ModuleCodeImage(module));
        }
    }

    public bool IsModuleLoaded(string module)
    {
        lock (gate)
            return loadedModules.ContainsKey(module);
    }

    public bool HasOldCode(string module)
    {
        lock (gate)
            return oldCode.ContainsKey(module);
    }

    public Term DeleteModule(string module)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        lock (gate)
        {
            ThrowIfSourceClosing();
            if (!knownModules.Contains(module))
                return Term.A(ModuleCodeLifecycleNames.UnknownModule);
            if (oldCode.ContainsKey(module))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var entries = functions.Where(entry => entry.Key.Module == module).Select(entry => (entry.Key, entry.Value)).ToArray();
            codeDescriptors.TryGetValue(module, out var descriptor);
            if (entries.Length != 0 || descriptor is not null)
                oldCode.Add(
                    module,
                    new ModuleCodeSnapshot(
                        Array.AsReadOnly(entries),
                        descriptor,
                        codeImages[module],
                        Array.AsReadOnly(sourceOwners.Where(entry => entry.Key.Module == module).Select(entry => entry.Value).ToArray())
                    )
                );
            foreach (var entry in entries)
            {
                functions.Remove(entry.Key);
                intrinsicBindings.Remove(entry.Key);
                sourceOwners.Remove(entry.Key);
            }
            loadedModules.Remove(module);
            codeDescriptors.Remove(module);
            codeImages.Remove(module);
            informationSnapshots.Remove(module);

            return Term.A(ErlangBooleanAtoms.True);
        }
    }

    public NativeRecordDefinition? NativeRecordDefinition(string module, string name)
    {
        lock (gate)
        {
            return codeDescriptors.TryGetValue(module, out var descriptor) ? descriptor.NativeRecords.FirstOrDefault(definition => definition.Name == name) : null;
        }
    }

    public void PublishModuleCode(string module, ModuleCodeDescriptor descriptor)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(descriptor);
        ObserveDescriptor(descriptor);
        lock (gate)
        {
            ThrowIfSourceClosing();
            PublishModule(module);
            codeDescriptors[module] = descriptor;
            informationSnapshots.Remove(module);
        }
    }

    public Term ModuleInformation(string module, string? key = null)
    {
        lock (gate)
        {
            Term installedNifs = codeImages.TryGetValue(module, out var currentImage)
                && NifImageBindingStore.TryGetValue(currentImage, out var bindings)
                ? bindings!.InstalledFunctions : Nil.Value;
            if (informationSnapshots.TryGetValue(module, out var cached)
                && !ReferenceEquals(cached.Field(ModuleInformationKeys.Nifs), installedNifs))
                informationSnapshots.Remove(module);
            if (!informationSnapshots.TryGetValue(module, out var snapshot))
            {
                if (!loadedModules.ContainsKey(module))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var entries = functions.Where(entry => entry.Key.Module == module).Select(entry => (Signature: (entry.Key.Function, entry.Key.Arity), Body: entry.Value)).ToArray();
                codeDescriptors.TryGetValue(module, out var descriptor);
                if (descriptor is null && entries.Length == 0)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                var exports = entries.Select(entry => entry.Signature).ToArray();
                var signatures = descriptor is null ? exports : descriptor.Functions.Concat(exports);
                var additional = descriptor is null ? entries : entries.Where(entry => !descriptor.Exports.Contains(entry.Signature)).ToArray();
                BitString fingerprint = descriptor is not null && additional.Length == 0 ? descriptor.Fingerprint : ModuleCodeFingerprints.Registered(module, additional, descriptor?.Fingerprint);
                Term compilation = descriptor?.Compilation ?? ModuleCodeFingerprints.Compilation(typeof(ModuleRegistry));
                snapshot = new ModuleInformationSnapshot(
                    module,
                    exports,
                    signatures,
                    fingerprint,
                    compilation,
                    descriptor?.Attributes,
                    descriptor?.AttributePolicy ?? ModuleAttributePolicy.AddDefaultVersion,
                    installedNifs
                );
                informationSnapshots.Add(module, snapshot);
            }

            return key is null ? snapshot.All : snapshot.Field(key);
        }
    }

    public IReadOnlyCollection<string> LoadedModules
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(loadedModules.Keys.ToArray());
        }
    }

    public void InstallPreloaded(Action<ModuleRegistry> configure)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(configure);
        Apply(target =>
        {
            var previous = loadedModules.Keys.ToHashSet(StringComparer.Ordinal);
            configure(target);
            foreach (var module in loadedModules)
            {
                if (!previous.Contains(module.Key))
                    preloadedModules.TryAdd(module.Key, module.Value);
            }
        });
    }

    public IReadOnlyCollection<string> PreloadedModules
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(preloadedModules.Keys.ToArray());
        }
    }

    internal Term PreloadedModuleList()
    {
        lock (gate)
        {
            Term result = Nil.Value;
            foreach (Atom module in preloadedModules.Values)
                result = new Cons(module, result);

            return result;
        }
    }

    internal Term LoadedModuleList()
    {
        lock (gate)
        {
            Term result = Nil.Value;
            foreach (string module in loadedModules.Keys.Concat(oldCode.Keys).Distinct(StringComparer.Ordinal))
                result = new Cons(Term.A(module), result);

            return result;
        }
    }

    public void Apply(Action<ModuleRegistry> configure)
    {
        var modification = CodeModification.Enter();
        ModuleRegistryApplyFrame? transaction = null;
        try
        {
            ArgumentNullException.ThrowIfNull(configure);
            transaction = new ModuleRegistryApplyFrame(this, modification.ReleasesPermission);
            lock (gate)
            {
                var previous = new Dictionary<(string Module, string Function, int Arity), ErlangFunction>(functions);
                var previousIntrinsics = new Dictionary<(string Module, string Function, int Arity), IntrinsicBinding>(intrinsicBindings);
                var previousModules = new Dictionary<string, Atom>(loadedModules, StringComparer.Ordinal);
                var previousPreloaded = new Dictionary<string, Atom>(preloadedModules, StringComparer.Ordinal);
                var previousDescriptors = new Dictionary<string, ModuleCodeDescriptor>(codeDescriptors, StringComparer.Ordinal);
                var previousInformation = new Dictionary<string, ModuleInformationSnapshot>(informationSnapshots, StringComparer.Ordinal);
                var previousKnown = knownModules.ToArray();
                var previousOld = new Dictionary<string, ModuleCodeSnapshot>(oldCode, StringComparer.Ordinal);
                var previousOwners = new Dictionary<(string Module, string Function, int Arity), object>(sourceOwners);
                var previousImages = new Dictionary<string, ModuleCodeImage>(codeImages, StringComparer.Ordinal);
                try
                {
                    configure(this);
                }
                catch
                {
                    intrinsicBindings.Clear();
                    foreach (var entry in previousIntrinsics)
                        intrinsicBindings.Add(entry.Key, entry.Value);
                    functions.Clear();
                    foreach (var entry in previous)
                        functions.Add(entry.Key, entry.Value);
                    loadedModules.Clear();
                    foreach (var module in previousModules)
                        loadedModules.Add(module.Key, module.Value);
                    preloadedModules.Clear();
                    foreach (var module in previousPreloaded)
                        preloadedModules.Add(module.Key, module.Value);
                    codeDescriptors.Clear();
                    foreach (var entry in previousDescriptors)
                        codeDescriptors.Add(entry.Key, entry.Value);
                    informationSnapshots.Clear();
                    foreach (var entry in previousInformation)
                        informationSnapshots.Add(entry.Key, entry.Value);
                    knownModules.Clear();
                    knownModules.UnionWith(previousKnown);
                    oldCode.Clear();
                    foreach (var entry in previousOld)
                        oldCode.Add(entry.Key, entry.Value);
                    sourceOwners.Clear();
                    foreach (var entry in previousOwners)
                        sourceOwners.Add(entry.Key, entry.Value);
                    codeImages.Clear();
                    foreach (var entry in previousImages)
                        codeImages.Add(entry.Key, entry.Value);
                    throw;
                }
            }
        }
        finally
        {
            transaction?.Exit();
            modification.Dispose();
            transaction?.ReleaseAfterWriter();
        }
    }

    public IReadOnlyCollection<(string Module, string Function, int Arity)> Exports
    {
        get
        {
            lock (gate)
                return Array.AsReadOnly(functions.Keys.ToArray());
        }
    }
}
