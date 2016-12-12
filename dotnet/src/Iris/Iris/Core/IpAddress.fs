namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import.JS
open System.Text.RegularExpressions

#else

open System.Net

#endif

type IpAddress =
  | IPv4Address of string
  | IPv6Address of string

  override self.ToString () =
    match self with
      | IPv4Address str -> str
      | IPv6Address str -> str

  static member Parse (str: string) =
#if FABLE_COMPILER
    let regex = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")
    match regex.IsMatch str with
    | true -> IPv4Address str
    | _    -> IPv6Address str
#else
    let ip = IPAddress.Parse str
    match ip.AddressFamily with
      | Sockets.AddressFamily.InterNetwork   -> IPv4Address str
      | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str
      | _ -> failwith "Addressfamily not supportet"
#endif

  static member TryParse (str: string) =
#if FABLE_COMPILER
    try
      IpAddress.Parse str
      |> Either.succeed
    with
      | exn ->
        sprintf "Unable to parse IP: %s Cause: %s" str exn.Message
        |> ParseError
        |> Either.fail
#else
    try
      let ip = IPAddress.Parse(str)
      match ip.AddressFamily with
      | Sockets.AddressFamily.InterNetwork   -> IPv4Address str |> Right
      | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str |> Right
      | fam ->
        ("IpAddress.Parse", sprintf "Unable to parse IP: %s Unsupported AddressFamily: %A" str fam)
        |> ParseError
        |> Either.fail

    with
      | exn ->
        ("IpAddress.Parse", sprintf "Unable to parse IP: %s Cause: %s" str exn.Message)
        |> ParseError
        |> Either.fail
#endif
