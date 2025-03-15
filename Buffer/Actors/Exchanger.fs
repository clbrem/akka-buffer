namespace Buffer.Actors
open Akka.FSharp
open Akka.Actor
type Exchanger =
    | MakeChild of string
    | SwitchChild of string    

module Exchanger =
    let rec create (mailbox: Actor<Exchanger>) =
        let rec loop () =                 
            actor {
                match! mailbox.Receive() with
                | MakeChild name ->
                    spawn mailbox.Context name create |> ignore
                    return! loop ()
                | SwitchChild name -> 
                    let child = mailbox.Context.Child(name)
                    if child.IsNobody() then
                        return! loop()
                    else
                        mailbox.Context.
                        child.Tell(SwitchChild name)
                    return! loop ()
            }
        loop()
    
    

