//------------------------------------------------------------------------------
// Shadowing configuration
//------------------------------------------------------------------------------

#define SHADOW_SAMPLING_HARD              0
#define SHADOW_SAMPLING_PCF_LOW           1
#define SHADOW_SAMPLING_PCF_MEDIUM        2
#define SHADOW_SAMPLING_PCF_HIGH          3

#define SHADOW_SAMPLING_ERROR_DISABLED   0
#define SHADOW_SAMPLING_ERROR_ENABLED    1

#define SHADOW_RECEIVER_PLANE_DEPTH_BIAS_DISABLED   0
#define SHADOW_RECEIVER_PLANE_DEPTH_BIAS_ENABLED    1

#define SHADOW_RECEIVER_PLANE_DEPTH_BIAS_MIN_SAMPLING_METHOD    SHADOW_SAMPLING_PCF_MEDIUM

#ifdef TARGET_MOBILE
  #define SHADOW_SAMPLING_METHOD            SHADOW_SAMPLING_HARD
  #define SHADOW_SAMPLING_ERROR             SHADOW_SAMPLING_ERROR_DISABLED
  #define SHADOW_RECEIVER_PLANE_DEPTH_BIAS  SHADOW_RECEIVER_PLANE_DEPTH_BIAS_DISABLED
#else
  #define SHADOW_SAMPLING_METHOD            SHADOW_SAMPLING_HARD
  #define SHADOW_SAMPLING_ERROR             SHADOW_SAMPLING_ERROR_DISABLED
  #define SHADOW_RECEIVER_PLANE_DEPTH_BIAS  SHADOW_RECEIVER_PLANE_DEPTH_BIAS_DISABLED
#endif

#if SHADOW_SAMPLING_ERROR == SHADOW_SAMPLING_ERROR_ENABLED
  #undef SHADOW_RECEIVER_PLANE_DEPTH_BIAS
  #define SHADOW_RECEIVER_PLANE_DEPTH_BIAS  SHADOW_RECEIVER_PLANE_DEPTH_BIAS_ENABLED
#elif SHADOW_SAMPLING_METHOD < SHADOW_RECEIVER_PLANE_DEPTH_BIAS_MIN_SAMPLING_METHOD
  #undef SHADOW_RECEIVER_PLANE_DEPTH_BIAS
  #define SHADOW_RECEIVER_PLANE_DEPTH_BIAS  SHADOW_RECEIVER_PLANE_DEPTH_BIAS_DISABLED
#endif

//------------------------------------------------------------------------------
// Shadow sampling methods
//------------------------------------------------------------------------------

vec2 computeReceiverPlaneDepthBias(const vec3 position) {
    // see: GDC '06: Shadow Mapping: GPU-based Tips and Techniques
    vec2 bias;
#if SHADOW_RECEIVER_PLANE_DEPTH_BIAS == SHADOW_RECEIVER_PLANE_DEPTH_BIAS_ENABLED
    vec3 dx = dFdx(position);
    vec3 dy = dFdy(position);
    bias = vec2(dy.y * dx.z - dx.y * dy.z, dx.x * dy.z - dy.x * dx.z);
    bias *= 1.0 / ((dx.x * dy.y) - (dx.y * dy.x));
#else
    bias = vec2(0.0);
#endif
    return bias;
}

float samplingBias(float depth, const vec2 rpdb, const vec2 texelSize) {
#if SHADOW_SAMPLING_ERROR == SHADOW_SAMPLING_ERROR_ENABLED
    float samplingError = min(dot(texelSize, abs(rpdb)), 0.01);
    depth -= samplingError;
#endif
    return depth;
}

float sampleDepth(const lowp sampler2DShadow map, vec2 base, vec2 dudv, float depth, vec2 rpdb) {
#if SHADOW_RECEIVER_PLANE_DEPTH_BIAS == SHADOW_RECEIVER_PLANE_DEPTH_BIAS_ENABLED
 #if SHADOW_SAMPLING_METHOD >= SHADOW_RECEIVER_PLANE_DEPTH_BIAS_MIN_SAMPLING_METHOD
    depth += dot(dudv, rpdb);
 #endif
#endif
#if defined(TARGET_METAL_ENVIRONMENT)
    return texture(map, vec3(float2(base.x, 1.0 - base.y) + dudv, depth));
#else
    return texture(map, vec3(float2(base.x, base.y) + dudv, depth));
#endif
}

