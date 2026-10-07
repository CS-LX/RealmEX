#version 100

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='COLOR' Attribute='a_color' />
// <Semantic Name='TEXCOORD' Attribute='a_texcoord' />

uniform mat4 u_viewProjectionMatrix;
uniform float u_verticalOffset;

attribute vec3 a_position;
attribute vec4 a_color;
attribute vec2 a_texcoord;

varying vec4 v_color;
varying vec2 v_texcoord;

void main()
{
    v_texcoord = a_texcoord;
    v_color = a_color;
    gl_Position = u_viewProjectionMatrix * vec4(a_position + vec3(0.0, u_verticalOffset, 0.0), 1.0);
    OPENGL_POSITION_FIX;
}
