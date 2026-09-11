cmake_minimum_required(VERSION 3.20)
project(ffpe_native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(ffpe_native SHARED src/ffpe_native.cpp)
target_include_directories(ffpe_native PUBLIC include)
if(MSVC)
  target_compile_options(ffpe_native PRIVATE /W4 /WX)
else()
  target_compile_options(ffpe_native PRIVATE -Wall -Wextra -Wpedantic -Werror)
endif()

add_executable(ffpe_native_tests tests/native_tests.cpp)
target_link_libraries(ffpe_native_tests PRIVATE ffpe_native)
target_include_directories(ffpe_native_tests PRIVATE include)

include(CTest)
add_test(NAME ffpe_native_tests COMMAND ffpe_native_tests)
