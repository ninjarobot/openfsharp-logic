
% Managing a machine.
% state(HardwareState, OperatingSystemState, ApplicationState)
system_action(machine_state(off, not_running, not_running),
    power_on,
    machine_state(on, not_running, not_running)).
system_action(machine_state(on, _, _),
    power_off,
    machine_state(off, not_running, not_running)).
system_action(machine_state(on, not_running, not_running),
    init_os,
    machine_state(on, running, not_running)).
system_action(machine_state(on, hung, _),
    restart,
    machine_state(on, running, not_running)).
system_action(machine_state(on, running, _),
    shutdown_os,
    machine_state(on, not_running, not_running)).
system_action(machine_state(on, running, not_running),
    start_application,
    machine_state(on, running, running)).
system_action(machine_state(on, running, running),
    shutdown_application,
    machine_state(on, running, not_running)).
system_action(machine_state(on, running, hung),
    kill_application,
    machine_state(on, running, not_running)).

% server_management(state(off, _, _), state(_, _, running), Plan).
% or
% ?- append(Plan, _, _), server_management(state(on, hung, not_running), state(_, _, running), Plan).

server_management(State, State, []).
server_management(State1, GoalState, [Action1 | RestOfPlan]) :-
    system_action(State1, Action1, State2), % first action
    server_management(State2, GoalState, RestOfPlan). % build rest of plan

build_plan(Start, Finish, Plan) :-
    append(Plan, _, _),
    server_management(Start, Finish, Plan).

start(Plan) :-
    build_plan(machine_state(on, running, hung), machine_state(_, _, running), Plan).
