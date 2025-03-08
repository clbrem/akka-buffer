namespace Buffer.Test
open System
open Xunit
open Akka.FSharp
open Akka.Actor
open Akka.TestKit.Xunit2
open Xunit.Abstractions
open Buffer.Actors
open MyAssert

type DoAThing_Spec(output: ITestOutputHelper) as this =
    inherit TestKit(config="akka.loglevel=DEBUG", output = output)    
    [<Fact>]
    let ``Can Do A Thing`` () =
        task {
            let doAThing = spawn this.Sys "doStuff" ( TimeSpan.FromSeconds 0.1 |> DoAThing.create )
            doAThing.Tell(DoAThing.DoAThing "This should JUST make it")
            do! System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(0.01))
            doAThing.Tell(DoAThing.DoAThing "This should be a little too late")            
            let! resp = this.ExpectMsgAsync<string>(System.TimeSpan.FromSeconds(0.15))
            Assert.Equal("This should JUST make it", resp)            
            do! this.ExpectNoMsgAsync(System.TimeSpan.FromSeconds(0.09))
        }
    [<Fact>]
    let ``Can Do A Couple Of Things``()=
        task {
            let guid = Guid.NewGuid()
            let manager = spawn this.Sys "manager" (Manager.create (fun item -> $"All Done with {item}"))
            manager.Tell(Start (Guid.NewGuid()))
            manager.Tell(Start guid)
            let! resp = manager.Ask<string> ( Start guid)
            let! secondResp = this.ExpectMsgAsync<string>(System.TimeSpan.FromSeconds(10.0))
            let! thirdResp = this.ExpectMsgAsync<string>(System.TimeSpan.FromSeconds(10.0))
            Assert.Equal($"All Done with {guid}", resp)            
            Assert.Equal($"All Done with {guid}", thirdResp)
            Assert.Equal($"All Done with {guid}", secondResp)
        }
        
    
        
    