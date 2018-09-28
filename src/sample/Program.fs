open System
open Pengines
open Pengines.Prolog
open Pengines.Pengine

type MachineState = 
    | PoweredOn
    | PoweredOff
    | OsRunning
    | OsNotRunning
    | OsHung
    | AppRunning
    | AppNotRunning
    | AppHung
with
    member m.Prolog =
        match m with
        | PoweredOn ->
            CompoundTerm("machine_state", 
                [ Atom("on"); Atom("not_running"); Atom("not_running") ]) 
        | PoweredOff ->
            CompoundTerm("machine_state",
                [ Atom("off"); Atom("not_running"); Atom("not_running") ]) 
        | OsRunning ->
            CompoundTerm("machine_state",
                [ Atom("on"); Atom("running"); Atom("not_running") ]) 
        | OsNotRunning ->
            CompoundTerm("machine_state",
                [ Atom("on"); Atom("not_running"); Atom("not_running") ]) 
        | OsHung ->
            CompoundTerm("machine_state",
                [ Atom("on"); Atom("hung"); Atom("_") ])
        | AppRunning ->
            CompoundTerm("machine_state",
                [ Atom("on"); Atom("running"); Atom("running") ]) 
        | AppNotRunning ->
            CompoundTerm("machine_state",
                [ Atom("on"); Atom("running"); Atom("not_running") ]) 
        | AppHung ->
            CompoundTerm("machine_state",
                [ Atom("on"); Atom("running"); Atom("hung") ]) 
        
type SystemAction = 
    | PowerOn
    | PowerOff
    | Restart
    | InitOS
    | ShutdownOS
    | StartApplication
    | ShutdownApplication
    | KillApplication
with
    static member Parse = function
        | "power_on" -> PowerOn |> Ok
        | "power_off" -> PowerOff |> Ok
        | "restart" -> Restart |> Ok
        | "init_os" -> InitOS |> Ok
        | "shutdown_os" -> ShutdownOS |> Ok
        | "start_application" -> StartApplication |> Ok
        | "shutdown_application" -> ShutdownApplication |> Ok
        | "kill_application" -> KillApplication |> Ok
        | _ -> Error "Unsupported system action in plan."

type BuildStateChanges = MachineState -> MachineState -> Async<SystemAction list>
let recoveryPlanning = System.IO.File.ReadAllText("pl/system_management.pl")
let http = new System.Net.Http.HttpClient ()

// Remote: "http://pengines.swi-prolog.org"
let penginesUri = Uri  "http://localhost:3030"

// If there is an answer, it contains a list of solutions.
let (|SolutionsList|_|) (answerOpt:Answer option) =
    match answerOpt with
    | Some answer ->
        match answer.Data with
        | ListTerm solutions -> Some solutions
        | _ -> None
    | _ -> None

let buildRecoveryPlan (initial:MachineState) (final:MachineState) =
    async {
        let ask = Operators.Fact "build_plan" [initial.Prolog; final.Prolog; Variable("Plan")]
        let! results = 
            {
                SrcText = recoveryPlanning
                Ask = (ask |> Serialization.termToProlog) |> Some
                Format ="json"
                Chunk = 5 |> Some // Number of results at a time.
            }
            |> Pengine.createPengine penginesUri http
        match results with
        | Ok createResponse ->
            match createResponse.Answer with
            | SolutionsList solutions ->
                return solutions |> List.map (fun item ->
                    match item with
                    | DictTerm d when d.ContainsKey "Plan" -> 
                        d.["Plan"] |> function
                        | ListTerm plan -> plan |> Ok
                        | _ -> "No plan returned" |> Error
                    | _ -> "Expected solution to contain a dictionary of terms." |> Error
                ) |> Ok
            | _ -> return Error "No answer returned."
        | Error msg -> return Error (sprintf "Error finding recovery plans: %s" msg)
    }

[<EntryPoint>]
let main argv =
    let plan = buildRecoveryPlan AppHung AppRunning |> Async.RunSynchronously
    printfn "Plan: %A" plan
    0
