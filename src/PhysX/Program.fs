open System.Runtime.InteropServices
open Aardvark.Base
open FSharp.Data.Adaptive
open Aardvark.Application
open Aardvark.SceneGraph
open Aardvark.Rendering


[<Struct; StructLayout(LayoutKind.Sequential)>]
type PhysXHandle =
    val mutable public Handle : nativeint

[<Struct; StructLayout(LayoutKind.Sequential)>]
type PhysXSceneHandle = 
    val mutable public Handle : nativeint

[<Struct; StructLayout(LayoutKind.Sequential)>]
type PhysXMaterialHandle = 
    val mutable public Handle : nativeint

[<Struct; StructLayout(LayoutKind.Sequential)>]
type PhysxActorHandle = 
    val mutable public Handle : nativeint

module PhysX =
    [<DllImport("PhysXNative")>]
    extern PhysXHandle pxInit()
    
    [<DllImport("PhysXNative")>]
    extern void pxDestroy(PhysXHandle handle)
    
    [<DllImport("PhysXNative")>]
    extern PhysXSceneHandle pxCreateScene(PhysXHandle handle, V3d gravity) 

    [<DllImport("PhysXNative")>]
    extern void pxDestroyScene(PhysXSceneHandle scene)

    [<DllImport("PhysXNative")>]
    extern PhysXMaterialHandle pxCreateMaterial(PhysXHandle handle, float staticFriction, float dynamicFriction, float restitution)

    [<DllImport("PhysXNative")>]
    extern void pxDestroyMaterial(PhysXMaterialHandle material)
    
    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxAddCube(PhysXSceneHandle scene, V3d size, V3d position, V4d quat, PhysXMaterialHandle mat, double density)

    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxAddStaticPlane(PhysXSceneHandle scene, V4d coeff, PhysXMaterialHandle mat)

    [<DllImport("PhysXNative")>]
    extern void pxSimulate(PhysXSceneHandle scene, double dt)

    [<DllImport("PhysXNative")>]
    extern void pxGetPose(PhysxActorHandle thing, V3d& position, V4d& quat)


[<EntryPoint>]
let main args =
    Aardvark.Init()
    let h = PhysX.pxInit()
    let s = PhysX.pxCreateScene(h, V3d(0.0, 0.0, -9.81))
    let mat = PhysX.pxCreateMaterial(h, 0.5, 0.5, 0.5)

    let plane = PhysX.pxAddStaticPlane(s, V4d.OOIO, mat)

    let r = Rot3d.Rotation(V3d.III.Normalized, 2.0)

    let box = PhysX.pxAddCube(s, V3d.Half, V3d.OOI* 4.0, V4d(r.X, r.Y, r.Z, r.W), mat, 1.0)

    let position = cval (V3d.OOI* 4.0)
    let rot = cval (V4d(r.X, r.Y, r.Z, r.W))
    let mutable run = false
    let thread =
        startThread <| fun () ->
            let sw = System.Diagnostics.Stopwatch()
            while true do
                let dt = sw.Elapsed.TotalSeconds
                sw.Restart()
                if run then
                    PhysX.pxSimulate(s, dt)

                    let mutable pos = V3d.Zero
                    let mutable quat = V4d.Zero
                    PhysX.pxGetPose(box, &pos, &quat)

                    transact (fun () ->
                        position.Value <- pos
                        rot.Value <- quat
                    )


    let win = 
        window {
            backend Backend.GL
        }

    win.Keyboard.DownWithRepeats.Values.Add (fun k ->
        match k with
        | Keys.Space -> run <- not run
        | _ -> ()
    )

    win.Scene <-
        Sg.box' C4b.White (Box3d.FromCenterAndSize(V3d.Zero, V3d.Half))
        |> Sg.trafo (
            (position, rot) ||> AVal.map2 (fun p r -> 
                let r : Trafo3d = Rot3d(r.W, r.X, r.Y, r.Z) |> Rot3d.op_Explicit
                r * Trafo3d.Translation(p)
            )
        )
        |> Sg.shader {
            do! DefaultSurfaces.trafo
            do! DefaultSurfaces.simpleLighting
        }


    win.Run()


    PhysX.pxDestroyScene s
    PhysX.pxDestroy h
    0
    