#if SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_HARD
float ShadowSample_Hard(const lowp sampler2DShadow map, const vec2 size, const vec3 position) {
    // vec2 rpdb = computeReceiverPlaneDepthBias(position);
    // float depth = samplingBias(position.z, rpdb, vec2(1.0) / size);
    float depth = position.z;
#if defined(TARGET_METAL_ENVIRONMENT)
    return texture(map, vec3(float2(position.x, 1.0 - position.y), depth));
#else
    return texture(map, vec3(float2(position.x, position.y), depth));
#endif
}
#endif

#if SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_PCF_LOW
float ShadowSample_PCF_Low(const lowp sampler2DShadow map, const vec2 size, vec3 position) {
    //  Castaño, 2013, "Shadow Mapping Summary Part 1"
    vec2 texelSize = vec2(1.0) / size;

    // clamp position to avoid overflows below, which cause some GPUs to abort
    position.xy = clamp(position.xy, vec2(-1.0), vec2(2.0));

    vec2 offset = vec2(0.5);
    vec2 uv = (position.xy * size) + offset;
    vec2 base = (floor(uv) - offset) * texelSize;
    vec2 st = fract(uv);

    vec2 uw = vec2(3.0 - 2.0 * st.x, 1.0 + 2.0 * st.x);
    vec2 vw = vec2(3.0 - 2.0 * st.y, 1.0 + 2.0 * st.y);

    vec2 u = vec2((2.0 - st.x) / uw.x - 1.0, st.x / uw.y + 1.0);
    vec2 v = vec2((2.0 - st.y) / vw.x - 1.0, st.y / vw.y + 1.0);

    u *= texelSize.x;
    v *= texelSize.y;

    vec2 rpdb = computeReceiverPlaneDepthBias(position);

    float depth = samplingBias(position.z, rpdb, texelSize);
    float sum = 0.0;

    sum += uw.x * vw.x * sampleDepth(map, base, vec2(u.x, v.x), depth, rpdb);
    sum += uw.y * vw.x * sampleDepth(map, base, vec2(u.y, v.x), depth, rpdb);

    sum += uw.x * vw.y * sampleDepth(map, base, vec2(u.x, v.y), depth, rpdb);
    sum += uw.y * vw.y * sampleDepth(map, base, vec2(u.y, v.y), depth, rpdb);

    return sum * (1.0 / 16.0);
}
#endif

#if SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_PCF_MEDIUM
float ShadowSample_PCF_Medium(const lowp sampler2DShadow map, const vec2 size, vec3 position) {
    //  Castaño, 2013, "Shadow Mapping Summary Part 1"
    vec2 texelSize = vec2(1.0) / size;

    // clamp position to avoid overflows below, which cause some GPUs to abort
    position.xy = clamp(position.xy, vec2(-1.0), vec2(2.0));

    vec2 offset = vec2(0.5);
    vec2 uv = (position.xy * size) + offset;
    vec2 base = (floor(uv) - offset) * texelSize;
    vec2 st = fract(uv);

    vec3 uw = vec3(4.0 - 3.0 * st.x, 7.0, 1.0 + 3.0 * st.x);
    vec3 vw = vec3(4.0 - 3.0 * st.y, 7.0, 1.0 + 3.0 * st.y);

    vec3 u = vec3((3.0 - 2.0 * st.x) / uw.x - 2.0, (3.0 + st.x) / uw.y, st.x / uw.z + 2.0);
    vec3 v = vec3((3.0 - 2.0 * st.y) / vw.x - 2.0, (3.0 + st.y) / vw.y, st.y / vw.z + 2.0);

    u *= texelSize.x;
    v *= texelSize.y;

    vec2 rpdb = computeReceiverPlaneDepthBias(position);

    float depth = samplingBias(position.z, rpdb, texelSize);
    float sum = 0.0;

    sum += uw.x * vw.x * sampleDepth(map, base, vec2(u.x, v.x), depth, rpdb);
    sum += uw.y * vw.x * sampleDepth(map, base, vec2(u.y, v.x), depth, rpdb);
    sum += uw.z * vw.x * sampleDepth(map, base, vec2(u.z, v.x), depth, rpdb);

    sum += uw.x * vw.y * sampleDepth(map, base, vec2(u.x, v.y), depth, rpdb);
    sum += uw.y * vw.y * sampleDepth(map, base, vec2(u.y, v.y), depth, rpdb);
    sum += uw.z * vw.y * sampleDepth(map, base, vec2(u.z, v.y), depth, rpdb);

    sum += uw.x * vw.z * sampleDepth(map, base, vec2(u.x, v.z), depth, rpdb);
    sum += uw.y * vw.z * sampleDepth(map, base, vec2(u.y, v.z), depth, rpdb);
    sum += uw.z * vw.z * sampleDepth(map, base, vec2(u.z, v.z), depth, rpdb);

    return sum * (1.0 / 144.0);
}
#endif

