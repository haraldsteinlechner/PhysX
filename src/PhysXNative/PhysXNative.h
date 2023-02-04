#pragma once


#ifdef __APPLE__
#define DllExport(t) extern "C" __attribute__((visibility("default"))) t
#elif __GNUC__
#define DllExport(t) extern "C" __attribute__((visibility("default"))) t
#else
#define DllExport(t) extern "C"  __declspec( dllexport ) t __cdecl
#endif

#include <PxPhysicsAPI.h>
typedef struct {
    float X;
    float Y;
    float Z;
} V3f;

typedef struct {
    float X;
    float Y;
    float Z;
    float W;
} V4f;

typedef struct {
    double X;
    double Y;
    double Z;
} V3d;

typedef struct {
    double X;
    double Y;
    double Z;
    double W;
} V4d;

typedef struct {
    double W;
    double X;
    double Y;
    double Z;
} Rot3d;

typedef struct {
    Rot3d Rot;
    V3d Trans;
} Euclidean3d;

typedef struct {
    physx::PxFoundation* Foundation;
    physx::PxPhysics* Physics;
    physx::PxCooking* Cooking;
} PxHandle;

typedef struct {
    physx::PxFoundation* Foundation;
    physx::PxPhysics* Physics;
    physx::PxCooking* Cooking;
    physx::PxScene* Scene;
    physx::PxCudaContextManager* CudaManager;
} PxSceneHandle;

typedef struct {
    physx::PxArray<physx::PxVec4>* posInvMass;
    physx::PxArray<physx::PxVec4>* velocity;
    physx::PxArray<physx::PxU32>* phase;
} PxParticleInfo;

typedef struct {
    physx::PxFoundation* Foundation;
    physx::PxPhysics* Physics;
    physx::PxCooking* Cooking;
    physx::PxScene* Scene;
    physx::PxCudaContextManager* CudaManager;
    physx::PxPBDParticleSystem* Pbd;
    physx::PxParticleBuffer* ParticleBuffer;
} PxPbdHandle;

typedef struct {
    physx::PxMaterial* Material;
    physx::PxGeometry* Geometry;
    Euclidean3d Pose;
} PxShapeDescription;



DllExport(PxHandle*) pxInit();
DllExport(void) pxDestroy(PxHandle* handle);

DllExport(PxSceneHandle*) pxCreateScene(PxHandle* handle, V3d gravity);
DllExport(void) pxDestroyScene(PxPbdHandle* handle);
DllExport(void) pxSimulate(PxSceneHandle* scene, float dt);