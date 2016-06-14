namespace GjallarhornTest

open System
open System.Windows.Forms

open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Observable
open Gjallarhorn.Validation

type ModelThing = { Directory : string }

module VM =
    let noValidation _ = None

    let directoryExists dir = if System.IO.Directory.Exists dir then None else Some "No"

    let getDirectory() =
        use dialog = new FolderBrowserDialog()
        match dialog.ShowDialog() with
        | DialogResult.OK -> Some dialog.SelectedPath
        | _ -> None

    let create (thingIn : IObservable<ModelThing>) initialValue =
        let bindingSource = Binding.createObservableSource()

        let source = bindingSource.ObservableToSignal initialValue thingIn

        let directory = source |> Binding.memberToFromView bindingSource <@ initialValue.Directory @> (Validation.custom directoryExists)

        let dir = Signal.map (fun d -> { Directory = d }) directory

        let chooseDirectoryCommand = bindingSource |> Binding.createCommand "ChooseDirectoryCommand"

        let chooseDir =
            chooseDirectoryCommand
            |> Observable.map (fun _ ->
                match getDirectory() with
                | Some dir -> { source.Value with Directory = dir }
                | None -> source.Value)

        dir
        |> Observable.merge chooseDir
        |> bindingSource.OutputObservable

        bindingSource

module Model =
    
    let directoryHistory = Mutable.create []

    let directoryStream = Observable.map List.head directoryHistory

    let add directory =
        directoryHistory
        |> Mutable.step (fun previous -> directory :: previous)


module Program =

    [<STAThread>]
    [<EntryPoint>]
    let main _ =
        Gjallarhorn.Wpf.install true |> ignore

        let app = System.Windows.Application()

        let vm = VM.create Model.directoryStream { Directory = @"C:\" }

        let window = GjallarhornTest.UI.MainView()
        window.DataContext <- vm

        use sub = vm |> Observable.subscribe Model.add

        app.Run window
