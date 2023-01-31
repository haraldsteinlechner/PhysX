// MiniCVNative.cpp : Defines the exported functions for the DLL application.
//

#include "PhysXNative.h"
#include <string>
#include <iostream>

#include <PxPhysicsAPI.h>
#include <extensions/PxExtensionsAPI.h>
#include <extensions/PxDefaultErrorCallback.h>
#include <extensions/PxDefaultAllocator.h> 
#include <extensions/PxDefaultSimulationFilterShader.h>
#include <extensions/PxDefaultCpuDispatcher.h>
#include <extensions/PxShapeExt.h>
#include <foundation/PxMat33.h> 
#include <extensions/PxSimpleFactory.h>
#include <extensions/PxTriangleMeshExt.h>

using namespace physx;

static PxDefaultErrorCallback gDefaultErrorCallback;
static PxDefaultAllocator gDefaultAllocatorCallback;
static PxSimulationFilterShader gDefaultFilterShader = PxDefaultSimulationFilterShader;

DllExport(PxHandle*) pxInit() {
    auto thing = PxCreateFoundation(PX_PHYSICS_VERSION, gDefaultAllocatorCallback, gDefaultErrorCallback);
    if(!thing) return nullptr;

    auto physics = PxCreatePhysics(PX_PHYSICS_VERSION, *thing, PxTolerancesScale());
    if(!physics) return nullptr;

    // auto cooking = PxCreateCooking(PX_PHYSICS_VERSION, *thing, PxCookingParams(PxTolerancesScale()));
    // if(!cooking) return nullptr;

    auto handle = new PxHandle();
    handle->Foundation = thing;
    handle->Physics = physics;
    //handle->Cooking = cooking;
    return handle;
}

DllExport(void) pxDestroy(PxHandle* handle) {
    handle->Physics->release();
    handle->Foundation->release();
    delete handle;
}

DllExport(PxMaterial*) pxCreateMaterial(PxHandle* handle, double staticFriction, double dynamicFriction, double restitution) {
    return handle->Physics->createMaterial(staticFriction, dynamicFriction, restitution);
}

DllExport(void) pxDestroyMaterial(PxMaterial* mat) {
    mat->release();
}

DllExport(void*) pxAddCube(PxSceneHandle* scene, V3d size, V3d position, V4d quaternion, PxMaterial* mat, double density) {
    PxTransform transform(PxVec3(position.X, position.Y, position.Z), PxQuat(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W));
    PxBoxGeometry geometry(size.X, size.Y, size.Z);
    PxRigidDynamic* actor = PxCreateDynamic(*scene->Physics, transform, geometry, *mat, density);
    if(actor) {
        actor->setAngularDamping(0.75);
        scene->Scene->addActor(*actor);
    }
    return actor;
} 

DllExport(PxGeometry*) pxCreateBoxGeometry(PxHandle* handle, V3d size) {
    return new PxBoxGeometry(size.X/2.0, size.Y/2.0, size.Z/2.0);
}

DllExport(PxGeometry*) pxCreateSphereGeometry(PxHandle* handle, double radius) {
    return new PxSphereGeometry(radius);
}

DllExport(PxGeometry*) pxCreatePlaneGeometry(PxHandle* handle) {
    return new PxPlaneGeometry();
}

DllExport(PxGeometry*) pxCreateTriangleGeometry(PxHandle* handle, int fvc, const int* indices, int vc, V3f* vertices) {
//     PxTriangleMeshDesc desc;
//     desc.points.count = vc;
//     desc.points.stride = sizeof(V3f);
//     desc.points.data = vertices;

//     desc.triangles.count = fvc;
//     desc.triangles.stride = 3*sizeof(PxU32);
//     desc.triangles.data = indices;

//     PxDefaultMemoryOutputStream writeBuffer;
//     PxTriangleMeshCookingResult::Enum result;

//     bool status = handle->Cooking->cookTriangleMesh(desc, writeBuffer, &result);
//     if(!status) return NULL;

//     PxDefaultMemoryInputData readBuffer(writeBuffer.getData(), writeBuffer.getSize());
//     auto mesh = handle->Physics->createTriangleMesh(readBuffer);
//     return new PxTriangleMeshGeometry(mesh);
    return nullptr;
}


DllExport(PxRigidStatic*) pxCreateStatic(PxSceneHandle* scene, PxMaterial* mat, Euclidean3d trafo, PxGeometry* geometry) {
    PxTransform pose(PxVec3(trafo.Trans.X, trafo.Trans.Y, trafo.Trans.Z), PxQuat(trafo.Rot.X, trafo.Rot.Y, trafo.Rot.Z, trafo.Rot.W));
    return PxCreateStatic(*scene->Physics, pose, *geometry, *mat);
}

DllExport(PxRigidDynamic*) pxCreateDynamic(PxSceneHandle* scene, PxMaterial* mat, double density, Euclidean3d trafo, PxGeometry* geometry) {
    PxTransform pose(PxVec3(trafo.Trans.X, trafo.Trans.Y, trafo.Trans.Z), PxQuat(trafo.Rot.X, trafo.Rot.Y, trafo.Rot.Z, trafo.Rot.W));
    return PxCreateDynamic(*scene->Physics, pose, *geometry, *mat, density);
}

DllExport(void) pxAddActor(PxSceneHandle* scene, PxRigidActor* actor) {
    scene->Scene->addActor(*actor);
}

DllExport(void) pxRemoveActor(PxSceneHandle* scene, PxRigidActor* actor) {
    scene->Scene->removeActor(*actor);
}


DllExport(void) pxDestroyActor(PxRigidActor* actor) {
    actor->release();
}

