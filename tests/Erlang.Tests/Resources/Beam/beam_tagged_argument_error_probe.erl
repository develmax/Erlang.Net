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

-module(beam_tagged_argument_error_probe).
-export([handle_call/3, handle_cast/2, reordered/3]).

handle_call(Call, From, _State) ->
    error({unhandled_call, Call, From}).

handle_cast(Cast, _State) ->
    error({unhandled_cast, Cast}).

reordered(First, _, Third) ->
    erlang:error({reordered, Third, First, Third}).
