namespace ParticlePlaygroundVR

open System
open Aardvark.Base
open Aardvark.Rendering.Text
open Aardvark.Vr
open Aardvark.SceneGraph
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.UI.Generic
open Aardvark.Application.OpenVR

open FSharp.Data.Adaptive

open Aardvark.PhysX

type Message =
    | SetText of string 
    | ToggleVR
    | UpdatePose
    | MyRendered
    | Shoot
    | CameraMessage of FreeFlyController.Message

module Demo =
    open Aardvark.Rendering
    
    let show  (model : AdaptiveModel) (att : list<string * AttributeValue<_>>) (sg : ISg<_>) =

        let view (m : AdaptiveCameraControllerState) =
            let frustum = Frustum.perspective 60.0 0.1 1000.0 1.0 |> AVal.constant
            FreeFlyController.controlledControl m id frustum (AttributeMap.ofList att) sg

        view model.cameraController

    let initial () = 
        let scene = new PhysXScene(V3d(0.0, 0.0, -9.81))

        
        let mat =
            {
                StaticFriction = 0.5
                DynamicFriction = 0.5
                Restitution = 0.5
            }
        let plane = 
            scene.AddStatic {
                Geometry = Plane (Plane3d(V3d.OOI, 0.0))
                Pose = Euclidean3d.Identity
                Material = mat
            }

    
        let boxes =
            [
                for x in -5 .. 5 do
                    for y in -5 .. 5 do
                        for z in 0 .. 3 do
                            let p = V3d(float x, float y, float z + 0.5) * 0.5

                            let box = 
                                scene.AddDynamic  {
                                    Geometry = Box V3d.Half
                                    Pose = Euclidean3d.Translation(p)
                                    Density = 1.0
                                    Material = mat
                                    Velocity = V3d.Zero
                                    AngularVelocity = V3d.Zero
                                }
                            yield box
            ] |> HashSet.ofList



        {
            text = "some text"
            vr = false
            scene = scene
            boxes = boxes
            lastSimulation = None
            spheres = HashMap.empty
            cameraController = FreeFlyController.initial 
            particlePositions = [|Trafo3d.Identity|]
            version = 0
        }
        
    let sw = System.Diagnostics.Stopwatch()
        
    let update (state : VrState) (vr : VrActions) (model : Model) (msg : Message) =
  
        let model = 
            match model.lastSimulation with
            | None -> 
                { model with lastSimulation = Some DateTime.Now; version = model.version + 1 }
            | Some l -> 
                let dt = (DateTime.Now - l).TotalSeconds
                if dt < 0.1  then
                    model.scene.Simulate (float dt)
                    model.scene.ReadParticleProperties()
                    let particleTrafos = model.scene.particlePositions |> Array.map (fun m -> Trafo3d.Translation(V3d(m.XYZ)))
                    { model with lastSimulation = Some DateTime.Now; particlePositions = particleTrafos; version = model.version + 1 }
                else
                    { model with lastSimulation = Some DateTime.Now; version = model.version + 1 }

        match msg with
        | CameraMessage msg -> { model with cameraController = FreeFlyController.update model.cameraController msg }
        | Shoot -> 
            let now = sw.Elapsed.TotalSeconds
            let view = model.cameraController.view
            let mat =
                {
                    StaticFriction = 0.5
                    DynamicFriction = 0.5
                    Restitution = 0.5
                }

            let spheres = 
                [
                    for x in  0 .. 5 do
                        for y in 0 .. 5 do
                            let d = V2d(float x, float y)
                            if d.Length <= 5.0 then
                                let o = d * 0.08
                                let origin = view.Location + o.X * view.Right + o.Y * view.Up

                                let dir = view.Forward
                                let origin = origin + dir * 3.0

                                let bullet =
                                    model.scene.AddDynamic {
                                        Geometry = Sphere 0.05
                                        Density = 1000.0
                                        Velocity = dir * 10.0
                                        Pose = Euclidean3d.Translation(origin)
                                        Material = mat
                                        AngularVelocity = V3d.Zero
                                    }

                                yield (bullet, now)
                ] 

            { model with spheres = HashMap.union model.spheres (HashMap.ofList spheres)}
                        
        | MyRendered -> model
        | UpdatePose -> model
        | SetText t -> 
            { model with text = t }
        | ToggleVR ->
            if model.vr then vr.stop()
            else vr.start()
            { model with vr = not model.vr }

    let threads (model : Model) =
        ThreadPool.empty
        
    let input (msg : VrMessage) =
        match msg with
        | VrMessage.PressButton(_,1) ->
            [show]
        | VrMessage.UpdatePose(_,_) ->  
            [UpdatePose]
        | _ -> 
            []

    let physxSg (m : AdaptiveModel) =

        let boxTrafos = 
            let actors = ASet.toAVal m.boxes
            AVal.custom (fun t ->
                let a = actors.GetValue t
                m.version.GetValue t |> ignore
                a |> HashSet.toArray |> Array.map (fun m -> m.Pose |> Euclidean3d.op_Explicit : Trafo3d)
        
            )

        
        let sphereTrafos = 
            let actors = AMap.toAVal m.spheres
            AVal.custom (fun t ->
                let a = actors.GetValue t
                m.version.GetValue t |> ignore

                a |> HashMap.toKeyArray |> Array.map (fun m -> m.Pose |> Euclidean3d.op_Explicit : Trafo3d)
        
            )
            
        //let particleTrafos = 
        //    let particles = ASet.toAVal m.particlePositions
        //    //let particles = m.particlePositions |> AVal.map (fun theArray -> Array.map (fun (v: V3d) -> Trafo3d.Translation(v.XYZ)))
        //    AVal.custom (fun t ->
        //        m.version.GetValue t |> ignore
        //        particles
        //        //m.particlePositions |> AVal.map (fun theArray -> Array.map (fun v -> v))
        //    )
            
        //let particleTrafos = 
        //    m.particlePositions |> AVal.map (fun theArray -> Array.map (fun (v: V3d) -> Trafo3d.Translation(v.XYZ)))

        Sg.ofList [
            //Sg.box C4b.White (Box3d.FromCenterAndSize(V3d.Zero, V3d.Half))
            //|> Sg.instanced boxTrafos
            //|> Sg.noEvents
            //|> Sg.shader {
            //    do! DefaultSurfaces.trafo
            //    do! DefaultSurfaces.simpleLighting
            //}

            Sg.box' C4b.White (Box3d.FromCenterAndSize(V3d.Zero, V3d.Half))
            |> Sg.instanced boxTrafos
            |> Sg.noEvents
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }
            
            Sg.sphere' 5 C4b.Red 0.05
            |> Sg.instanced sphereTrafos
            |> Sg.noEvents
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }

            Sg.quad
            |> Sg.transform (Trafo3d.Scale(10.0, 10.0, 1.0))
            |> Sg.diffuseTexture DefaultTextures.checkerboard
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.diffuseTexture
                do! DefaultSurfaces.simpleLighting
            }
            
            Sg.sphere' 5 C4b.Blue 0.1
            |> Sg.instanced m.particlePositions
            |> Sg.noEvents
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }
        ]


    let ui (info : VrSystemInfo) (m : AdaptiveModel) =
        let t = m.vr |> AVal.map (function true -> "Stop VR" | false -> "Start VR")


        let hmd =
            m.vr |> AVal.bind (fun vr ->
                if vr then
                    AVal.map2 (Array.map2 (fun (v : Trafo3d) (p : Trafo3d) -> (v * p).Inverse)) info.render.viewTrafos info.render.projTrafos
                else
                    AVal.constant [|Trafo3d.Translation(100000.0,10000.0,1000.0)|]
            )

        let hmdSg =
            List.init 2 (fun i ->
                Sg.wireBox (AVal.constant C4b.Yellow) (AVal.constant (Box3d(V3d(-1,-1,-1000), V3d(1.0,1.0,-0.9))))
                |> Sg.noEvents
                |> Sg.trafo (hmd |> AVal.map (fun t -> if i < t.Length then t.[i] else Trafo3d.Translation(100000.0,10000.0,1000.0)))
            )
            |> Sg.ofList
            

        let chap =
            match info.bounds with
            | Some bounds ->
                let arr = bounds.EdgeLines |> Seq.toArray
                Sg.lines (AVal.constant C4b.Red) (AVal.constant arr)
                |> Sg.noEvents
                |> Sg.transform (Trafo3d.FromBasis(V3d.IOO, V3d.OOI, -V3d.OIO, V3d.Zero))
            | _ ->
                Sg.empty

        let physxScene = physxSg m

        let stuff =
            Sg.ofList [hmdSg; chap; physxScene]
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.vertexColor
            }

        div [ style "width: 100%; height: 100%" ] [
            show m [ style "width: 100%; height: 100%"; 
                     attribute "data-renderalways" "true" 
                   ] (
                Sg.textWithConfig TextConfig.Default m.text
                |> Sg.noEvents
                |> Sg.andAlso stuff
            ) |> UI.map CameraMessage
            textarea [ style "position: fixed; top: 5px; left: 5px"; onChange SetText ] m.text
            button [ style "position: fixed; bottom: 5px; right: 5px"; onClick (fun () -> ToggleVR) ] t
            button [ style "position: fixed; bottom: 20px; right: 5px"; onClick (fun () -> Shoot) ] [text "shoot"]

        ]

   
    let vr (info : VrSystemInfo) (m : AdaptiveModel) =
    
        let deviceSgs = 
            info.state.devices |> AMap.toASet |> ASet.chooseA (fun (_,d) ->
                d.model |> AVal.map (fun m ->
                    match m.Value with
                    | Some sg -> 
                        sg 
                        |> Sg.noEvents 
                        |> Sg.trafo d.pose.deviceToWorld
                        |> Sg.onOff d.pose.isValid
                        |> Some
                    | None -> 
                        None 
                )
            )
            |> Sg.set
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.diffuseTexture
                do! DefaultSurfaces.simpleLighting
            }

        let physxScene = physxSg m

        Sg.textWithConfig TextConfig.Default m.text
        |> Sg.noEvents
        |> Sg.andAlso deviceSgs
        |> Sg.andAlso physxScene

        
    let pause (info : VrSystemInfo) (m : AdaptiveModel) =
        Sg.box' C4b.Red Box3d.Unit
        |> Sg.noEvents
        |> Sg.shader {
            do! DefaultSurfaces.trafo
            do! DefaultSurfaces.vertexColor
            do! DefaultSurfaces.simpleLighting
        }

    let app =
        {
            unpersist = Unpersist.instance
            initial = initial ()
            update = update
            threads = threads
            input = input
            ui = ui
            vr = vr
            pauseScene = Some pause
        }