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
#include <extensions/PxParticleExt.h>
#include <cudamanager/PxCudaContext.h>
#include <cudamanager/PxCudaContextManager.h>

using namespace physx;
using namespace ExtGpu;

static PxDefaultErrorCallback gDefaultErrorCallback;
static PxDefaultAllocator gDefaultAllocatorCallback;
static PxSimulationFilterShader gDefaultFilterShader = PxDefaultSimulationFilterShader;
static PxParticleInfo gParticleInfo = PxParticleInfo();
static physx::PxU32 gMaxParticles = 0;

DllExport(PxHandle*) pxInit() {
    auto thing = PxCreateFoundation(PX_PHYSICS_VERSION, gDefaultAllocatorCallback, gDefaultErrorCallback);
    std::cout << "my stuff 1";
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
    
    return new PxBoxGeometry((float)size.X/2.0f, (float)size.Y/2.0f, (float)size.Z/2.0f);
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
        scene->Scene->fetchResultsParticleSystem();
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
    auto cudaContextManager = PxCreateCudaContextManager(*handle->Foundation, cudaContextManagerDesc, PxGetProfilerCallback());
    sceneDesc.cudaContextManager = cudaContextManager;

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
    sceneHandle->CudaManager = cudaContextManager;
    return sceneHandle;
}

DllExport(void) pxDestroyScene(PxPbdHandle* handle) {
    //delete handle->ParticleInfo.posInvMass;
    //delete handle->ParticleInfo.velocity;
    //delete handle->ParticleInfo.phase;
    handle->Scene->release();
    delete handle;
}

DllExport(PxPbdHandle*) pxCreatePBD(
        PxSceneHandle* sceneHandle, PxU32 maxParticles, 
        float centerX, float centerY, float centerZ, PxU32 numParticlesDim,
        PxReal particleSpacing = 0.2f, PxReal fluidDensity = 1000.f) {

    PxPBDParticleSystem* particleSystem = sceneHandle->Physics->createPBDParticleSystem(*sceneHandle->CudaManager, 96);

    const PxReal restOffset = 0.5f * particleSpacing / 0.6f;
    const PxReal solidRestOffset = restOffset;
    const PxReal fluidRestOffset = restOffset * 0.6f;
    particleSystem->setRestOffset(restOffset);
    particleSystem->setContactOffset(restOffset + 0.01f);
    particleSystem->setParticleContactOffset(fluidRestOffset / 0.6f);
    particleSystem->setSolidRestOffset(solidRestOffset);
    particleSystem->setFluidRestOffset(fluidRestOffset);
    particleSystem->enableCCD(false);
    particleSystem->setMaxVelocity(solidRestOffset * 100.f);

    auto pbdHandle = new PxPbdHandle();
    pbdHandle->Foundation = sceneHandle->Foundation;
    pbdHandle->Physics = sceneHandle->Physics;
    pbdHandle->Scene = sceneHandle->Scene;
    pbdHandle->Cooking = sceneHandle->Cooking;
    pbdHandle->CudaManager = sceneHandle->CudaManager;
    pbdHandle->Pbd = particleSystem;

    gMaxParticles = maxParticles;
    gParticleInfo.posInvMass = new PxArray<PxVec4>(maxParticles);
    gParticleInfo.velocity = new PxArray<PxVec4>(maxParticles);
    gParticleInfo.phase = new PxArray<PxU32>(maxParticles);

    pbdHandle->Scene->addActor(*particleSystem);


    PxU32* phase = sceneHandle->CudaManager->allocPinnedHostBuffer<PxU32>(gMaxParticles);
    PxVec4* positionInvMass = sceneHandle->CudaManager->allocPinnedHostBuffer<PxVec4>(gMaxParticles);
    PxVec4* velocity = sceneHandle->CudaManager->allocPinnedHostBuffer<PxVec4>(gMaxParticles);

    // We are applying different material parameters for each section
    const PxU32 maxMaterials = 3;
    PxU32 phases[maxMaterials];
    for (PxU32 i = 0; i < maxMaterials; ++i)
    {
        PxPBDMaterial* mat = pbdHandle->Physics->createPBDMaterial(0.05f, i / (maxMaterials - 1.0f), 0.f, 10.002f * (i + 1), 0.5f, 0.005f * i, 0.01f, 0.f, 0.f);
        phases[i] = pbdHandle->Pbd->createPhase(mat, PxParticlePhaseFlags(PxParticlePhaseFlag::eParticlePhaseFluid | PxParticlePhaseFlag::eParticlePhaseSelfCollide));
    }

    PxU32 numX = numParticlesDim;
    PxU32 numY = numParticlesDim;
    PxU32 numZ = numParticlesDim;
    PxReal x = centerX;
    PxReal y = centerY;
    PxReal z = centerZ;
    const PxReal particleMass = fluidDensity * 1.333f * 3.14159f * particleSpacing * particleSpacing * particleSpacing;
    for (PxU32 i = 0; i < numX; ++i)
    {
        for (PxU32 j = 0; j < numY; ++j)
        {
            for (PxU32 k = 0; k < numZ; ++k)
            {
                const PxU32 index = i * (numY * numZ) + j * numZ + k;
                const PxU16 matIndex = (PxU16)(i * maxMaterials / numX);
                const PxVec4 pos(x, y, z, 1.0f / particleMass);
                phase[index] = phases[matIndex];
                positionInvMass[index] = pos;
                velocity[index] = PxVec4(0.0f);

                z += particleSpacing;
            }
            z = centerZ;
            y += particleSpacing;
        }
        y = centerY;
        x += particleSpacing;
    }

    ExtGpu::PxParticleBufferDesc bufferDesc;
    bufferDesc.maxParticles = gMaxParticles;
    bufferDesc.numActiveParticles = gMaxParticles;

    bufferDesc.positions = positionInvMass;
    bufferDesc.velocities = velocity;
    bufferDesc.phases = phase;

    auto particleBuffer = physx::ExtGpu::PxCreateAndPopulateParticleBuffer(bufferDesc, pbdHandle->CudaManager);
    pbdHandle->Pbd->addParticleBuffer(particleBuffer);
    pbdHandle->ParticleBuffer = particleBuffer;

    pbdHandle->CudaManager->freePinnedHostBuffer(positionInvMass);
    pbdHandle->CudaManager->freePinnedHostBuffer(velocity);
    pbdHandle->CudaManager->freePinnedHostBuffer(phase);

    return pbdHandle;
}

