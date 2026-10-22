using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOpcodeEdgeHostChecks
{
    internal static async Task GetListAliases()
    {
        Term head = Term.Tuple(Term.I(1), Term.I(2));
        Term tail = Term.I(9);
        Term input = new Cons(head, tail);
        // Source aliases head, then source aliases tail; both results must use the original cell.
        foreach (bool headAliasesSource in new[] { true, false })
        {
            byte hd = headAliasesSource ? X(0) : X(1);
            byte tl = headAliasesSource ? X(1) : X(0);
            byte[] code = [.. Start(
                1,
                2,
                2,
                1
            ),
                56, F(1), X(0), 65, X(0), hd, tl,
                164, X(0), 0x17, U(2), hd, tl, 19];
            Require((await Invoke(Image(code), input)).Equals(Term.Tuple(head, tail)));
        }
    }

    internal static async Task TrimZeroTailContinuation()
    {
        // run -> normal call middle -> trim all Y slots -> tail call leaf -> return to run.
        // Wrapping leaf's result in run proves the outer continuation was not discarded.
        byte[] code = [.. Start(
            1,
            2,
            2,
            0
        ),
            12, U(0), U(0), 4, U(0), F(4),
            164, X(0), 0x17, U(2), X(0), A(7), 18, U(0), 19,
            .. Start(
                3,
                4,
                3,
                0
            ),
            12, U(2), U(0), 64, A(7), Y(0), 64, A(8), Y(1),
            136, U(2), U(0), 5, U(0), F(6), U(0),
            .. Start(
                5,
                6,
                4,
                0
            ), 64, A(8), X(0), 19];
        Term expected = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Other), Term.A(BeamOpcodeEdgeNames.Marker));
        Require((await Invoke(Image(
            code,
            labels: 7,
            functions: 3,
            arity: 0
        ))).Equals(expected));
    }

    internal static async Task GuardBifDestination()
    {
        // The failure edge observes the pre-existing Y0. The success edge observes the result.
        byte[] code = [.. Start(
            1,
            2,
            2,
            1
        ),
            12, U(1), U(1), 64, A(7), Y(0),
            124, F(3), U(1), U(0), X(0), Y(0),
            64, Y(0), X(0), 18, U(1), 19,
            1, U(3), 64, Y(0), X(0), 18, U(1), 19];
        byte[] bytes = Image(code);
        Require((await Invoke(bytes, new Cons(Term.I(1), Term.I(2)))).Equals(Term.A(BeamOpcodeEdgeNames.Marker)));
        Require((await Invoke(bytes, Term.List(Term.I(1), Term.I(2)))).Equals(Term.I(2)));
    }

    internal static Task SelectTableAdmission()
    {
        // OTP beam_validator assert_choices requires one literal class: atom/integer/float.
        // Nil is not an allowed select_val choice; atom+integer is not homogeneous.
        foreach (byte[] choices in new[]
        {
            new byte[] { A(0), F(4) },
            new byte[] { A(7), F(4), I(1), F(4) }
        })
        {
            byte[] code = [.. Start(
                1,
                2,
                2,
                1
            ),
                59, X(0), F(3), 0x17, U(choices.Length), .. choices,
                1, U(3), 64, A(7), X(0), 19,
                1, U(4), 64, A(8), X(0), 19];
            bool rejected = false;
            try
            {
                BeamModule module = BeamModule.Prepare(Image(code));
                rejected = !module.Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            Require(rejected);
        }

        return Task.CompletedTask;
    }

    private static async Task<Term> Invoke(byte[] image, params Term[] arguments)
    {
        BeamModule module = BeamModule.Prepare(image);
        Require(module.Supports(BeamOpcodeEdgeNames.Run, arguments.Length));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        return await actor.InvokeAsync(async context => await module.Invoke(context, BeamOpcodeEdgeNames.Run, arguments));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamOpcodeEdgeNames.Failure);
    }
}
