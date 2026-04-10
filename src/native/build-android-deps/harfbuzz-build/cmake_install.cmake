# Install script for directory: D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "C:/Program Files (x86)/harfbuzz")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Release")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Install shared libraries without execute permission?
if(NOT DEFINED CMAKE_INSTALL_SO_NO_EXE)
  set(CMAKE_INSTALL_SO_NO_EXE "0")
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "TRUE")
endif()

# Set path to fallback-tool for dependency-resolution.
if(NOT DEFINED CMAKE_OBJDUMP)
  set(CMAKE_OBJDUMP "C:/Users/suppe/AppData/Local/Android/Sdk/ndk/27.2.12479018/toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-objdump.exe")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/harfbuzz" TYPE FILE FILES
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-aat-layout.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-aat.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-blob.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-buffer.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-common.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-cplusplus.hh"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-deprecated.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-draw.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-face.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-font.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-map.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-color.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-deprecated.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-font.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-layout.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-math.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-meta.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-metrics.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-name.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-shape.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot-var.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ot.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-paint.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-set.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-shape-plan.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-shape.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-style.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-unicode.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-version.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb.h"
    "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-10.4.0/src/hb-ft.h"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/libharfbuzz.a")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "pkgconfig" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/pkgconfig" TYPE FILE FILES "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/harfbuzz.pc")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/harfbuzz/harfbuzzConfig.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/harfbuzz/harfbuzzConfig.cmake"
         "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/CMakeFiles/Export/6988f0906c47366608790bc51d4c19aa/harfbuzzConfig.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/harfbuzz/harfbuzzConfig-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/harfbuzz/harfbuzzConfig.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/harfbuzz" TYPE FILE FILES "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/CMakeFiles/Export/6988f0906c47366608790bc51d4c19aa/harfbuzzConfig.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/harfbuzz" TYPE FILE FILES "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/CMakeFiles/Export/6988f0906c47366608790bc51d4c19aa/harfbuzzConfig-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/harfbuzz" TYPE FILE FILES "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/src/hb-features.h")
endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
if(CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_COMPONENT MATCHES "^[a-zA-Z0-9_.+-]+$")
    set(CMAKE_INSTALL_MANIFEST "install_manifest_${CMAKE_INSTALL_COMPONENT}.txt")
  else()
    string(MD5 CMAKE_INST_COMP_HASH "${CMAKE_INSTALL_COMPONENT}")
    set(CMAKE_INSTALL_MANIFEST "install_manifest_${CMAKE_INST_COMP_HASH}.txt")
    unset(CMAKE_INST_COMP_HASH)
  endif()
else()
  set(CMAKE_INSTALL_MANIFEST "install_manifest.txt")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "D:/Users/suppe/source/repos/Jalium.UI/src/native/build-android-deps/harfbuzz-build/${CMAKE_INSTALL_MANIFEST}"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
