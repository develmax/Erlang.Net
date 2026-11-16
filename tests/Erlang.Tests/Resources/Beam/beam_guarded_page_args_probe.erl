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
-module(beam_guarded_page_args_probe).
-export([init/1]).

init([ParentWin, Callback]) when is_atom(Callback) ->
    init(ParentWin, Callback);
init([ParentWin, {App, Fun}]) when is_function(Fun) ->
    init([ParentWin, {App, Fun()}]);
init([ParentWin, {expand,HtmlText,Tab}]) ->
    init(ParentWin, HtmlText, Tab, cdv);
init([ParentWin, {App, {expand,HtmlText,Tab}}]) ->
    init(ParentWin, HtmlText, Tab, App);
init([ParentWin, {App,HtmlText}]) ->
    init(ParentWin, HtmlText, undefined, App);
init([ParentWin, HtmlText]) ->
    init(ParentWin, HtmlText, undefined, cdv).

init(Parent, Callback) -> {Parent, Callback}.

init(Parent, Text, Table, App) -> {Parent, Text, Table, App}.
