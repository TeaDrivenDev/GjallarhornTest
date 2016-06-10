namespace GjallarhornTest

open System
open System.Windows.Forms

open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Observable
open Gjallarhorn.Validation

type ModelThing = { Directory : string }

module VM =

    let getDirectory() =
        use dialog = new FolderBrowserDialog()
        match dialog.ShowDialog() with
        | DialogResult.OK -> Some dialog.SelectedPath
        | _ -> None

    let create (thingIn : IObservable<ModelThing>) initialValue =
        let subject = Binding.createSubject()

        let source = subject.ObservableToSignal initialValue thingIn

        let directory = source |> Binding.editMember subject <@ initialValue.Directory @> noValidation

        // This should call `getDirectory()` and write the result back into the model.
        // However, I can't figure out how to fit that into the "Observable" concept.
        let chooseDirectoryCommand = subject.Command "ChooseDirectoryCommand"

        subject

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

        let window = GjallarhornTest.UI.MainView()
        window.DataContext <- VM.create Model.directoryStream { Directory = @"C:\" }

        app.Run window
