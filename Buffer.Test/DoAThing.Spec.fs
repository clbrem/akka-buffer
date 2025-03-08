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
            do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(0.01))
            doAThing.Tell(DoAThing.DoAThing "This should be a little too late")            
            let! resp = this.ExpectMsgAsync<string>(TimeSpan.FromSeconds(0.15))
            Assert.Equal("This should JUST make it", resp)            
            do! this.ExpectNoMsgAsync(TimeSpan.FromSeconds(0.09))
        }
    [<Fact>]
    let ``Can Do A Couple Of Things``()=
        task {            
            let guid2 = Guid.NewGuid()
            let guid = Guid.NewGuid()
            let manager = spawn this.Sys "manager" (Manager.create (fun item -> $"All Done with {item}"))
            manager.Tell(Start guid )
            do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(0.1))
            manager.Tell(Start guid2)
            do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(0.1))
            manager.Tell(Start guid)
            let! resp = this.ExpectMsgAsync<string>(TimeSpan.FromSeconds(30.0))
            let! secondResp = this.ExpectMsgAsync<string>(TimeSpan.FromSeconds(30.0))
            let! thirdResp = this.ExpectMsgAsync<string>(TimeSpan.FromSeconds(30.0))
            Assert.Equal($"All Done with {guid}", resp)
            Assert.Equal($"All Done with {guid2}", secondResp)
            Assert.Equal($"All Done with {guid}", thirdResp)
        }
