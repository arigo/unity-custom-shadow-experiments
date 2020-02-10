

float4 EncodeToARGB(float depth)
{
    depth = clamp(depth, 0, 1);
    int int_depth = (int)(depth * 16776960.0);
    float b = int_depth & 255;
    int_depth >>= 8;
    float g = int_depth & 255;
    int_depth >>= 8;
    float r = int_depth;
    return float4(r, g, b, 0) / 255.0;
}

float DecodeFromARGB(float4 value)
{
    float3 x = value.rgb * float3(1 << 16, 1 << 8, 1);
    return (x.r + x.g + x.b) / (16776960.0 / 255.0);
}
