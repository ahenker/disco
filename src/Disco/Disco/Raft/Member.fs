(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Raft

// * Imports

open Disco.Core

#if FABLE_COMPILER

open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System
open System.Net
open FlatBuffers
open Disco.Serialization

#endif

// * MemberStatus

type MemberStatus =
  | Joining                             // excludes mem from voting
  | Running                             // normal execution state
  | Failed                              // mem has failed for some reason

  // ** ToString

  override self.ToString() =
    match self with
    | Joining -> "Joining"
    | Running -> "Running"
    | Failed  -> "Failed"

  // ** Parse

  static member Parse (str: string) =
    match str with
    | "Joining" -> Joining
    | "Running" -> Running
    | "Failed"  -> Failed
    | _         -> failwithf "MemberState: failed to parse %s" str

  // ** TryParse

  static member TryParse (str: string) =
    try
      str |> MemberStatus.Parse |> Either.succeed
    with
      | exn ->
        sprintf "Could not parse MemberStatus: %s" exn.Message
        |> Error.asParseError "MemberStatus.TryParse"
        |> Either.fail

  // ** ToOffset

  member self.ToOffset () =
    match self with
      | Running -> MemberStatusFB.RunningFB
      | Joining -> MemberStatusFB.JoiningFB
      | Failed  -> MemberStatusFB.FailedFB

  // ** FromFB

  static member FromFB (fb: MemberStatusFB) =
    #if FABLE_COMPILER
    match fb with
      | x when x = MemberStatusFB.JoiningFB -> Right Joining
      | x when x = MemberStatusFB.RunningFB -> Right Running
      | x when x = MemberStatusFB.FailedFB  -> Right Failed
      | x ->
        sprintf "Could not parse MemberStatus: %A" x
        |> Error.asParseError "MemberStatus.FromFB"
        |> Either.fail
    #else
    match fb with
      | MemberStatusFB.JoiningFB -> Right Joining
      | MemberStatusFB.RunningFB -> Right Running
      | MemberStatusFB.FailedFB  -> Right Failed
      | x ->
        sprintf "Could not parse MemberStatus: %A" x
        |> Error.asParseError "MemberStatus.FromFB"
        |> Either.fail
    #endif

// * RaftMemberYaml

#if !FABLE_COMPILER && !DISCO_NODES

type RaftMemberYaml() =
  [<DefaultValue>] val mutable Id:               string
  [<DefaultValue>] val mutable HostName:         string
  [<DefaultValue>] val mutable IpAddress:        string
  [<DefaultValue>] val mutable MulticastAddress: string
  [<DefaultValue>] val mutable MulticastPort:    uint16
  [<DefaultValue>] val mutable HttpPort:         uint16
  [<DefaultValue>] val mutable RaftPort:         uint16
  [<DefaultValue>] val mutable WsPort:           uint16
  [<DefaultValue>] val mutable GitPort:          uint16
  [<DefaultValue>] val mutable ApiPort:          uint16
  [<DefaultValue>] val mutable State:            string
  [<DefaultValue>] val mutable Status:           string
  [<DefaultValue>] val mutable NextIndex:        Index
  [<DefaultValue>] val mutable MatchIndex:       Index
  [<DefaultValue>] val mutable Voting:           bool
  [<DefaultValue>] val mutable VotedForMe:       bool

#endif

// * MemberSate

/// Raft Member State
///
/// ## States
///  - `Follower` - this Member is currently following a different Leader
///  - `Candiate` - this Member currently seeks to become Leader
///  - `Leader`   - this Member currently is Leader of the cluster
type MemberState =
  | Follower
  | Candidate
  | Leader

  // ** ToString

  override self.ToString() =
    match self with
    | Follower -> "Follower"
    | Candidate -> "Candiate"
    | Leader -> "Leader"

  // ** Parse

  static member Parse str =
    match str with
    | "Follower"  -> Follower
    | "Candidate" -> Candidate
    | "Leader"    -> Leader
    | _           -> failwithf "unable to parse %A as RaftState" str

  // ** TryParse

  static member TryParse str =
    try
      MemberState.Parse str
      |> Either.succeed
    with exn ->
      exn.Message
      |> Error.asParseError "RaftState.TryParse"
      |> Either.fail

  // ** ToOffset

  member self.ToOffset(_:FlatBufferBuilder) =
    match self with
    | Follower  -> MemberStateFB.FollowerFB
    | Leader    -> MemberStateFB.LeaderFB
    | Candidate -> MemberStateFB.CandidateFB

  // ** FromFB

  static member FromFB(fb: MemberStateFB) =
    match fb with
    #if FABLE_COMPILER
    | x when x = MemberStateFB.FollowerFB  -> Right Follower
    | x when x = MemberStateFB.LeaderFB    -> Right Leader
    | x when x = MemberStateFB.CandidateFB -> Right Candidate
    #else
    | MemberStateFB.FollowerFB  -> Right Follower
    | MemberStateFB.LeaderFB    -> Right Leader
    | MemberStateFB.CandidateFB -> Right Candidate
    #endif
    | other ->
      other
      |> String.format "unknown raft state: {0}"
      |> Error.asParseError "RaftState.FromFB"
      |> Either.fail

// * RaftMember

type RaftMember =
  { Id:               MemberId
    HostName:         Name
    IpAddress:        IpAddress
    MulticastAddress: IpAddress
    MulticastPort:    Port
    HttpPort:         Port
    RaftPort:         Port
    WsPort:           Port
    GitPort:          Port
    ApiPort:          Port
    Voting:           bool
    VotedForMe:       bool
    State:            MemberState
    Status:           MemberStatus
    NextIndex:        Index
    MatchIndex:       Index }

  // ** optics

  static member Id_ =
    (fun (mem:RaftMember) -> mem.Id),
    (fun id (mem:RaftMember) -> { mem with Id = id })

  static member HostName_ =
    (fun (mem:RaftMember) -> mem.HostName),
    (fun hostName (mem:RaftMember) -> { mem with HostName = hostName })

  static member IpAddress_ =
    (fun (mem:RaftMember) -> mem.IpAddress),
    (fun ipAddress (mem:RaftMember) -> { mem with IpAddress = ipAddress })

  static member MulticastAddress_ =
    (fun (mem:RaftMember) -> mem.MulticastAddress),
    (fun multicastAddress (mem:RaftMember) -> { mem with MulticastAddress = multicastAddress })

  static member MulticastPort_ =
    (fun (mem:RaftMember) -> mem.MulticastPort),
    (fun multicastPort (mem:RaftMember) -> { mem with MulticastPort = multicastPort })

  static member RaftPort_ =
    (fun (mem:RaftMember) -> mem.RaftPort),
    (fun raftPort (mem:RaftMember) -> { mem with RaftPort = raftPort })

  static member HttpPort_ =
    (fun (mem:RaftMember) -> mem.HttpPort),
    (fun httpPort (mem:RaftMember) -> { mem with HttpPort = httpPort })

  static member WsPort_ =
    (fun (mem:RaftMember) -> mem.WsPort),
    (fun wsPort (mem:RaftMember) -> { mem with WsPort = wsPort })

  static member GitPort_ =
    (fun (mem:RaftMember) -> mem.GitPort),
    (fun gitPort (mem:RaftMember) -> { mem with GitPort = gitPort })

  static member ApiPort_ =
    (fun (mem:RaftMember) -> mem.ApiPort),
    (fun apiPort (mem:RaftMember) -> { mem with ApiPort = apiPort })

  static member Voting_ =
    (fun (mem:RaftMember) -> mem.Voting),
    (fun voting (mem:RaftMember) -> { mem with Voting = voting })

  static member VotedForMe_ =
    (fun (mem:RaftMember) -> mem.VotedForMe),
    (fun votedForMe (mem:RaftMember) -> { mem with VotedForMe = votedForMe })

  static member State_ =
    (fun (mem:RaftMember) -> mem.State),
    (fun state (mem:RaftMember) -> { mem with State = state })

  static member Status_ =
    (fun (mem:RaftMember) -> mem.Status),
    (fun status (mem:RaftMember) -> { mem with Status = status })

  static member NextIndex_ =
    (fun (mem:RaftMember) -> mem.NextIndex),
    (fun nextIndex (mem:RaftMember) -> { mem with NextIndex = nextIndex })

  static member MatchIndex_ =
    (fun (mem:RaftMember) -> mem.MatchIndex),
    (fun matchIndex (mem:RaftMember) -> { mem with MatchIndex = matchIndex })

  // ** ToString

  override self.ToString() =
    sprintf "%O (%O, %O) on %A (%O:%d) (group:%O:%d) (NextIdx: %A) (MatchId: %d)"
      self.Id
      self.HostName
      self.State
      self.Status
      self.IpAddress
      self.RaftPort
      self.MulticastAddress
      self.MulticastPort
      self.NextIndex
      self.MatchIndex

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml () =
    let yaml = RaftMemberYaml()
    yaml.Id               <- string self.Id
    yaml.HostName         <- unwrap self.HostName
    yaml.IpAddress        <- string self.IpAddress
    yaml.MulticastAddress <- string self.MulticastAddress
    yaml.MulticastPort    <- unwrap self.MulticastPort
    yaml.HttpPort         <- unwrap self.HttpPort
    yaml.RaftPort         <- unwrap self.RaftPort
    yaml.WsPort           <- unwrap self.WsPort
    yaml.GitPort          <- unwrap self.GitPort
    yaml.ApiPort          <- unwrap self.ApiPort
    yaml.State            <- string self.State
    yaml.Status           <- string self.Status
    yaml.NextIndex        <- self.NextIndex
    yaml.MatchIndex       <- self.MatchIndex
    yaml.Voting           <- self.Voting
    yaml.VotedForMe       <- self.VotedForMe
    yaml

  // ** FromYaml

  static member FromYaml (yaml: RaftMemberYaml) : Either<DiscoError, RaftMember> =
    either {
      let! id = DiscoId.TryParse yaml.Id
      let! ip = IpAddress.TryParse yaml.IpAddress
      let! mcastip = IpAddress.TryParse yaml.MulticastAddress
      let! state = MemberState.TryParse yaml.State
      let! status = MemberStatus.TryParse yaml.Status
      return {
        Id               = id
        HostName         = name yaml.HostName
        MulticastAddress = mcastip
        MulticastPort    = port yaml.MulticastPort
        IpAddress        = ip
        HttpPort         = port yaml.HttpPort
        RaftPort         = port yaml.RaftPort
        WsPort           = port yaml.WsPort
        GitPort          = port yaml.GitPort
        ApiPort          = port yaml.ApiPort
        Voting           = yaml.Voting
        VotedForMe       = yaml.VotedForMe
        NextIndex        = yaml.NextIndex
        MatchIndex       = yaml.MatchIndex
        State            = state
        Status           = status
      }
    }

  #endif

  // ** ToOffset

  member mem.ToOffset (builder: FlatBufferBuilder) =
    let id = RaftMemberFB.CreateIdVector(builder,mem.Id.ToByteArray())
    let ip = string mem.IpAddress |> builder.CreateString
    let mcastip = string mem.MulticastAddress |> builder.CreateString

    let hostname =
      let unwrapped = unwrap mem.HostName
      if isNull unwrapped then
        None
      else
        unwrapped |> builder.CreateString |> Some

    let state = mem.State.ToOffset(builder)
    let status = mem.Status.ToOffset()

    RaftMemberFB.StartRaftMemberFB(builder)
    RaftMemberFB.AddId(builder, id)

    match hostname with
    | Some hostname -> RaftMemberFB.AddHostName(builder, hostname)
    | None -> ()

    RaftMemberFB.AddMulticastAddress(builder, mcastip)
    RaftMemberFB.AddMulticastPort(builder, unwrap mem.MulticastPort)
    RaftMemberFB.AddIpAddress(builder, ip)
    RaftMemberFB.AddRaftPort(builder, unwrap mem.RaftPort)
    RaftMemberFB.AddHttpPort(builder, unwrap mem.HttpPort)
    RaftMemberFB.AddWsPort(builder, unwrap mem.WsPort)
    RaftMemberFB.AddGitPort(builder, unwrap mem.GitPort)
    RaftMemberFB.AddApiPort(builder, unwrap mem.ApiPort)
    RaftMemberFB.AddVoting(builder, mem.Voting)
    RaftMemberFB.AddVotedForMe(builder, mem.VotedForMe)
    RaftMemberFB.AddState(builder, state)
    RaftMemberFB.AddStatus(builder, status)
    RaftMemberFB.AddNextIndex(builder, int mem.NextIndex)
    RaftMemberFB.AddMatchIndex(builder, int mem.MatchIndex)
    RaftMemberFB.EndRaftMemberFB(builder)

  // ** FromFB

  static member FromFB (fb: RaftMemberFB) : Either<DiscoError, RaftMember> =
    either {
      let! id = Id.decodeId fb
      let! state = MemberState.FromFB fb.State
      let! status = MemberStatus.FromFB fb.Status
      let! ip = IpAddress.TryParse fb.IpAddress
      let! mcastip = IpAddress.TryParse fb.MulticastAddress
      return {
        Id               = id
        State            = state
        Status           = status
        HostName         = name fb.HostName
        IpAddress        = ip
        MulticastAddress = mcastip
        MulticastPort    = port fb.MulticastPort
        HttpPort         = port fb.HttpPort
        RaftPort         = port fb.RaftPort
        WsPort           = port fb.WsPort
        GitPort          = port fb.GitPort
        ApiPort          = port fb.ApiPort
        Voting           = fb.Voting
        VotedForMe       = fb.VotedForMe
        NextIndex        = index fb.NextIndex
        MatchIndex       = index fb.MatchIndex
      }
    }

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> RaftMemberFB.GetRootAsRaftMemberFB
    |> RaftMember.FromFB

// * ConfigChangeYaml

#if !FABLE_COMPILER && !DISCO_NODES

type ConfigChangeYaml() =
  [<DefaultValue>] val mutable ChangeType : string
  [<DefaultValue>] val mutable Member       : RaftMemberYaml

  // ** MemberAdded

  static member MemberAdded (mem: RaftMemberYaml) =
    let yaml = new ConfigChangeYaml()
    yaml.ChangeType <- "MemberAdded"
    yaml.Member <- mem
    yaml

  // ** MemberRemoved

  static member MemberRemoved (mem: RaftMemberYaml) =
    let yaml = new ConfigChangeYaml()
    yaml.ChangeType <- "MemberRemoved"
    yaml.Member <- mem
    yaml

#endif

// * ConfigChange

type ConfigChange =
  | MemberAdded   of RaftMember
  | MemberRemoved of RaftMember

  // ** ToString

  override self.ToString() =
    match self with
    | MemberAdded   n -> sprintf "MemberAdded (%s)"   (string n.Id)
    | MemberRemoved n ->sprintf "MemberRemoved (%s)" (string n.Id)

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    match self with
      | MemberAdded mem ->
        let mem = mem.ToOffset(builder)
        ConfigChangeFB.StartConfigChangeFB(builder)
        ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.MemberAdded)
        ConfigChangeFB.AddMember(builder, mem)
        ConfigChangeFB.EndConfigChangeFB(builder)
      | MemberRemoved mem ->
        let mem = mem.ToOffset(builder)
        ConfigChangeFB.StartConfigChangeFB(builder)
        ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.MemberRemoved)
        ConfigChangeFB.AddMember(builder, mem)
        ConfigChangeFB.EndConfigChangeFB(builder)

  // ** FromFB

  static member FromFB (fb: ConfigChangeFB) : Either<DiscoError,ConfigChange> =
    either {
      #if FABLE_COMPILER
      let! mem = fb.Member |> RaftMember.FromFB
      match fb.Type with
      | x when x = ConfigChangeTypeFB.MemberAdded   -> return (MemberAdded   mem)
      | x when x = ConfigChangeTypeFB.MemberRemoved -> return (MemberRemoved mem)
      | x ->
        return!
          sprintf "Could not parse ConfigChangeTypeFB %A" x
          |> Error.asParseError "ConfigChange.FromFB"
          |> Either.fail
      #else
      let nullable = fb.Member
      if nullable.HasValue then
        let! mem = RaftMember.FromFB nullable.Value
        match fb.Type with
        | ConfigChangeTypeFB.MemberAdded   -> return (MemberAdded   mem)
        | ConfigChangeTypeFB.MemberRemoved -> return (MemberRemoved mem)
        | x ->
          return!
            sprintf "Could not parse ConfigChangeTypeFB %A" x
            |> Error.asParseError "ConfigChange.FromFB"
            |> Either.fail
      else
        return!
          "Could not parse empty ConfigChangeFB payload"
          |> Error.asParseError "ConfigChange.FromFB"
          |> Either.fail
      #endif
    }

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> ConfigChangeFB.GetRootAsConfigChangeFB
    |> ConfigChange.FromFB

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  member self.ToYaml() =
    match self with
    | MemberAdded mem   -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberAdded
    | MemberRemoved mem -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberRemoved

  // ** FromYaml

  static member FromYaml (yml: ConfigChangeYaml) =
    match yml.ChangeType with
    | "MemberAdded" -> either {
        let! mem = Yaml.fromYaml yml.Member
        return MemberAdded(mem)
      }
    | "MemberRemoved" -> either {
        let! mem = Yaml.fromYaml yml.Member
        return MemberRemoved(mem)
      }
    | x ->
      sprintf "Could not parse %s as ConfigChange" x
      |> Error.asParseError "ConfigChange.FromYaml"
      |> Either.fail

  #endif

