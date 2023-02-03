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
    extern PhysXMaterialHandle pxCreateMaterial(PhysXHandle handle, float32 staticFriction, float32 dynamicFriction, float32 restitution)

    [<DllImport("PhysXNative")>]
    extern void pxDestroyMaterial(PhysXMaterialHandle material)
    
    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxAddCube(PhysXSceneHandle scene, V3d size, V3d position, V4d quat, PhysXMaterialHandle mat, float32 density)

    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxAddStaticPlane(PhysXSceneHandle scene, V4d coeff, PhysXMaterialHandle mat)

    [<DllImport("PhysXNative")>]
    extern void pxSimulate(PhysXSceneHandle scene, float32 dt)

    [<DllImport("PhysXNative")>]
    extern void pxGetPose(PhysxActorHandle thing, Euclidean3d& trafo)

    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreateBoxGeometry(PhysXHandle handle, V3d size)

    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreateSphereGeometry(PhysXHandle handle, float32 radius)
    
    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreatePlaneGeometry(PhysXHandle handle)

    [<DllImport("PhysXNative")>]
    extern PhysXGeometryHandle pxCreateTriangleGeometry(PhysXHandle handle, int fvc, int[] indices, int vc, V3f[] vertices)
    
    [<DllImport("PhysXNative")>]
    extern void pxDestroyGeometry(PhysXGeometryHandle actor)

    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxCreateStatic(PhysXSceneHandle scene, PhysXMaterialHandle mat, Euclidean3d trafo, PhysXGeometryHandle geometry)

    [<DllImport("PhysXNative")>]
    extern PhysxActorHandle pxCreateDynamic(PhysXSceneHandle scene, PhysXMaterialHandle mat, float32 density, Euclidean3d trafo, PhysXGeometryHandle geometry)

    [<DllImport("PhysXNative")>]
    extern void pxAddActor(PhysXSceneHandle scene, PhysxActorHandle actor)
    
    [<DllImport("PhysXNative")>]
    extern void pxRemoveActor(PhysXSceneHandle scene, PhysxActorHandle actor)

    [<DllImport("PhysXNative")>]
    extern void pxDestroyActor(PhysxActorHandle actor)

    [<DllImport("PhysXNative")>]
    extern void pxSetLinearVelocity(PhysxActorHandle actor, V3d v)
    
    [<DllImport("PhysXNative")>]
    extern void pxSetAngularVelocity(PhysxActorHandle actor, V3d v)

    [<DllImport("PhysXNative")>]
    extern V3d pxGetLinearVelocity(PhysxActorHandle actor)
    
    [<DllImport("PhysXNative")>]
    extern V3d pxGetAngularVelocity(PhysxActorHandle actor)

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

type Shape =
    {
        Geometry : list<Euclidean3d * Geometry>
        Material : Material
        Density : float
        AngularDamping : float
    }

type PhysXDynamicActorDescription =
    {
        Geometry        : Geometry
        Pose            : Euclidean3d
        Material        : Material
        Density         : float
        Velocity        : V3d
        AngularVelocity : V3d
    }

type PhysXStaticActorDescription =
    {
        Geometry        : Geometry
        Pose            : Euclidean3d
        Material        : Material
    }

