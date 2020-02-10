

float4 EncodeToARGB(float depth)
{
    depth = clamp(depth, 0, 1);
    depth *= 255.0;           /* in the closed interval [0, 255] */
    float r = floor(depth);   /* integer in [0, 255] */
    depth -= r;
    depth *= 256.0;           /* in the open interval [0, 256[ */
    float g = floor(depth);   /* integer in [0, 255] */
    depth -= g;
    depth *= 255.0;           /* in the closed interval [0, 255] */
    float b = depth;
    return float4(r, g, b, 255.0) / 255.0;
}

float DecodeFromARGB(float4 value)
{
    value *= 255.0;

    float depth = value.b;
    depth /= 255.0;
    depth += value.g;
    depth /= 256.0;
    depth += value.r;
    depth /= 255.0;
    return depth;
}
