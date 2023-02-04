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
    std::cout << "my stuff 0";
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

DllExport(PxMaterial*) pxCreateMaterial(PxHandle* handle, float staticFriction, float dynamicFriction, float restitution) {
    auto a = handle->Physics->createMaterial(staticFriction, dynamicFriction, restitution);
    return a;
}

DllExport(void) pxDestroyMaterial(PxMaterial* mat) {
    mat->release();
}

DllExport(PxGeometry*) pxCreateBoxGeometry(PxHandle* handle, V3d size) {
    
    return new PxBoxGeometry((float)size.X/2.0, (float)size.Y/2.0, (float)size.Z/2.0);
}

DllExport(PxGeometry*) pxCreateSphereGeometry(PxHandle* handle, float radius) {
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
    PxTransform pose(PxVec3((float)trafo.Trans.X, (float)trafo.Trans.Y, (float)trafo.Trans.Z), PxQuat((float)trafo.Rot.X, (float)trafo.Rot.Y, (float)trafo.Rot.Z, (float)trafo.Rot.W));
    return PxCreateStatic(*scene->Physics, pose, *geometry, *mat);
}

DllExport(PxRigidDynamic*) pxCreateDynamicComposite(PxSceneHandle* scene, float density, Euclidean3d trafo, int count, PxShapeDescription* shapes) {
    PxTransform pose(PxVec3((float)trafo.Trans.X, (float)trafo.Trans.Y, (float)trafo.Trans.Z), PxQuat((float)trafo.Rot.X, (float)trafo.Rot.Y, (float)trafo.Rot.Z, (float)trafo.Rot.W));
    auto thing = scene->Physics->createRigidDynamic(pose);

    for(int i = 0; i < count; i++) {
        auto d = &shapes[i];
        PxTransform pose(PxVec3((float)d->Pose.Trans.X, (float)d->Pose.Trans.Y, (float)d->Pose.Trans.Z), PxQuat((float)d->Pose.Rot.X, (float)d->Pose.Rot.Y, (float)d->Pose.Rot.Z, (float)d->Pose.Rot.W));
        auto shape = scene->Physics->createShape(*d->Geometry, *d->Material);
        shape->setLocalPose(pose);
        
        thing->attachShape(*shape);
    
    }
    PxRigidBodyExt::updateMassAndInertia(*thing, density);
    return thing;
}

DllExport(PxRigidDynamic*) pxCreateDynamic(PxSceneHandle* scene, PxMaterial* mat, float density, Euclidean3d trafo, PxGeometry* geometry) {
    PxTransform pose(PxVec3((float)trafo.Trans.X, (float)trafo.Trans.Y, (float)trafo.Trans.Z), PxQuat((float)trafo.Rot.X, (float)trafo.Rot.Y, (float)trafo.Rot.Z, (float)trafo.Rot.W));
    return PxCreateDynamic(*scene->Physics, pose, *geometry, *mat, density);
}

DllExport(void) pxSetLinearVelocity(PxRigidDynamic* actor, V3d vel) {
    actor->setLinearVelocity(PxVec3((float)vel.X, (float)vel.Y, (float)vel.Z));
}

DllExport(void) pxSetAngularVelocity(PxRigidDynamic* actor, V3d vel) {
    actor->setAngularVelocity(PxVec3((float)vel.X, (float)vel.Y, (float)vel.Z));
}

DllExport(void) pxSetDensity(PxRigidDynamic* actor, float density) {
    PxRigidBodyExt::updateMassAndInertia(*actor, density);
}


DllExport(V3d) pxGetLinearVelocity(PxRigidDynamic* actor, V3d vel) {
    auto v = actor->getLinearVelocity();
    return {v.x, v.y, v.z};
}

DllExport(V3d) pxGetAngularVelocity(PxRigidDynamic* actor, V3d vel) {
    auto v = actor->getAngularVelocity();
    return {v.x, v.y, v.z};
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
    PxTransform pose = PxTransformFromPlaneEquation(PxPlane((float)coeff.X, (float)coeff.Y, (float)coeff.Z, (float)coeff.W));
    PxRigidStatic* plane = scene->Physics->createRigidStatic(pose);

    auto planeShape = scene->Physics->createShape(PxPlaneGeometry(), *mat);
    planeShape->setName("floor");
    plane->attachShape(*planeShape);
    scene->Scene->addActor(*plane);
    return plane;
} 


DllExport(void) pxSimulate(PxSceneHandle* scene, float dt) {
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


DllExport(PxSceneHandle*) pxCreateScene(PxHandle* handle, V3d gravity) {
    PxSceneDesc sceneDesc(handle->Physics->getTolerancesScale());
    sceneDesc.gravity = PxVec3((float)gravity.X, (float)gravity.Y, (float)gravity.Z);

    PxCudaContextManagerDesc cudaContextManagerDesc;
    //auto gCudaContextManager = PxCreateCudaContextManager(*gFoundation, cudaContextManagerDesc, PxGetProfilerCallback());
    auto gCudaContextManager = PxCreateCudaContextManager(*handle->Foundation, cudaContextManagerDesc, PxGetProfilerCallback());
    sceneDesc.cudaContextManager = gCudaContextManager;

    sceneDesc.flags |= PxSceneFlag::eENABLE_GPU_DYNAMICS;
    sceneDesc.broadPhaseType = PxBroadPhaseType::eGPU;

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

int main(int argc, char** argv) {
    return 0;
}