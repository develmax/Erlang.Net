-module(beam_choice_search_probe).
-export([find_choice/2, accepts/2]).
find_choice([], _) -> false;
find_choice([{Name, _, Shape} | Fields], Tag) ->
    case accepts(Shape, Tag) of
        true -> {Name, Shape};
        false -> find_choice(Fields, Tag)
    end.

accepts({ref, Name}, Tag) -> accepts(schema(Name), Tag);
accepts({choice, _, Fields}, Tag) -> find_choice(Fields, Tag) =/= false;
accepts(Shape, Tag) -> wire_tag(Shape) =:= Tag.

schema(Type) -> Type.
wire_tag(Shape) -> Shape.