type PhysXScene(gravity : V3d) =
    static let physxInstance = lazy (PhysX.pxInit())

    let physx = physxInstance.Value
    let handle = PhysX.pxCreateScene(physx, gravity)
    let matCache = Dict<Material, PhysXMaterialHandle>()
    let actors = System.Collections.Generic.HashSet<PhysXActor>()

    member x.Handle = handle
    member internal x.ActorSet = actors

    member x.Actors = actors :> seq<_>

    member x.AddStatic(desc : PhysXStaticActorDescription) =
        lock actors (fun () ->
            let hMat = matCache.GetOrCreate(desc.Material, fun m -> PhysX.pxCreateMaterial(physx, float32 m.StaticFriction,float32 m.DynamicFriction, float32 m.Restitution))

            let hGeom = 
                match desc.Geometry with
                | Box size -> PhysX.pxCreateBoxGeometry(physx, size)
                | Sphere radius -> PhysX.pxCreateSphereGeometry(physx, float32 radius)
                | Plane _ -> PhysX.pxCreatePlaneGeometry(physx)

            let trafo =
                match desc.Geometry with
                | Plane p -> desc.Pose * Euclidean3d.Translation(p.Point) * Euclidean3d.RotateInto(V3d.IOO, p.Normal)
                | _ -> desc.Pose

            let actor =
                PhysX.pxCreateStatic(handle, hMat, trafo, hGeom)

            PhysX.pxAddActor(handle, actor)
            let res = new PhysXActor(x, false, desc.Geometry, actor, hGeom)
            actors.Add res |> ignore
            res
        )

    member x.AddDynamic(desc : PhysXDynamicActorDescription) =
        lock actors (fun () ->
            let hMat = 
                matCache.GetOrCreate(desc.Material, fun m -> PhysX.pxCreateMaterial(physx, float32 m.StaticFriction, float32 m.DynamicFriction, float32 m.Restitution))

            let hGeom = 
                match desc.Geometry with
                | Box size -> PhysX.pxCreateBoxGeometry(physx, size)
                | Sphere radius -> PhysX.pxCreateSphereGeometry(physx, float32 radius)
                | Plane _ -> PhysX.pxCreatePlaneGeometry(physx)

            let trafo =
                match desc.Geometry with
                | Plane p -> desc.Pose * Euclidean3d.Translation(p.Point) * Euclidean3d.RotateInto(V3d.IOO, p.Normal)
                | _ -> desc.Pose

            let actor =
                PhysX.pxCreateDynamic(handle, hMat, float32 desc.Density, trafo, hGeom)

            PhysX.pxAddActor(handle, actor)

            if desc.Velocity <> V3d.Zero then
                PhysX.pxSetLinearVelocity(actor, desc.Velocity)

            if desc.AngularVelocity <> V3d.Zero then
                PhysX.pxSetAngularVelocity(actor, desc.AngularVelocity)

            let res = new PhysXActor(x, true, desc.Geometry, actor, hGeom)
            actors.Add res |> ignore
            res
        )

    member x.Simulate(dt : float) =
        lock actors (fun () ->
            PhysX.pxSimulate(handle, float32 dt)
        )

    member x.Dispose() =
        lock actors (fun () ->
            PhysX.pxDestroyScene(handle)
        )

    interface System.IDisposable with
        member x.Dispose() = x.Dispose()

and PhysXActor(parent : PhysXScene, isDynamic : bool, geometryDesc : Geometry, handle : PhysxActorHandle, geometry : PhysXGeometryHandle) =
    let mutable isDisposed = 0
    
    member x.Geometry = geometryDesc


    member x.Velocity
        with get() = 
            if isDisposed <> 0 then 
                if isDynamic then PhysX.pxGetLinearVelocity(handle)
                else V3d.Zero
            else
                V3d.Zero
        and set(v) =
            if isDisposed <> 0 && isDynamic then PhysX.pxSetLinearVelocity(handle, v)

    member x.AngularVelocity
        with get() = 
            if isDisposed <> 0 then 
                if isDynamic then PhysX.pxGetAngularVelocity(handle)
                else V3d.Zero
            else 
                V3d.Zero
        and set(v) =
            if isDisposed <> 0 && isDynamic then PhysX.pxSetAngularVelocity(handle, v)

    member x.Pose = 
        if isDisposed <> 0 then 
            Euclidean3d.Identity 
        else
            let mutable pose = Euclidean3d.Identity
            PhysX.pxGetPose(handle, &pose)
            pose

    member x.Position = 
        if isDisposed <> 0 then 
            V3d.PositiveInfinity
        else
            let mutable pose = Euclidean3d.Identity
            PhysX.pxGetPose(handle, &pose)
            pose.Trans

    member x.Dispose() =
        if System.Threading.Interlocked.Exchange(&isDisposed, 1) = 0 then
            lock parent.ActorSet (fun () ->
                PhysX.pxRemoveActor(parent.Handle, handle)
                PhysX.pxDestroyActor handle
                PhysX.pxDestroyGeometry geometry
                parent.ActorSet.Remove x |> ignore
            )

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

        ]
        |> Sg.trafo turn


    win.Run()

    //plane.Dispose()

    scene.Dispose()
    0
    

