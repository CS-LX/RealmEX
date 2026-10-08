#version 100

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='COLOR' Attribute='a_color' />
// <Semantic Name='TEXCOORD' Attribute='a_texcoord' />

uniform mat4 u_viewProjectionMatrix;
#ifdef TRANSPARENT
uniform vec3 u_viewPosition;
#endif

attribute vec3 a_position;
attribute vec4 a_color;
attribute vec2 a_texcoord;

varying vec4 v_color;
varying vec2 v_texcoord;

void main()
{
    v_texcoord = a_texcoord;
    v_color = a_color;
#ifdef TRANSPARENT
    // SC fluid vertices encode top/side in alpha; it is not their final opacity.
    vec3 direction = u_viewPosition - a_position;
    float incidence = abs(direction.y) / max(length(direction), 0.001);
    v_color.a = mix(clamp(1.2 - 0.7 * incidence, 0.0, 1.0), 0.85, a_color.a);
#endif
    gl_Position = u_viewProjectionMatrix * vec4(a_position, 1.0);
    OPENGL_POSITION_FIX;
}
