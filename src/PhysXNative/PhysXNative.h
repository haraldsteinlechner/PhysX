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
    physx::PxFoundation* Foundation;
    physx::PxPhysics* Physics;
} PxHandle;

typedef struct {
    physx::PxFoundation* Foundation;
    physx::PxPhysics* Physics;
    physx::PxScene* Scene;
} PxSceneHandle;

DllExport(PxHandle*) pxInit();
DllExport(void) pxDestroy(PxHandle* handle);

DllExport(PxSceneHandle*) pxCreateScene(PxHandle* handle, V3d gravity);
DllExport(void) pxDestroyScene(PxSceneHandle* handle);