DllExport(void) pxGetParticleProperties(PxPbdHandle* handle, V4f* positionsHost, V4f* velsHost, PxU32* phasesHost){
//DllExport(void) pxGetParticleProperties(PxPbdHandle* handle, float* positionsHost, float* velsHost, PxU32* phasesHost){

    PxVec4* positions = handle->ParticleBuffer->getPositionInvMasses();
    PxVec4* vels = handle->ParticleBuffer->getVelocities();
    PxU32* phases = handle->ParticleBuffer->getPhases();

    const PxU32 numParticles = handle->ParticleBuffer->getNbActiveParticles();

    PxScene* scene;
    PxGetPhysics().getScenes(&scene, 1);
    PxCudaContextManager* cudaContexManager = scene->getCudaContextManager();
    
    cudaContexManager->acquireContext();

    PxCudaContext* cudaContext = cudaContexManager->getCudaContext();
    //cudaContext->memcpyDtoH(gParticleInfo.posInvMass->begin(), CUdeviceptr(positions), sizeof(PxVec4) * numParticles);
    //cudaContext->memcpyDtoH(gParticleInfo.velocity->begin(), CUdeviceptr(vels), sizeof(PxVec4) * numParticles);
    //cudaContext->memcpyDtoH(gParticleInfo.phase->begin(), CUdeviceptr(phases), sizeof(PxU32) * numParticles);
    cudaContext->memcpyDtoH(positionsHost, CUdeviceptr(positions), sizeof(PxVec4) * numParticles);
    cudaContext->memcpyDtoH(velsHost, CUdeviceptr(vels), sizeof(PxVec4) * numParticles);
    cudaContext->memcpyDtoH(phasesHost, CUdeviceptr(phases), sizeof(PxU32) * numParticles);
    cudaContexManager->releaseContext();

    //int i = 0;
    //for (auto it = gParticleInfo.posInvMass->begin(); it != gParticleInfo.posInvMass->end(); ++it)
    //{   
    //    //positionsHost[i].X = it->x;
    //    //positionsHost[i].Y = it->y;
    //    //positionsHost[i].Z = it->z;
    //    //positionsHost[i].W = it->w;
    //    //++i;

    //    positionsHost[i++] = it->x;
    //    positionsHost[i++] = it->y;
    //    positionsHost[i++] = it->z;
    //    positionsHost[i++] = it->w;
    //}
    //i = 0;
    //for (auto it = gParticleInfo.velocity->begin(); it != gParticleInfo.velocity->end(); ++it)
    //{
    //    //velsHost[i].X = it->x;
    //    //velsHost[i].Y = it->y;
    //    //velsHost[i].Z = it->z;
    //    //velsHost[i].W = it->w;
    //    //++i;

    //    velsHost[i++] = it->x;
    //    velsHost[i++] = it->y;
    //    velsHost[i++] = it->z;
    //    velsHost[i++] = it->w;
    //}
    //i = 0;
    //for (auto it = gParticleInfo.phase->begin(); it != gParticleInfo.phase->end(); ++it)
    //{
    //    phasesHost[i] = *it;
    //    ++i;
    //}

    positionsHost[0].X = 1.1f; 
    positionsHost[0].Y = 0.2f;
    positionsHost[1].Z = 0.3f;
    positionsHost[1].W = 9.4f;
    velsHost[0].X = 0.6f;
    phasesHost[0] = PxU32(42);
}
 
//DllExport(void) pxSetParticleProperties(PxPbdHandle* handle, int maxParticles, 
//    PxU32* phase, PxVec4* positionInvMass, PxVec4* velocity) {
//    auto cudaContextManager = handle->CudaManager;
//    //PxVec4* bufferPos = particleBuffer->getPositionInvMasses();
//
//    PxU32* phaseMem = cudaContextManager->allocPinnedHostBuffer<PxU32>(maxParticles);
//    PxVec4* positionInvMassMem = cudaContextManager->allocPinnedHostBuffer<PxVec4>(maxParticles);
//    PxVec4* velocityMem = cudaContextManager->allocPinnedHostBuffer<PxVec4>(maxParticles);
//
//    for (PxU32 i = 0; i < maxParticles; ++i)
//    {
//        phaseMem[i] = phase[i];
//        positionInvMassMem[i] = positionInvMass[i];
//        velocityMem[i] = velocity[i];
//    }
//
//    auto cudaContext = handle->CudaManager->context();
//    cudaContext->memcpyHtoDAsync(bufferPos, positionsHost, 1000 * sizeof(PxVec4), 0);
//    particleBuffer->raiseFlags(PxParticleBufferFlag::eUPDATE_POSITION);
//    particleBuffer->setNbActiveParticles(1000);
//
//    gParticleBuffer = ExtGpu::PxCreateAndPopulateParticleAndDiffuseBuffer(bufferDesc, cudaContextManager);
//    gParticleSystem->addParticleBuffer(gParticleBuffer);
//}


int main(int argc, char** argv) {
    return 0;
}