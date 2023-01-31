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

[<Struct; StructLayout(LayoutKind.Sequential)>]
type PhysXGeometryHandle = 
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
    extern void pxGetPose(PhysxActorHandle thing, Euclidean3d& trafo)

    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreateBoxGeometry(PhysXHandle handle, V3d size)

    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreateSphereGeometry(PhysXHandle handle, double radius)
    
    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreatePlaneGeometry(PhysXHandle handle)

    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreateTriangleGeometry(PhysXHandle handle, int fvc, int[] indices, int vc, V3f[] vertices)
    
    [<DllImport("PhysXNative")>]
    extern void pxDestroyGeometry(PhysXGeometryHandle actor)

    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxCreateStatic(PhysXSceneHandle scene, PhysXMaterialHandle mat, Euclidean3d trafo, PhysXGeometryHandle geometry)

    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxCreateDynamic(PhysXSceneHandle scene, double density, PhysXMaterialHandle mat, Euclidean3d trafo, PhysXGeometryHandle geometry)

    [<DllImport("PhysXNative")>]
    extern void pxAddActor(PhysXSceneHandle scene, PhysxActorHandle actor)
    
    [<DllImport("PhysXNative")>]
    extern void pxRemoveActor(PhysXSceneHandle scene, PhysxActorHandle actor)

    [<DllImport("PhysXNative")>]
    extern void pxDestroyActor(PhysxActorHandle actor)

    [<DllImport("PhysXNative")>]
    extern void pxSetActorVelocity(PhysxActorHandle actor, V3d v)

type Material =
    {
        StaticFriction : float
        DynamicFriction : float
        Restitution : float
    }

type Geometry =
    | Box of size : V3d
    | Sphere of radius : double
    | Plane of Plane3d

type PhysXActorDescription =
    {
        Geometry    : Geometry
        Pose        : Euclidean3d
        Material    : Material
        Density     : float
        Velocity    : V3d
    }

type PhysXScene(gravity : V3d) =
    static let physxInstance = lazy (PhysX.pxInit())

    let physx = physxInstance.Value
    let handle = PhysX.pxCreateScene(physx, gravity)
    let matCache = Dict<Material, PhysXMaterialHandle>()

    member x.Handle = handle

    member x.AddStatic(desc : PhysXActorDescription) =
        let hMat = matCache.GetOrCreate(desc.Material, fun m -> PhysX.pxCreateMaterial(physx, m.StaticFriction, m.DynamicFriction, m.Restitution))

        let hGeom = 
            match desc.Geometry with
            | Box size -> PhysX.pxCreateBoxGeometry(physx, size)
            | Sphere radius -> PhysX.pxCreateSphereGeometry(physx, radius)
            | Plane _ -> PhysX.pxCreatePlaneGeometry(physx)

        let trafo =
            match desc.Geometry with
            | Plane p -> desc.Pose * Euclidean3d.Translation(p.Point) * Euclidean3d.RotateInto(V3d.IOO, p.Normal)
            | _ -> desc.Pose

        let actor =
            PhysX.pxCreateStatic(handle, hMat, trafo, hGeom)

        PhysX.pxAddActor(handle, actor)
        new PhysXActor(x, desc, actor, hGeom)

    member x.AddDynamic(desc : PhysXActorDescription) =
        let hMat = matCache.GetOrCreate(desc.Material, fun m -> PhysX.pxCreateMaterial(physx, m.StaticFriction, m.DynamicFriction, m.Restitution))

        let hGeom = 
            match desc.Geometry with
            | Box size -> PhysX.pxCreateBoxGeometry(physx, size)
            | Sphere radius -> PhysX.pxCreateSphereGeometry(physx, radius)
            | Plane _ -> PhysX.pxCreatePlaneGeometry(physx)

        let trafo =
            match desc.Geometry with
            | Plane p -> desc.Pose * Euclidean3d.Translation(p.Point) * Euclidean3d.RotateInto(V3d.IOO, p.Normal)
            | _ -> desc.Pose

        let actor =
            PhysX.pxCreateDynamic(handle, desc.Density, hMat, trafo, hGeom)

        PhysX.pxAddActor(handle, actor)

        if desc.Velocity <> V3d.Zero then
            PhysX.pxSetActorVelocity(actor, desc.Velocity)

        new PhysXActor(x, desc, actor, hGeom)

    member x.Simulate(dt : float) =
        PhysX.pxSimulate(handle, dt)

    member x.Dispose() =
        PhysX.pxDestroyScene(handle)

    interface System.IDisposable with
        member x.Dispose() = x.Dispose()

