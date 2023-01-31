cmake_minimum_required(VERSION 3.9)
include("cmake/Aardvark.cmake")

project(PhysXNative)


find_path(PHYSX_INCLUDE_DIR PxPhysicsAPI.h)
add_library(PhysXNative SHARED PhysXNative.h PhysXNative.cpp)


# PhysX does not have a find_package setup therefore we have to find it manually
include(SelectLibraryConfigurations)

find_library(PhysXPvdSDK_LIBRARY_RELEASE NAMES PhysXPvdSDK_64 PhysXPvdSDK_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}" NO_DEFAULT_PATH REQUIRED)
find_library(PhysXPvdSDK_LIBRARY_DEBUG NAMES PhysXPvdSDK_64 PhysXPvdSDK_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/debug" NO_DEFAULT_PATH REQUIRED)
select_library_configurations(PhysXPvdSDK)

find_library(PhysXExtensions_LIBRARY_RELEASE NAMES PhysXExtensions_64 PhysXExtensions_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}" NO_DEFAULT_PATH REQUIRED)
find_library(PhysXExtensions_LIBRARY_DEBUG NAMES PhysXExtensions_64 PhysXExtensions_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/debug" NO_DEFAULT_PATH REQUIRED)
select_library_configurations(PhysXExtensions)

find_library(PhysXFoundation_LIBRARY_RELEASE NAMES PhysXFoundation_64 PhysXFoundation_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}" NO_DEFAULT_PATH REQUIRED)
find_library(PhysXFoundation_LIBRARY_DEBUG NAMES PhysXFoundation_64 PhysXFoundation_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/debug" NO_DEFAULT_PATH REQUIRED)
select_library_configurations(PhysXFoundation)

find_library(PhysXCommon_LIBRARY_RELEASE NAMES PhysXCommon_64 PhysXCommon_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}" NO_DEFAULT_PATH REQUIRED)
find_library(PhysXCommon_LIBRARY_DEBUG NAMES PhysXCommon_64 PhysXCommon_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/debug" NO_DEFAULT_PATH REQUIRED)
select_library_configurations(PhysXCommon)

find_library(PhysX_LIBRARY_RELEASE NAMES PhysX_64 PhysX_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}" NO_DEFAULT_PATH REQUIRED)
find_library(PhysX_LIBRARY_DEBUG NAMES PhysX_64 PhysX_static_64 PATH_SUFFIXES lib PATHS "${_VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/debug" NO_DEFAULT_PATH REQUIRED)
select_library_configurations(PhysX)

if(WIN32 OR APPLE)
    target_link_libraries(${PROJECT_NAME} PRIVATE ${PhysXPvdSDK_LIBRARY} ${PhysXExtensions_LIBRARY} ${PhysXFoundation_LIBRARY} ${PhysXCommon_LIBRARY} ${PhysX_LIBRARY} ${PhysXPvdSDK_LIBRARY})
else()
    target_link_libraries(${PROJECT_NAME} PRIVATE -Wl,--start-group ${PhysXPvdSDK_LIBRARY} ${PhysXExtensions_LIBRARY} ${PhysXFoundation_LIBRARY} ${PhysXCommon_LIBRARY} ${PhysX_LIBRARY} ${PhysXPvdSDK_LIBRARY} -Wl,--end-group)
endif()

target_include_directories(${PROJECT_NAME} PRIVATE ${PHYSX_INCLUDE_DIR})

set(CMAKE_BUILD_TYPE, "Release")
if(UNIX)
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -O3 -ffunction-sections -fdata-sections -fvisibility=hidden")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3 -ffunction-sections -fdata-sections -fvisibility=hidden")
endif()



set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
if(APPLE)
    set(CMAKE_SHARED_LINKER_FLAGS "-O3 -flto=thin -dead_strip")
elseif(UNIX)
    set(CMAKE_SHARED_LINKER_FLAGS "-O3 -Wl,--gc-sections -Wl,--strip-all")
endif()


# include_directories(~/vcpkg/packages/ceres_arm64-osx/include)
# target_link_libraries(PhysXNative PRIVATE physx)

SET(CMAKE_INSTALL_PREFIX "${CMAKE_CURRENT_SOURCE_DIR}/../../libs/Native/PhysX/") 
install(TARGETS PhysXNative DESTINATION ${OS}/${ARCH})