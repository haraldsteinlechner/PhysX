open System.Runtime.InteropServices
open Aardvark.Base
open FSharp.Data.Adaptive
open Aardvark.Application
open Aardvark.SceneGraph
open Aardvark.Rendering

open Aardvark.PhysX

[<EntryPoint>]
let main args =
    Aardvark.Init()

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
        
    let rand = RandomSystem()
    let randomBox() =
        let p = rand.UniformV2d(Box2d.FromCenterAndSize(V2d.Zero, V2d.II * 5.0))
        let r = rand.UniformV3dDirection()
        let a = rand.UniformDouble() * Constant.PiTimesTwo
        {
            Geometry = Box V3d.Half
            Pose = Euclidean3d.Translation(V3d(p, 5.0)) * Euclidean3d.Rotation(r, a)
            Density = 1.0
            Material = mat
            Velocity = V3d.Zero
            AngularVelocity = V3d.Zero
        }



    let boxes = cset []
    let spheres = cmap<PhysXActor, float>()
    

    
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
                transact (fun () -> boxes.Add box |> ignore)




    let mutable run = false

    let win = 
        window {
            backend Backend.GL
        }

    let delete = new System.Collections.Concurrent.BlockingCollection<PhysXActor>()

    let deleter =
        startThread <| fun () ->
            for a in delete.GetConsumingEnumerable() do
            
                transact (fun () ->
                    lock boxes (fun () ->
                        boxes.Remove a |> ignore
                        spheres.Remove a |> ignore
                    )
                )
                a.Dispose()

    let sim =
        let sw = System.Diagnostics.Stopwatch()
        win.Time |> AVal.map (fun _ ->
            let dt = sw.Elapsed.TotalSeconds
            sw.Restart()
            if run && dt > 0.0 then 
                scene.Simulate(dt)

                for a in scene.Actors do
                    if a.Position.XY.Abs().AnyGreater 20.0 then
                        delete.Add a


            scene
        )

    let turn = 
        let sw = System.Diagnostics.Stopwatch.StartNew()
        win.Time |> AVal.map (fun _ ->
            Trafo3d.RotationZ(sw.Elapsed.TotalSeconds * 0.05)
        )

    let vp =
        (turn, win.View) ||> AVal.map2 (fun t v -> t * v.[0])

    let sw = System.Diagnostics.Stopwatch.StartNew()
    let now() = sw.Elapsed.TotalSeconds

    win.Keyboard.DownWithRepeats.Values.Add (fun k ->
        match k with
        | Keys.Space -> 
            run <- not run
        | Keys.Enter | Keys.Return ->
            for x in -4 .. 4 do
                for y in -4 .. 4 do
                    let p = V3d(float x, float y, 5.0)
                    let r = rand.UniformV3dDirection()
                    let a = rand.UniformDouble() * Constant.PiTimesTwo
                    
                    let box = 
                        scene.AddDynamic {
                            Geometry = Box V3d.Half
                            Pose = Euclidean3d.Translation(p) * Euclidean3d.Rotation(r, a)
                            Density = 1.0
                            Material = mat
                            Velocity = V3d.Zero
                            AngularVelocity = V3d.Zero
                        }
                    transact (fun () -> lock boxes (fun () -> boxes.Add box |> ignore))

        | Keys.Escape-> 
            for a in boxes do a.Dispose()
            let newBox = scene.AddDynamic (randomBox())
            transact (fun () ->
                lock boxes (fun () -> boxes.Value <- HashSet.single newBox)
            )

        | Keys.Q -> 
            let view = AVal.force vp


            for x in -5 .. 5 do
                for y in -5 .. 5 do
                    let d = V2d(float x, float y)
                    if d.Length <= 5.0 then
                        let o = d * 0.03
                        let origin = view.Backward.TransformPos V3d.OOO 

                        let dir = view.Backward.TransformDir (V3d(o, -1.0).Normalized)
                        let origin = origin + dir * 3.0

                        let bullet =
                            scene.AddDynamic {
                                Geometry = Sphere 0.05
                                Density = 1000.0
                                Velocity = dir * 10.0
                                Pose = Euclidean3d.Translation(origin)
                                Material = mat
                                AngularVelocity = V3d.Zero
                            }
                        transact (fun () ->
                            lock boxes (fun () -> spheres.[bullet] <- now())
                        )
            // for i in 1 .. 20 do
            //     let origin = view.Backward.TransformPos V3d.OOO 
            //     let phi = rand.UniformDouble() * Constant.PiTimesTwo
            //     let theta = (rand.UniformDouble() * 2.0 - 1.0) * Constant.RadiansPerDegree * 5.0




            //     printfn "%A" (V3d(sin theta * cos phi, sin theta * sin phi, -cos theta))
            //     let dir = view.Backward.TransformDir (V3d(sin theta * cos phi, sin theta * sin phi, -cos theta))
            //     let origin = origin + dir * 10.0

            //     let bullet =
            //         scene.AddDynamic {
            //             Geometry = Sphere 0.1
            //             Density = 1000.0
            //             Velocity = V3d.Zero //dir * 100.0
            //             Pose = Euclidean3d.Translation(origin)
            //             Material = mat
            //             AngularVelocity = V3d.Zero
            //         }
            //     transact (fun () ->
            //         lock boxes (fun () -> spheres.[bullet] <- now())
            //     )

        | _ -> ()
    )

    let boxTrafos = 
        let actors = ASet.toAVal boxes
        AVal.custom (fun t ->
            let sim = sim.GetValue t 
            let a = actors.GetValue t
            a |> HashSet.toArray |> Array.map (fun m -> m.Pose |> Euclidean3d.op_Explicit : Trafo3d)
        
        )

    let sphereTrafos = 
        let actors = AMap.toAVal spheres
        AVal.custom (fun t ->
            let sim = sim.GetValue t 
            let a = actors.GetValue t
            a |> HashMap.toKeyArray |> Array.map (fun m -> m.Pose |> Euclidean3d.op_Explicit : Trafo3d)
        )

    let particleTrafos = 
        scene.ReadParticleProperties()
        AVal.custom (fun t ->
            let sim = sim.GetValue t 
            scene.particlePositions |> Array.map (fun m -> m.XYZ |> Shift3f |> Trafo3f |> Trafo3d)
        )

    win.Scene <-
        Sg.ofList [
            Sg.box' C4b.White (Box3d.FromCenterAndSize(V3d.Zero, V3d.Half))
            |> Sg.instanced boxTrafos
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }
            
            Sg.sphere' 5 C4b.Red 0.05
            |> Sg.instanced sphereTrafos
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
            
            Sg.sphere' 5 C4b.Blue 0.01
            |> Sg.instanced particleTrafos
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }
        ]
        |> Sg.trafo turn


    win.Run()

    //plane.Dispose()

    scene.Dispose()
    0
    