and PhysXActor(parent : PhysXScene, desc : PhysXActorDescription, handle : PhysxActorHandle, geometry : PhysXGeometryHandle) =
    member x.Geometry = desc.Geometry

    member x.Pose = 
        let mutable pose = Euclidean3d.Identity
        PhysX.pxGetPose(handle, &pose)
        pose

    member x.Dispose() =
        PhysX.pxRemoveActor(parent.Handle, handle)
        PhysX.pxDestroyActor handle
        PhysX.pxDestroyGeometry geometry

    interface System.IDisposable with
        member x.Dispose() = x.Dispose()

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
            Density = 0.0
            Material = mat
            Velocity = V3d.Zero
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
        }



    let box = scene.AddDynamic(randomBox())




    let boxes = cset [box]
    let spheres = cset []

    
    let mutable run = false

    let win = 
        window {
            backend Backend.GL
        }

    let sim =
        let sw = System.Diagnostics.Stopwatch()
        win.Time |> AVal.map (fun _ ->
            let dt = sw.Elapsed.TotalSeconds
            sw.Restart()
            if run && dt > 0.0 then 
                scene.Simulate(dt)
            scene
        )

    let turn = 
        let sw = System.Diagnostics.Stopwatch.StartNew()
        win.Time |> AVal.map (fun _ ->
            Trafo3d.RotationZ(sw.Elapsed.TotalSeconds * 0.05)
        )

    let vp =
        (turn, win.View) ||> AVal.map2 (fun t v -> t * v.[0])

    win.Keyboard.DownWithRepeats.Values.Add (fun k ->
        match k with
        | Keys.Space -> 
            run <- not run
        | Keys.Enter | Keys.Return -> 
            let box = scene.AddDynamic (randomBox())
            transact (fun () -> boxes.Add box |> ignore)


        | Keys.Escape-> 
            for a in boxes do a.Dispose()
            let newBox = scene.AddDynamic (randomBox())
            transact (fun () ->
                boxes.Value <- HashSet.single newBox
            )

        | Keys.X -> 
            let view = AVal.force vp
            let origin = view.Backward.TransformPos V3d.OOO
            let dir = view.Backward.TransformDir V3d.OON

            let bullet =
                scene.AddDynamic {
                    Geometry = Sphere 0.25
                    Density = 50.0
                    Velocity = dir * 40.0
                    Pose = Euclidean3d.Translation(origin)
                    Material = mat
                }
            transact (fun () ->
                spheres.Add bullet |> ignore
            )

            ()

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
        let actors = ASet.toAVal spheres
        AVal.custom (fun t ->
            let sim = sim.GetValue t 
            let a = actors.GetValue t
            a |> HashSet.toArray |> Array.map (fun m -> m.Pose |> Euclidean3d.op_Explicit : Trafo3d)
        
        )
    win.Scene <-
        Sg.ofList [
            Sg.box' C4b.White (Box3d.FromCenterAndSize(V3d.Zero, V3d.Half))
            |> Sg.instanced boxTrafos
            |> Sg.shader {
                do! DefaultSurfaces.trafo
                do! DefaultSurfaces.simpleLighting
            }
            
            Sg.sphere' 5 C4b.Red 0.25
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

        ]
        |> Sg.trafo turn


    win.Run()

    //plane.Dispose()

    scene.Dispose()
    0
    

