namespace Buffer

open System
open Akka.FSharp
open Akka.Hosting
open Buffer.Actors
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open RabbitMQ.Client

module Program =
    let addAkka (akkaBuilder: AkkaConfigurationBuilder)  =
        akkaBuilder.WithActors(
            fun system registry resolver ->                
                let actor = spawn system "word-counter-manager" (Manager.create (fun _ ->()) )
                registry.Register<Manager>(actor) 
            ) |> ignore
    let addServices (services: IServiceCollection) =
            services.AddSingleton<ConnectionFactory>(fun _ ->  ConnectionFactory(HostName="localhost", UserName="guest", Password="guest")) |> ignore
            services.AddHostedService<Worker>() |> ignore
            services.AddAkka(                
                "MyActorSystem",
                fun builder sp ->
                   builder
                       .ConfigureLoggers(
                            _.AddLoggerFactory()
                            >> ignore)
                   |> addAkka                
                ) |> ignore
    
    
    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services |> addServices
        builder.Build().Run()

        0 // exit code