namespace ParticlePlaygroundVR

open System

open Aardvark.Base
open Aardvark.Vr
open Aardvark.UI.Primitives

open FSharp.Data.Adaptive
open Adaptify

open Aardvark.PhysX

[<ModelType>]
type Model =
    {
        text    : string
        vr      : bool

        scene : PhysXScene

        boxes : HashSet<PhysXActor>
        spheres : HashMap<PhysXActor, float>

        lastSimulation : Option<DateTime>

        cameraController : CameraControllerState

        version : int
    }
