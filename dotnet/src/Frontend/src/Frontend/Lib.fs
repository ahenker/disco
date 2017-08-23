module rec Iris.Web.Lib

// Helper methods to be used from JS

open System
open System.Collections.Generic
open Iris.Raft
open Iris.Core
open Iris.Web.Core
open Iris.Core.Commands
open Fable.Core
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Fable.Core.JsInterop
open Fable.Import

let notify(msg: string) =
  Browser.console.log(msg)

  match !!Browser.window?Notification with
  // Check if the browser supports notifications
  | null -> Browser.console.log msg

  // Check whether notification permissions have already been granted
  | notify when !!notify?permission = "granted" ->
    !!createNew notify msg

  // Ask the user for permission
  | notify when !!notify?permission <> "denied" ->
    !!notify?requestPermission(function
        | "granted" -> !!createNew notify msg
        | _ -> ())
  | _ -> ()

let removeMember(config: IrisConfig, memId: Id) =
  match Config.findMember config memId with
  | Right mem ->
    RemoveMember mem
    |> ClientContext.Singleton.Post
  | Left error ->
    printfn "%O" error

let alert msg (_: Exception) =
  Browser.window.alert("ERROR: " + msg)

/// Works like function composition but the second operand
/// is a value, and the result of the first function is ignored
let (&>) fst v =
  fun x -> fst x; v

let private postCommandPrivate (ipAndPort: string option) (cmd: Command) =
  let url =
    match ipAndPort with
    | Some ipAndPort -> sprintf "http://%s%s" ipAndPort Constants.WEP_API_COMMAND
    | None -> Constants.WEP_API_COMMAND
  GlobalFetch.fetch(
    RequestInfo.Url url,
    requestProps
      [ RequestProperties.Method HttpMethod.POST
        requestHeaders [ContentType "application/json"]
        RequestProperties.Body (toJson cmd |> U3.Case3) ])

let postCommand onSuccess onFail (cmd: Command) =
  postCommandPrivate None cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.map onFail
    else res.text() |> Promise.map onSuccess)

let postCommandAndBind onSuccess onFail (cmd: Command) =
  postCommandPrivate None cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.bind onFail
    else res.text() |> Promise.bind onSuccess)

/// Posts a command, parses the JSON response returns a promise (can fail)
[<PassGenerics>]
let postCommandParseAndContinue<'T> (ipAndPort: string option) (cmd: Command) =
  postCommandPrivate ipAndPort cmd
  |> Promise.bind (fun res ->
    if res.Ok
    then res.text() |> Promise.map ofJson<'T>
    else res.text() |> Promise.map (failwithf "%s"))

let postCommandWithErrorNotifier defValue onSuccess cmd =
  postCommand onSuccess (fun msg -> notify msg; defValue) cmd

let postCommandAndForget cmd =
  postCommand ignore notify cmd

let listProjects() =
  ListProjects
  |> postCommandWithErrorNotifier [||] (ofJson<NameAndId[]> >> Array.map (fun x -> x.Name))

let addMember(memberIpAddr: string, memberHttpPort: uint16) =
  Promise.start (promise {
  // See workflow: https://bitbucket.org/nsynk/iris/wiki/md/workflows.md
  try
    let latestState =
      match ClientContext.Singleton.Store with
      | Some store -> store.State
      | None -> failwith "The client store is not initialized"

    let memberIpAndPort =
      sprintf "%s:%i" memberIpAddr memberHttpPort |> Some

    memberIpAndPort |> Option.iter (printfn "New member URI: %s")

    let! status =
      postCommandParseAndContinue<MachineStatus>
        memberIpAndPort
        MachineStatus

    match status with
    | Busy (_, name) -> failwithf "Host cannot be added. Busy with project %A" name
    | _ -> ()

    // Get the added machines configuration
    let! machine =
      postCommandParseAndContinue<IrisMachine>
        memberIpAndPort
        MachineConfig

    // List projects of member candidate (B)
    let! projects =
      postCommandParseAndContinue<NameAndId[]>
        memberIpAndPort
        ListProjects

    // If B has leader (A) active project,
    // then **pull** project from A into B
    // else **clone** active project from A into B

    let! commandMsg =
      let projectGitUri =
        match Project.localRemote latestState.Project with
        | Some uri -> uri
        | None -> failwith "Cannot get URI of project git repository"
      match projects |> Array.tryFind (fun p -> p.Id = latestState.Project.Id) with
      | Some p -> PullProject(p.Id, latestState.Project.Name, projectGitUri)
      | None   -> CloneProject(latestState.Project.Name, projectGitUri)
      |> postCommandParseAndContinue<string> memberIpAndPort

    notify commandMsg

    let active = latestState.Project.Config.ActiveSite

    // Load active project in machine B
    // Note that we don't use loadProject from below, since that function
    // restarts the ClientContextn and thus disconnects us from the service.

    // TODO: Using the admin user for now, should it be the same user as leader A?
    let! loadResult =
      LoadProject(latestState.Project.Name, name "admin", password "Nsynk", active)
      |> postCommandPrivate memberIpAndPort

    printfn "response: %A" loadResult

    // Add member B to the leader (A) cluster
    { Member.create machine.MachineId with
        HostName = machine.HostName
        IpAddr   = machine.BindAddress
        Port     = machine.RaftPort
        WsPort   = machine.WsPort
        GitPort  = machine.GitPort
        ApiPort  = machine.ApiPort }
    |> AddMember
    |> ClientContext.Singleton.Post // TODO: Check the state machine post has been successful
  with
  | exn ->
    sprintf "Cannot add new member: %s" exn.Message |> notify
})

let shutdown() =
  Shutdown |> postCommand (fun _ -> notify "The service has been shut down") notify

let saveProject() =
  SaveProject |> postCommand (fun _ -> notify "The project has been saved") notify

let unloadProject() =
  UnloadProject |> postCommand (fun _ ->
    notify "The project has been unloaded"
    Browser.location.reload()) notify

let setLogLevel(lv) =
  LogLevel.Parse(lv) |> SetLogLevel |> ClientContext.Singleton.Post

let nullify _: 'a = null

let rec loadProject(project: Name, username: UserName, pass: Password, site: Id option, ipAndPort: string option): JS.Promise<string option> =
  LoadProject(project, username, pass, site)
  |> postCommandPrivate ipAndPort
  |> Promise.bind (fun res ->
    if res.Ok
    then
      ClientContext.Singleton.ConnectWithWebSocket()
      |> Promise.map (fun _msg -> // TODO: Check message?
        notify "The project has been loaded successfully"
        Browser.location.reload()
        None)
    else
      res.text() |> Promise.map (fun msg ->
        if msg.Contains(ErrorMessages.PROJECT_NO_ACTIVE_CONFIG)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_CLUSTER)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_MEMBER)
        then Some msg
        // We cannot deal with the error, just notify it
        else notify msg; None
      )
  )

let getProjectSites(project, username, password) =
  GetProjectSites(project, username, password)
  |> postCommand ofJson<string[]> (fun msg -> notify msg; [||])

let createProject(name: string) =
  Promise.start (promise {
    let! (machine: IrisMachine) = postCommandParseAndContinue None MachineConfig
    do! { name     = name
          ipAddr   = string machine.BindAddress
          port     = unwrap machine.RaftPort
          apiPort  = unwrap machine.ApiPort
          wsPort   = unwrap machine.WsPort
          gitPort  = unwrap machine.GitPort }
        |> CreateProject
        |> postCommand (fun _ -> notify "The project has been created successfully") notify
  })