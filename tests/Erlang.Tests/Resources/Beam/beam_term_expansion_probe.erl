%%
%% %CopyrightBegin%
%%
%% SPDX-License-Identifier: Apache-2.0
%%
%% Copyright Ericsson AB 2013-2025. All Rights Reserved.
%%
%% Licensed under the Apache License, Version 2.0 (the "License");
%% you may not use this file except in compliance with the License.
%% You may obtain a copy of the License at
%%
%%     http://www.apache.org/licenses/LICENSE-2.0
%%
%% Unless required by applicable law or agreed to in writing, software
%% distributed under the License is distributed on an "AS IS" BASIS,
%% WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
%% See the License for the specific language governing permissions and
%% limitations under the License.
%%
%% %CopyrightEnd%

-module(beam_term_expansion_probe).
-export([expand/2]).

expand(['#CDVBin',Offset,Size,Pos], true) ->
    {ok,Bin} = crashdump_viewer:expand_binary({Offset,Size,Pos}),
    Bin;
expand(Bin, Tab) when is_binary(Bin), not is_boolean(Tab) ->
    observer_lib:make_obsbin(Bin, Tab);
expand([H|T], Expand) ->
    case expand(T, Expand) of
	ET when is_list(ET) ->
	    [expand(H, Expand)|ET];
        ET -> % The tail is an expanded binary - cannot append with |
	    [expand(H, Expand),ET]
    end;
expand(Tuple, Expand) when is_tuple(Tuple) ->
    list_to_tuple(expand(tuple_to_list(Tuple), Expand));
expand(Term, _) ->
    Term.