DllExport(void) pxDestroyGeometry(PxGeometry* geometry) {
    delete geometry;
}


DllExport(void*) pxAddStaticPlane(PxSceneHandle* scene, V4d coeff, PxMaterial* mat) {
    PxTransform pose = PxTransformFromPlaneEquation(PxPlane(coeff.X, coeff.Y, coeff.Z, coeff.W));
    PxRigidStatic* plane = scene->Physics->createRigidStatic(pose);

    auto planeShape = scene->Physics->createShape(PxPlaneGeometry(), *mat);
    planeShape->setName("floor");
    plane->attachShape(*planeShape);
    scene->Scene->addActor(*plane);
    return plane;
} 


DllExport(void*) pxSimulate(PxSceneHandle* scene, double dt) {
    if(dt > 0.0) {
        scene->Scene->simulate(dt);
        scene->Scene->fetchResults(true);
    }
}

DllExport(void) pxGetPose(PxRigidActor* actor, Euclidean3d& trafo) {
    auto pose = actor->getGlobalPose();
    trafo.Trans.X = pose.p.x;
    trafo.Trans.Y = pose.p.y;
    trafo.Trans.Z = pose.p.z;
    trafo.Rot.X = pose.q.x;
    trafo.Rot.Y = pose.q.y;
    trafo.Rot.Z = pose.q.z;
    trafo.Rot.W = pose.q.w;
}

DllExport(void) pxSetActorVelocity(PxRigidBody* actor, V3d v) {
    actor->setLinearVelocity(PxVec3(v.X, v.Y, v.Z));
}


DllExport(PxSceneHandle*) pxCreateScene(PxHandle* handle, V3d gravity) {
    PxSceneDesc sceneDesc(handle->Physics->getTolerancesScale());
    sceneDesc.gravity = PxVec3(gravity.X, gravity.Y, gravity.Z);
    
    if(!sceneDesc.cpuDispatcher) {
        PxDefaultCpuDispatcher* mCpuDispatcher = PxDefaultCpuDispatcherCreate(1);
        if(!mCpuDispatcher) return nullptr;
        sceneDesc.cpuDispatcher = mCpuDispatcher;
    }
    if(!sceneDesc.filterShader)
        sceneDesc.filterShader = gDefaultFilterShader;

    auto scene = handle->Physics->createScene(sceneDesc);

    scene->setVisualizationParameter(PxVisualizationParameter::eSCALE, 1.0);
    scene->setVisualizationParameter(PxVisualizationParameter::eCOLLISION_SHAPES, 1.0f);

    auto sceneHandle = new PxSceneHandle();
    sceneHandle->Foundation = handle->Foundation;
    sceneHandle->Physics = handle->Physics;
    sceneHandle->Scene = scene;
    sceneHandle->Cooking = handle->Cooking;
    return sceneHandle;
}

DllExport(void) pxDestroyScene(PxSceneHandle* handle) {
    handle->Scene->release();
    delete handle;
}

DllExport(int) pxTest() {

    auto thing = PxCreateFoundation(PX_PHYSICS_VERSION, gDefaultAllocatorCallback, gDefaultErrorCallback);
    auto physics = PxCreatePhysics(PX_PHYSICS_VERSION, *thing, PxTolerancesScale());
    if(!physics) {
        return -1;
    }

    if(!PxInitExtensions(*physics, nullptr)) {
        return -1;
    }

    PxSceneDesc sceneDesc(physics->getTolerancesScale());
    sceneDesc.gravity = PxVec3(0.0f, 0.0f, -9.81f);
    
    if(!sceneDesc.cpuDispatcher) {
        PxDefaultCpuDispatcher* mCpuDispatcher = PxDefaultCpuDispatcherCreate(1);
        if(!mCpuDispatcher) return -1;
        sceneDesc.cpuDispatcher = mCpuDispatcher;
    }
    if(!sceneDesc.filterShader)
        sceneDesc.filterShader = gDefaultFilterShader;


    auto scene = physics->createScene(sceneDesc);
    scene->setVisualizationParameter(PxVisualizationParameter::eSCALE, 1.0);
    scene->setVisualizationParameter(PxVisualizationParameter::eCOLLISION_SHAPES, 1.0f);

    auto mat = physics->createMaterial(0.5f, 0.5f, 0.6f);

    
    //1) Create ground plane
    PxTransform pose = PxTransformFromPlaneEquation(PxPlane(0, 0, 1, 0));
    PxRigidStatic* plane = physics->createRigidStatic(pose);
    if(!plane) return -1;

    auto planeShape = physics->createShape(PxPlaneGeometry(), *mat);
    planeShape->setName("floor");
    plane->attachShape(*planeShape);
    scene->addActor(*plane);

    PxVec3 boxSize(0.5, 0.5, 0.5);
    PxBoxGeometry boxGeometry(boxSize);

    PxTransform transform(PxVec3(0, 0, 5));
    auto box = PxCreateDynamic(*physics, transform, boxGeometry, *mat, 1.0);
    box->setLinearVelocity(PxVec3(0, 0, 0.0f));
    box->setAngularDamping(0.75);

    scene->addActor(*box);


    auto p0 = box->getGlobalPose();
    printf("%.2f %.2f %.2f\n", p0.p.x, p0.p.y, p0.p.z);

    for (int i = 0; i < 1000; i++) {
        scene->simulate(1.0f/60.0f);
        scene->fetchResults(true);

        auto pose = box->getGlobalPose();
        printf("%.2f %.2f %.2f\n", pose.p.x, pose.p.y, pose.p.z);

    }


    scene->release();
    physics->release();

    return 0;

}