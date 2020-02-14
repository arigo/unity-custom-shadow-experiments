
// Shadow Map info
sampler VSM_ShadowTex1, VSM_ShadowTex2;   // two textures because some targets don't support RGHalf
float4x4 VSM_LightMatrix;
float4x4 VSM_LightMatrixNormal;
float VSM_DeltaExtraDistance, VSM_InvNumCascades;


float VSM_ShadowIntensity(float3 wPos, float3 wNormal)
{
    float3 lightSpacePos = mul(VSM_LightMatrix, float4(wPos, 1));
    float2 lightSpaceNorm = mul((float3x3)VSM_LightMatrixNormal, wNormal);
    float depth = lightSpacePos.z;

    float2 uv = lightSpacePos.xy;       /* should be in range [-0.5, 0.5] here */
    uv += lightSpaceNorm.xy;

    const float MAX = 0.485;

    float2 uv_abs = abs(uv);
    float magnitude = max(uv_abs.x, uv_abs.y) * (2 / MAX);
    float cascade = floor(log2(max(magnitude, 1)));
    /* ^^^ an integer at least 0 */
    float cascade_scale = exp2(cascade);
    uv /= cascade_scale;


    uv += float2(0.5, 0.5 + cascade);
    uv.y *= VSM_InvNumCascades;

    float shadowIntensity = 0;
    float2 s = float2(tex2D(VSM_ShadowTex1, uv).r, tex2D(VSM_ShadowTex2, uv).r);
    //depth *= exp2(CASCADES - 1 - cascade);


    // https://www.gdcvault.com/play/1023808/Rendering-Antialiased-Shadows-with-Moment
    // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch08.html
    // The moments of the fragment live in "_shadowTex", decoded to 's'

    // average / expected depth and depth^2 across the texels
    // E(x) and E(x^2)
    float x = s.r;
    float x2 = s.g;

    // calculate the variance of the texel based on
    // the formula var = E(x^2) - E(x)^2
    // https://en.wikipedia.org/wiki/Algebraic_formula_for_the_variance#Proof
    float var = x2 - x * x;

    // calculate our initial probability based on the basic depths
    // if our depth is closer than x, then the fragment has a 100%
    // probability of being lit (p=1)
    float p_inv = depth > (x + VSM_DeltaExtraDistance * cascade_scale);

    // calculate the upper bound of the probability using Chebyshev's inequality
    // https://en.wikipedia.org/wiki/Chebyshev%27s_inequality
    float delta = depth - x;
    float p_max = var / (var + delta * delta);

    p_max = 1.9 - p_max * 3;
    //p_max *= pow(2, cascade);

    // To alleviate the light bleeding, expand the shadows to fill in the gaps
    //float amount = _VarianceShadowExpansion;
    //p_max = clamp( (p_max - amount) / (1 - amount), 0, 1);
    float p_max_inv = clamp(p_max + 0.5, 0, 1);

    //p_max_inv = 1;  // XXXXXXXXXXXXXX

    return 1 - p_max_inv * p_inv;
}

#undef UNITY_SHADOW_ATTENUATION
#define UNITY_SHADOW_ATTENUATION(a, worldPos)   VSM_ShadowIntensity(worldPos, a.worldNormal)
