using Erlang.Compiler;

namespace Erlang.Tests;

public static class NativeRecordDefinitionHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var owner = new Parser("-module(native_definition_owner). -export([make/0]). -record #r{x=1}. -export_record([r]). make()->#r{}.").ParseModule();
        var consumer = new Parser("-module(native_definition_consumer). -export([make/0,get/1]). -import_record(native_definition_owner,[r]). make()->#r{}. get(R)->R#r.x.").ParseModule();
        var replacement = new Parser("-module(native_definition_owner). -export([make/0]). -record #r{x=99,y=2}. make()->#r{}.").ParseModule();
        owner.Register(runtime.Modules);
        consumer.Register(runtime.Modules);
        Require(owner.NativeRecords is IList<NativeRecordDefinition> { IsReadOnly: true });
        Require(owner.NativeRecords[0].Fields is IList<KeyValuePair<Atom, Term?>> { IsReadOnly: true });
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                var captured = (NativeRecordTerm)await runtime.Modules.Call(context, NativeRecordDefinitionHostNames.Consumer, NativeRecordDefinitionHostNames.Make);
                Require(captured.IsExported && captured.Values[0].Equals(Term.I(1)));
                runtime.Modules.Apply(registry =>
                {
                    registry.DeleteModule(NativeRecordDefinitionHostNames.Owner);
                    replacement.Register(registry);
                });
                var current = (NativeRecordTerm)await runtime.Modules.Call(context, NativeRecordDefinitionHostNames.Owner, NativeRecordDefinitionHostNames.Make);
                Require(!current.IsExported && current.Values[0].Equals(Term.I(99)) && current.FieldNames.Count == 2);
                Require((await runtime.Modules.Call(
                    context,
                    NativeRecordDefinitionHostNames.Consumer,
                    NativeRecordDefinitionHostNames.Get,
                    captured
                )).Equals(Term.I(1)));
                await Reject(async () => await runtime.Modules.Call(
                    context,
                    NativeRecordDefinitionHostNames.Consumer,
                    NativeRecordDefinitionHostNames.Get,
                    current
                ));
                await Reject(async () => await runtime.Modules.Call(context, NativeRecordDefinitionHostNames.Consumer, NativeRecordDefinitionHostNames.Make));
                Term definition = await runtime.Modules.Call(
                    context,
                    NativeRecordDefinitionHostNames.RecordsModule,
                    NativeRecordDefinitionHostNames.GetDefinition,
                    Term.A(NativeRecordDefinitionHostNames.Owner),
                    Term.A(NativeRecordDefinitionHostNames.Record)
                );
                Require(definition.Equals(NativeRecordLanguage.Definition(replacement.NativeRecords[0])));
                await runtime.Modules.Call(
                    context,
                    NativeRecordDefinitionHostNames.RuntimeModule,
                    NativeRecordDefinitionHostNames.Purge,
                    Term.A(NativeRecordDefinitionHostNames.Owner)
                );
                try
                {
                    runtime.Modules.Apply(registry =>
                    {
                        registry.DeleteModule(NativeRecordDefinitionHostNames.Owner);
                        owner.Register(registry);
                        throw new InvalidOperationException(NativeRecordDefinitionHostNames.Broken);
                    });
                }
                catch (InvalidOperationException)
                {
                }
                Require(runtime.Modules.NativeRecordDefinition(NativeRecordDefinitionHostNames.Owner, NativeRecordDefinitionHostNames.Record) == replacement.NativeRecords[0]);
                runtime.Modules.DeleteModule(NativeRecordDefinitionHostNames.Owner);
                Require(runtime.Modules.NativeRecordDefinition(NativeRecordDefinitionHostNames.Owner, NativeRecordDefinitionHostNames.Record) is null);
                Require((await runtime.Modules.Call(
                    context,
                    NativeRecordDefinitionHostNames.Consumer,
                    NativeRecordDefinitionHostNames.Get,
                    captured
                )).Equals(Term.I(1)));

                return Nil.Value;
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await process.Completion.WaitAsync(NativeRecordDefinitionHostParameters.CompletionTimeout);
        if (failure is not null)
            throw failure;
    }

    private static async Task Reject(Func<Task<Term>> action)
    {
        try
        {
            await action();
        }
        catch (ErlangException exception) when (exception.Reason is TupleTerm { Items: [Atom { Name: NativeRecordDefinitionHostNames.BadRecord }, _] })
        {
            return;
        }

        throw new InvalidOperationException(NativeRecordDefinitionHostNames.Broken);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NativeRecordDefinitionHostNames.Broken);
    }
}
