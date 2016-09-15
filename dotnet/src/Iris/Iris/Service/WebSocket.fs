namespace Iris.Service

[<AutoOpen>]
module WebSocket =

  open System
  open System.Threading
  open Iris.Core
  open Iris.Service.Raft.Server
  open Fleck
  open Newtonsoft.Json

  type WsServer(config: Config, context: RaftServer) =

    let mutable onOpenCb    : Option<Session -> unit> = None
    let mutable onCloseCb   : Option<Id -> unit> = None
    let mutable onErrorCb   : Option<Id -> unit> = None
    let mutable onMessageCb : Option<Id -> StateMachine -> unit> = None

    let uri =
      sprintf "ws://%s:%d"
        config.RaftConfig.BindAddress
        config.PortConfig.WebSocket

    let server = new WebSocketServer(uri)

    let mutable sessions : Map<Id,IWebSocketConnection> = Map.empty

    let getSessionId (socket: IWebSocketConnection) : Id =
      string socket.ConnectionInfo.Id |> Id

    let buildSession (socket: IWebSocketConnection) : Session =
      let ua =
        if socket.ConnectionInfo.Headers.ContainsKey("User-Agent") then
          socket.ConnectionInfo.Headers.["User-Agent"]
        else
          "<no user agent specified>"

      { Id        = getSessionId socket
      ; UserName  = ""
      ; IpAddress = IpAddress.Parse socket.ConnectionInfo.ClientIpAddress
      ; UserAgent = ua }

    //   ___
    //  / _ \ _ __   ___ _ __
    // | | | | '_ \ / _ \ '_ \
    // | |_| | |_) |  __/ | | |
    //  \___/| .__/ \___|_| |_|
    //       |_|

    let onOpen (socket: IWebSocketConnection) _ =
      let session : Session = buildSession socket
      let sid = getSessionId socket
      sessions <- Map.add sid socket sessions
      Option.map (fun cb -> cb session) onOpenCb |> ignore
      printfn "[%s] onOpen!" (getSessionId socket |> string)

    //   ____ _
    //  / ___| | ___  ___  ___
    // | |   | |/ _ \/ __|/ _ \
    // | |___| | (_) \__ \  __/
    //  \____|_|\___/|___/\___|

    let onClose (socket: IWebSocketConnection) _ =
      let session = getSessionId socket
      sessions <- Map.remove session sessions
      Option.map (fun cb -> cb session) onCloseCb |> ignore
      printfn "[%s] onClose!" (string session)

    //  __  __
    // |  \/  | ___  ___ ___  __ _  __ _  ___
    // | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
    // | |  | |  __/\__ \__ \ (_| | (_| |  __/
    // |_|  |_|\___||___/___/\__,_|\__, |\___|
    //                             |___/

    let onMessage (socket: IWebSocketConnection) (msg: string) =
      let session = getSessionId socket
      let entry : StateMachine option = Json.decode msg

      match entry with
      | Some command -> Option.map (fun cb -> cb session command) onMessageCb |> ignore
      | _            -> ()

      printfn "[%s] onMessage: %s" (string session) msg

    //  _____
    // | ____|_ __ _ __ ___  _ __
    // |  _| | '__| '__/ _ \| '__|
    // | |___| |  | | | (_) | |
    // |_____|_|  |_|  \___/|_|

    let onError (socket: IWebSocketConnection) (exn: 'a when 'a :> Exception) =
      let session = getSessionId socket
      sessions <- Map.remove session sessions
      Option.map (fun cb -> cb session) onErrorCb |> ignore
      printfn "[%s] onError: %s" (string session) exn.Message

    let handler (socket: IWebSocketConnection) =
      socket.OnOpen    <- new System.Action(onOpen socket)
      socket.OnClose   <- new System.Action(onClose socket)
      socket.OnMessage <- new System.Action<string>(onMessage socket)
      socket.OnError   <- new System.Action<exn>(onError socket)

    member self.Start() =
      server.Start(new System.Action<IWebSocketConnection>(handler))

    member self.Stop() =
      Map.iter (fun _ (socket: IWebSocketConnection) -> socket.Close()) sessions
      dispose server

    member self.Broadcast(msg: StateMachine) =
      let send _ (socket: IWebSocketConnection) =
        msg |> Binary.encode |> socket.Send |> ignore
      Map.iter send sessions

    member self.Send (sessionid: Id) (msg: StateMachine) =
      match Map.tryFind sessionid sessions with
      | Some socket ->
        msg |> Binary.encode |> socket.Send |> ignore
      | _ -> printfn "could not send message to %A. not found." sessionid

    member self.OnOpen
      with set cb = onOpenCb <- Some cb

    member self.OnClose
      with set cb = onCloseCb <- Some cb

    member self.OnError
      with set cb = onErrorCb <- Some cb

    member self.OnMessage
      with set cb = onMessageCb <- Some cb

    interface IDisposable with
      member self.Dispose() =
        self.Stop()
