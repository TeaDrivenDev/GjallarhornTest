namespace GjallarhornTest

open System
open System.Windows.Forms

open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Observable
open Gjallarhorn.Validation

type ModelThing = { Directory : string }

module VM =
    let directoryExists dir = if System.IO.Directory.Exists dir then None else Some "No"

    let getDirectory() =
        use dialog = new FolderBrowserDialog()
        match dialog.ShowDialog() with
        | DialogResult.OK -> Some dialog.SelectedPath
        | _ -> None

    let create (source : ISignal<ModelThing>) =
        let bindingSource = Binding.createObservableSource()        

        let directory = source |> Binding.memberToFromView bindingSource <@ source.Value.Directory @> (Validation.custom directoryExists)

        let dir = 
            directory
            |> Signal.map (fun d -> { Directory = d }) 
            |> bindingSource.FilterValid // Note that this is required to prevent "invalid" results from showing in the stream

        let chooseDirectoryCommand = bindingSource |> Binding.createCommand "ChooseDirectoryCommand"

        let chooseDir =
            chooseDirectoryCommand
            |> Observable.choose (fun _ -> getDirectory())
            |> Observable.map (fun d -> { Directory = d })

        dir
        |> Observable.merge chooseDir
        |> bindingSource.OutputObservable

        bindingSource

module Model =
    
    let directoryHistory = Mutable.create [ { Directory = @"C:\" } ]

    let directoryStream = Signal.map List.head directoryHistory

    let add directory =
        directoryHistory
        |> Mutable.step (fun previous -> directory :: previous)

    let _subscription = directoryHistory.Subscribe (List.head >> printfn "New: %A")


module Program =

    [<STAThread>]
    [<EntryPoint>]
    let main _ =
        Gjallarhorn.Wpf.install true |> ignore

        let app = System.Windows.Application()

        let vm = VM.create Model.directoryStream 

        let window = GjallarhornTest.UI.MainView()
        window.DataContext <- vm

        use sub = vm |> Observable.subscribe Model.add

        app.Run window
