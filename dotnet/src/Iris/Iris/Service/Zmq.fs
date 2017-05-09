namespace Iris.Service

// * Imports

open System
open System.Threading
open ZeroMQ
open Iris.Zmq
open Iris.Raft
open Iris.Core
open Iris.Service

// * ZmqUtils

[<AutoOpen>]
module ZmqUtils =

  // ** request

  /// ## execute a request and return response
  ///
  /// execucte a RaftRequest and return response
  ///
  /// ### Signature:
  /// - sock: Req socket object
  /// - req: the reqest value
  ///
  /// Returns: RaftResponse option
  let request (sock: IClient) (req: RaftRequest) =
    req
    |> Binary.encode
    |> fun body -> { Body = body }
    |> sock.Request

  // ** getSocket

  /// ## getSocket for Member
  ///
  /// Gets the socket we memoized for given MemberId, else creates one and instantiates a
  /// connection.
  ///
  /// ### Signature:
  /// - appState: current TVar<AppState>
  ///
  /// Returns: Req option
  let getSocket (mem: RaftMember) (connections: Map<Id,IClient>) : IClient option =
    Map.tryFind mem.Id connections

  // ** disposeSocket

  /// ## Dispose of a client socket
  ///
  /// Dispose of a client socket that we don't need anymore.
  ///
  /// ### Signature:
  /// - mem: RaftMember whose socket should be disposed of.
  /// - appState: RaftAppContext TVar
  ///
  /// Returns: unit
  let disposeSocket (mem: RaftMember) (connections: Map<Id,IClient>) =
    match Map.tryFind mem.Id connections with
    | Some client ->
      dispose client
      Map.remove mem.Id connections
    | _  -> connections

  // ** rawRequest

  /// ## Perform a raw request cycle on a request socket
  ///
  /// Request a resource and return its response.
  ///
  /// ### Signature:
  /// - request: RaftRequest to perform
  /// - client: Req socket object
  /// - state: RaftAppContext to perform request against
  ///
  /// Returns: RaftResponse option
  let rawRequest (request: RaftRequest) (client: IClient) =
    request
    |> Binary.encode
    |> fun body -> { Body = body }
    |> client.Request

  // ** performRequest

  /// ## Send RaftRequest to mem
  ///
  /// Sends given RaftRequest to mem. If the request times out, None is return to indicate
  /// failure. Otherwise the de-serialized RaftResponse is returned, wrapped in option to
  /// indicate whether de-serialization was successful.
  ///
  /// ### Signature:
  /// - request:    RaftRequest to send
  /// - client:     client socket to use
  ///
  /// Returns: Either<IrisError,RaftResponse>
  let performRequest (request: RaftRequest) (client: IClient) =
    try
      rawRequest request client
      |> Either.mapError (string >> Logger.err "performRequest")
      |> ignore
    with
      | :? TimeoutException ->
        "Operation timed out"
        |> Logger.err "performRequest"
      | exn ->
        exn.Message
        |> sprintf "Encountered an exception: %s"
        |> Logger.err "performRequest"
