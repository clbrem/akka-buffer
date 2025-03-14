namespace Buffer.Client.Test
open Buffer.Client
open Xunit

module HandlerTest =
    [<Fact>]
    let ``Can Send a Message`` () =        
        task {
            let factory = RabbitMQ.Client.ConnectionFactory(HostName="localhost")
            do! Handlers.sendMessage factory "hello"
            }

