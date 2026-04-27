project(jalium.native.browser)

set(HEADERS
    include/jalium_browser_api.h
)

set(SOURCES
    src/browser.cpp
)

add_library(${PROJECT_NAME} ${JALIUM_LIB_TYPE} ${HEADERS} ${SOURCES})

target_include_directories(${PROJECT_NAME}
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/third_party/webview2/include
)

if(NOT JALIUM_BUILD_STATIC)
    target_compile_definitions(${PROJECT_NAME}
        PRIVATE
            JALIUM_BROWSER_EXPORTS
    )
endif()

if(WIN32)
    target_link_libraries(${PROJECT_NAME}
        PRIVATE
            ole32
            user32
    )
endif()

set_target_properties(${PROJECT_NAME} PROPERTIES
    OUTPUT_NAME "jalium.native.browser"
    DEBUG_POSTFIX ""
)

# Mirror output to the layout consumed by managed packaging.
add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E make_directory
        "${CMAKE_SOURCE_DIR}/bin/native/$<CONFIG>"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        "$<TARGET_FILE:${PROJECT_NAME}>"
        "${CMAKE_SOURCE_DIR}/bin/native/$<CONFIG>/$<TARGET_FILE_NAME:${PROJECT_NAME}>"
)