#if SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_PCF_HIGH
float ShadowSample_PCF_High(const lowp sampler2DShadow map, const vec2 size, vec3 position) {
    //  Castaño, 2013, "Shadow Mapping Summary Part 1"
    vec2 texelSize = vec2(1.0) / size;

    // clamp position to avoid overflows below, which cause some GPUs to abort
    position.xy = clamp(position.xy, vec2(-1.0), vec2(2.0));

    vec2 offset = vec2(0.5);
    vec2 uv = (position.xy * size) + offset;
    vec2 base = (floor(uv) - offset) * texelSize;
    vec2 st = fract(uv);

    vec4 uw = vec4(
         5.0 * st.x - 6.0,
         11.0 * st.x - 28.0,
        -(11.0 * st.x + 17.0),
        -(5.0 * st.x + 1.0));
    vec4 vw = vec4(
         5.0 * st.y - 6.0,
         11.0 * st.y - 28.0,
        -(11.0 * st.y + 17.0),
        -(5.0 * st.y + 1.0));

    vec4 u = vec4(
         (4.0 * st.x - 5.0) / uw.x - 3.0,
         (4.0 * st.x - 16.0) / uw.y - 1.0,
        -(7.0 * st.x + 5.0) / uw.z + 1.0,
        -st.x / uw.w + 3.0);
    vec4 v = vec4(
         (4.0 * st.y - 5.0) / vw.x - 3.0,
         (4.0 * st.y - 16.0) / vw.y - 1.0,
        -(7.0 * st.y + 5.0) / vw.z + 1.0,
        -st.y / vw.w + 3.0);

    u *= texelSize.x;
    v *= texelSize.y;

    vec2 rpdb = computeReceiverPlaneDepthBias(position);

    float depth = samplingBias(position.z, rpdb, texelSize);
    float sum = 0.0;

    sum += uw.x * vw.x * sampleDepth(map, base, vec2(u.x, v.x), depth, rpdb);
    sum += uw.y * vw.x * sampleDepth(map, base, vec2(u.y, v.x), depth, rpdb);
    sum += uw.z * vw.x * sampleDepth(map, base, vec2(u.z, v.x), depth, rpdb);
    sum += uw.w * vw.x * sampleDepth(map, base, vec2(u.w, v.x), depth, rpdb);

    sum += uw.x * vw.y * sampleDepth(map, base, vec2(u.x, v.y), depth, rpdb);
    sum += uw.y * vw.y * sampleDepth(map, base, vec2(u.y, v.y), depth, rpdb);
    sum += uw.z * vw.y * sampleDepth(map, base, vec2(u.z, v.y), depth, rpdb);
    sum += uw.w * vw.y * sampleDepth(map, base, vec2(u.w, v.y), depth, rpdb);

    sum += uw.x * vw.z * sampleDepth(map, base, vec2(u.x, v.z), depth, rpdb);
    sum += uw.y * vw.z * sampleDepth(map, base, vec2(u.y, v.z), depth, rpdb);
    sum += uw.z * vw.z * sampleDepth(map, base, vec2(u.z, v.z), depth, rpdb);
    sum += uw.w * vw.z * sampleDepth(map, base, vec2(u.w, v.z), depth, rpdb);

    sum += uw.x * vw.w * sampleDepth(map, base, vec2(u.x, v.w), depth, rpdb);
    sum += uw.y * vw.w * sampleDepth(map, base, vec2(u.y, v.w), depth, rpdb);
    sum += uw.z * vw.w * sampleDepth(map, base, vec2(u.z, v.w), depth, rpdb);
    sum += uw.w * vw.w * sampleDepth(map, base, vec2(u.w, v.w), depth, rpdb);

    return sum * (1.0 / 2704.0);
}
#endif

//------------------------------------------------------------------------------
// Shadow sampling dispatch
//------------------------------------------------------------------------------

/**
 * Samples the light visibility at the specified position in light (shadow)
 * space. The output is a filtered visibility factor that can be used to multiply
 * the light intensity.
 */
float shadow(const lowp sampler2DShadow shadowMap, const vec3 shadowPosition) {
    vec2 size = vec2(textureSize(shadowMap, 0));
#if SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_HARD
    return ShadowSample_Hard(shadowMap, size, shadowPosition);
#elif SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_PCF_LOW
    return ShadowSample_PCF_Low(shadowMap, size, shadowPosition);
#elif SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_PCF_MEDIUM
    return ShadowSample_PCF_Medium(shadowMap, size, shadowPosition);
#elif SHADOW_SAMPLING_METHOD == SHADOW_SAMPLING_PCF_HIGH
    return ShadowSample_PCF_High(shadowMap, size, shadowPosition);
#endif
}
