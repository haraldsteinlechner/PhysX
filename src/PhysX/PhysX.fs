namespace Aardvark.PhysX

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
type PhysXPbdHandle = 
    val mutable public Handle : nativeint

[<Struct; StructLayout(LayoutKind.Sequential)>]
type PhysXPbdParticleBuffer = 
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

//[<Struct; StructLayout(LayoutKind.Sequential)>]
//type PhysXParticleInfo = 
//    val mutable public posInvMass : V4f[]
//    val mutable public valocity : V4f[]
//    val mutable public phases : uint32[]

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
    
    [<DllImport("PhysXNative")>]
    extern PhysXPbdHandle pxCreatePBD(PhysXSceneHandle sceneHandle, uint32 maxParticles)
    
    [<DllImport("PhysXNative")>]
    extern PhysXPbdParticleBuffer pxCreateParticleBuffer(PhysXPbdHandle handle, single centerX, single centerY, single centerZ, uint32 numParticlesDim)
    
    [<DllImport("PhysXNative")>]
    extern void pxGetParticleProperties(
        PhysXPbdHandle handle, V4f* positionsHost, V4f* velsHost, uint32[] phasesHost)
    
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
    let sceneHandle = PhysX.pxCreateScene(physx, gravity)
    let maxParticles = 10000u
    let pbdHandle = PhysX.pxCreatePBD(sceneHandle, maxParticles)
    let fluidParticles = PhysX.pxCreateParticleBuffer(pbdHandle, 0.0f, 0.0f, 1.0f, 10u)
    let matCache = Dict<Material, PhysXMaterialHandle>()
    let actors = System.Collections.Generic.HashSet<PhysXActor>()
    
    let positionsBuffer : V4f array = maxParticles |> int |> Array.zeroCreate
    let velsBuffer : V4f array = maxParticles |> int |> Array.zeroCreate
    let phasesBuffer : uint32 array = maxParticles |> int |> Array.zeroCreate
    
    member x.Handle = sceneHandle
    member x.PbdHandle = pbdHandle
    member internal x.ActorSet = actors
    member x.particlePositions = positionsBuffer
    member x.particleVels = velsBuffer
    member x.particlePhases = phasesBuffer

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
                PhysX.pxCreateStatic(sceneHandle, hMat, trafo, hGeom)

            PhysX.pxAddActor(sceneHandle, actor)
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
                PhysX.pxCreateDynamic(sceneHandle, hMat, float32 desc.Density, trafo, hGeom)

            PhysX.pxAddActor(sceneHandle, actor)

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
            PhysX.pxSimulate(sceneHandle, float32 dt)
        )

    member x.ReadParticleProperties() =
        positionsBuffer.SetValue(V4f(42.0f, 43.0f, 44.0f, 45.0f), 0)
        velsBuffer.SetValue(V4f(22.0f, 43.0f, 44.0f, 45.0f), 0)
        phasesBuffer.SetValue(uint32(48), 1)
        use positionsBufferFixed = fixed positionsBuffer
        use velsBufferFixed = fixed velsBuffer
        PhysX.pxGetParticleProperties(pbdHandle, positionsBufferFixed, velsBufferFixed, phasesBuffer)

    member x.Dispose() =
        lock actors (fun () ->
            PhysX.pxDestroyScene(sceneHandle)
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