// * Member module

[<RequireQualifiedAccess>]
module Member =

  open Aether

  // ** getters

  let id = Optic.get RaftMember.Id_
  let hostName = Optic.get RaftMember.HostName_
  let ipAddress = Optic.get RaftMember.IpAddress_
  let multicastAddress = Optic.get RaftMember.MulticastAddress_
  let multicastPort = Optic.get RaftMember.MulticastPort_
  let raftPort = Optic.get RaftMember.RaftPort_
  let httpPort = Optic.get RaftMember.HttpPort_
  let wsPort = Optic.get RaftMember.WsPort_
  let gitPort = Optic.get RaftMember.GitPort_
  let apiPort = Optic.get RaftMember.ApiPort_
  let voting = Optic.get RaftMember.Voting_
  let votedForMe = Optic.get RaftMember.VotedForMe_
  let status = Optic.get RaftMember.Status_
  let state = Optic.get RaftMember.State_
  let nextIndex = Optic.get RaftMember.NextIndex_
  let matchIndex = Optic.get RaftMember.MatchIndex_

  // ** setters

  let setId = Optic.set RaftMember.Id_
  let setHostName = Optic.set RaftMember.HostName_
  let setIpAddress = Optic.set RaftMember.IpAddress_
  let setMulticastAddress = Optic.set RaftMember.MulticastAddress_
  let setMulticastPort = Optic.set RaftMember.MulticastPort_
  let setRaftPort = Optic.set RaftMember.RaftPort_
  let setHttpPort = Optic.set RaftMember.HttpPort_
  let setWsPort = Optic.set RaftMember.WsPort_
  let setGitPort = Optic.set RaftMember.GitPort_
  let setApiPort = Optic.set RaftMember.ApiPort_
  let setVoting = Optic.set RaftMember.Voting_
  let setVotedForMe = Optic.set RaftMember.VotedForMe_
  let setStatus = Optic.set RaftMember.Status_
  let setState = Optic.set RaftMember.State_
  let setNextIndex = Optic.set RaftMember.NextIndex_
  let setMatchIndex = Optic.set RaftMember.MatchIndex_

  // ** create

  let create id =
    #if FABLE_COMPILER
    let hostname = Fable.Import.Browser.window.location.host
    #else
    let hostname = Network.getHostName ()
    #endif
    { Id               = id
      HostName         = name hostname
      IpAddress        = IPv4Address "127.0.0.1"
      MulticastAddress = IpAddress.Parse Constants.DEFAULT_MCAST_ADDRESS
      MulticastPort    = Measure.port Constants.DEFAULT_MCAST_PORT
      HttpPort         = Measure.port Constants.DEFAULT_HTTP_PORT
      RaftPort         = Measure.port Constants.DEFAULT_RAFT_PORT
      WsPort           = Measure.port Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort          = Measure.port Constants.DEFAULT_GIT_PORT
      ApiPort          = Measure.port Constants.DEFAULT_API_PORT
      Status           = Running
      State            = Follower
      Voting           = true
      VotedForMe       = false
      NextIndex        = index 1
      MatchIndex       = index 0 }

  // ** isVoting

  let isVoting (mem : RaftMember) : bool =
    match mem.Status, mem.Voting with
    | Running, true -> true
    | _ -> false

  // ** hasVotedForMe

  let hasVoteForMe mem = mem.VotedForMe

  // ** setHasSufficientLogs

  let setHasSufficientLogs mem =
    { mem with
        Status = Running
        Voting = true }

  // ** hasSufficientLogs

  let hasSufficientLogs mem =
    mem.Status = Running

  // ** canVote

  let canVote peer =
    isVoting peer && hasVoteForMe peer

  // ** added

  let private added oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (Member.id >> ((=) mem.Id)) oldmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] newmems

  // ** removed

  let private removed oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (Member.id >> ((=) mem.Id)) newmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] oldmems

  // ** changes

  let changes (oldmems: RaftMember array) (newmems: RaftMember array) =
    []
    |> List.append (added oldmems newmems)
    |> List.append (removed oldmems newmems)
    |> Array.ofList
