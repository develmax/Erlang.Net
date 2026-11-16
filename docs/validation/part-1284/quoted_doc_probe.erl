-module(quoted_doc_probe).
-moduledoc "Quoted spec names.".
-export(['begin'/1, 'end'/0, plain/1, 'has space'/1]).
-spec 'begin'(Mode :: atom()) -> atom().
'begin'(Mode) -> Mode.
-spec 'end'() -> ok.
'end'() -> ok.
-spec plain(Value :: atom()) -> atom().
plain(Value) -> Value.
-spec 'has space'(Value :: atom()) -> atom().
'has space'(Value) -> Value.
