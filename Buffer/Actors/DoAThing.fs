namespace Buffer.Actors
open Buffer.Timeout
open System
open Akka.Event
open Akka.FSharp
open Akka.Actor

type DoAThing =    
    | DoAThing of obj
    | ThingIsDone
    | Stop

module DoAThing =
    let nameOfGuid guid = $"doAThing-{guid.ToString()}" 
    let create timespan =
        fun (mailbox: Actor<DoAThing>) ->
           let logger = mailbox.Context.GetLogger()           
           let rec loop message=
               mailbox.Context.OnTimeout(Stop, timeout=TimeSpan.FromSeconds(30.0))
               match message with
               // We're live! waiting on a thing to be done!
               | Some (sender: IActorRef, msg) -> 
                   actor {                       
                       match! mailbox.Receive() with
                       // new message better stash
                       | DoAThing newMsg ->
                           logger.Info("Stashing message {0}", newMsg)
                           do mailbox.Stash()
                           return! loop (Some (sender, msg))
                       | ThingIsDone ->
                           logger.Info("Done with {0}", [|msg|])
                           sender.Tell msg
                           mailbox.Unstash()
                           return! loop None
                       | Stop ->
                           logger.Warning("Received stop but not done with {0}", [|msg|])
                           mailbox.Unhandled(Stop)
                   }
               // Just chillaxin' waiting for a new thing to do
               | None -> 
                   actor {
                       match! mailbox.Receive() with
                       | DoAThing msg ->
                           logger.Info("Received message {0}", msg)
                           mailbox.Context.OnTimeout(ThingIsDone, "timer", timespan)                           
                           return! loop (Some (mailbox.Sender(), msg))
                       // yeah this shouldn't happen
                       | ThingIsDone -> 
                           mailbox.Unhandled(ThingIsDone)
                           return! loop None
                       | Stop ->
                           logger.Info("Goodbye Cruel World")
                           mailbox.Context.Stop(mailbox.Self)
                   }
           loop None 