namespace Buffer.Test
open Xunit
open Akka.FSharp
open Akka.Actor
open Akka.TestKit.Xunit2
open Xunit.Abstractions
open Buffer.Actors


type DoAThing_Spec(output: ITestOutputHelper) as this =
    inherit TestKit(config="akka.loglevel=DEBUG", output = output)    
    [<Fact>]
    let ``Can Do A Thing`` () =
        task {
            let doAThing = spawn this.Sys "doStuff" (System.TimeSpan.FromSeconds 0.1 |> DoAThing.create )
            doAThing.Tell(DoAThing.DoAThing "This should JUST make it")
            System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(0.01)).Wait()
            doAThing.Tell(DoAThing.DoAThing "This should be a little too late")            
            let! resp = this.ExpectMsgAsync<string>(System.TimeSpan.FromSeconds(0.15))
            Assert.Equal("This should JUST make it", resp)            
            do! this.ExpectNoMsgAsync(System.TimeSpan.FromSeconds(0.09))
        }
    
        
    