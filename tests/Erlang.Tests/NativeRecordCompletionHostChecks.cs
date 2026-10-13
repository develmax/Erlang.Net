using Erlang.Compiler;

namespace Erlang.Tests;

public static class NativeRecordCompletionHostChecks
{
    public static Task Run()
    {
        const string source = "-module(native_type_host).\n% supplementary \U0001F642\n-record #r{x=not_integer::integer(),y=[]::[atom()]} .\n-export([run/0]). run()->#r{}.";
        var definition = new Parser(source).ParseModule().NativeRecords.Single();
        Require(definition.FieldTypes.Count == 2);
        Require(definition.FieldTypes is IList<NativeRecordFieldType> { IsReadOnly: true });
        Require(definition.FieldTypes[0].SourceOffset == source.IndexOf("x=", StringComparison.Ordinal));
        var syntax = (TupleTerm)definition.FieldTypes[0].Syntax;
        Require(syntax.Items[0].Equals(Term.A("type")));
        Require(syntax.Items[1].Equals(Term.Tuple(Term.I(3), Term.I(27))));
        Require(definition.Fields[0].Value!.Equals(Term.A("not_integer")));
        var inputs = definition.FieldTypes.ToArray();
        var snapshot = new NativeRecordDefinition(
            definition.Name,
            false,
            definition.Fields,
            inputs
        );
        inputs[0] = new(Term.A("x"), Term.A("changed"), 0);
        Require(snapshot.FieldTypes[0].Equals(definition.FieldTypes[0]));
        Require(NativeRecordLanguage.Definition(definition).Equals(Term.Tuple(
            new MapTerm([new KeyValuePair<Term, Term>(Term.A("is_exported"), Term.A("false"))]),
            Term.List(Term.Tuple(Term.A("x"), Term.A("not_integer")), Term.Tuple(Term.A("y"), Nil.Value))
        )));
        const string invalid = "-module(native_offset_host). -record #r{x=1}. -export([run/0]). run()->#r{bad=2}.";
        try
        {
            CodeGeneration.CompileModule(invalid, "offset.erl");
            throw new InvalidOperationException("Expected unknown native field diagnostic");
        }
        catch (CompileException exception)
        {
            Require(exception.Offset == invalid.IndexOf("bad=", StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("Native record type metadata/source annotation contract failed");
    }
}
