using Erlang;

namespace Erlang.Tests;

internal static partial class EtsCompressed441Checks
{
    internal static async Task FunctionsAndCensus()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        PreparedBeamImage prepared = BeamStoredFunction406Access.Publish(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        foreach (bool compressed in new[] { false, true })
        {
            Term options = Term.List(
                Term.A(EtsCompressed441Names.Kinds[0]),
                Term.Tuple(Term.A(EtsCompressed441Names.KeyPosition), Term.I(EtsCompressed441Names.KeyField))
            );
            if (compressed) options = new Cons(Term.A(EtsCompressed441Names.Compressed), options);
            Term table = await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.New,
                Term.A(EtsCompressed441Names.Name),
                options
            ));
            await actor.InvokeAsync(async context =>
            {
                var value = (FunctionTerm)await runtime.Modules.Call(
                    context,
                    prepared.Name,
                    BeamClosureHostNames.Make,
                    Term.I(BeamClosureHostNames.Capture)
                );
                var key = (FunctionTerm)await runtime.Modules.Call(
                    context,
                    prepared.Name,
                    BeamClosureHostNames.Make,
                    Term.I(BeamClosureHostNames.OtherCapture)
                );
                Require(value.Environment.Count != 0 && key.Environment.Count != 0);
                await Call(
                    runtime,
                    context,
                    EtsCompressed441Names.Insert,
                    table,
                    Term.Tuple(value, key)
                );
                var rows = (Cons)await Call(
                    runtime,
                    context,
                    EtsCompressed441Names.Lookup,
                    table,
                    key
                );
                var row = (TupleTerm)rows.Head;
                var valueCopy = (FunctionTerm)row.Items[EtsCompressed441Names.ValueField - 1];
                var keyCopy = (FunctionTerm)row.Items[EtsCompressed441Names.KeyField - 1];
                Require(!ReferenceEquals(value, valueCopy) && !ReferenceEquals(key, keyCopy));
                Require(value.Equals(valueCopy) && key.Equals(keyCopy));
                Require((await valueCopy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
                Require((await keyCopy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.OtherResult)));

                return Nil.Value;
            });
            await actor.Invoke(context =>
            {
                object payload = Field(Property(Slot(storage, table), EtsCompressed441Names.PayloadProperty), EtsCompressed441Names.PayloadField)!;
                object? encoded = Field(payload, EtsCompressed441Names.CompressedField);
                Require((encoded is not null) == compressed);
                if (compressed) Require(Field(payload, EtsCompressed441Names.NodesField) is null);

                return Census(
                    runtime,
                    storage,
                    table,
                    encoded,
                    expectedBeamRecipes: 2
                );
            });
            await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Delete,
                table
            ));
            EtsFoundation405Access.Empty(storage);
        }
    }
}
