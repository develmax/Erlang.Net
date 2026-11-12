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
-module(beam_constant_record_argument_probe).
-export([create_menus/1]).

-record(create_menu,
	{id,
	 text,
	 help = [],
	 type = append,
	 check = false
	}).

create_menus(MenuBar) ->
    Menus = [{"File", [#create_menu{id=5001, text="Close"}]}],
    observer_lib:create_menus(Menus, MenuBar, new_window).
