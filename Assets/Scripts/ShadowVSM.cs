using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]
public class ShadowVSM : MonoBehaviour
{
    public enum ShadowComputation
    {
        ManualFromScript,
        AutomaticFull,
        AutomaticIncrementalCascade,
    }

    [Header("Shadow computation")]
    public ShadowComputation _shadowComputation = ShadowComputation.AutomaticFull;

    [Header("Initialization")]
    public Shader _depthShader;
    public Shader blurShader;
    Material _blur_material;

    public int _resolution = 512;

    [Header("Shadow Settings")]
    public bool drawTransparent = true;
    public int numCascades = 6;

    public float deltaExtraDistance = 0.0024f;
    public float firstCascadeLevelSize = 8.0f;
    public float depthOfShadowRange = 1000.0f;
    public FilterMode _filterMode = FilterMode.Bilinear;

    // Render Targets
    Camera _shadowCam;
    public RenderTexture _backTarget1; // debugging, see the inspector
    public RenderTexture _backTarget2; // debugging, see the inspector
    public RenderTexture _target;      // debugging, see the inspector


    public void UpdateShadowTargets()
    {

    }


    #region LifeCycle
    void OnEnable()
    {
        if (Application.isPlaying)
            switch (_shadowComputation)
            {
                case ShadowComputation.AutomaticFull:
                    Camera.onPreRender += AutomaticFull;
                    break;

                case ShadowComputation.AutomaticIncrementalCascade:
                    Camera.onPreRender += AutomaticIncrementalCascade;
                    break;
            }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            switch (_shadowComputation)
            {
                case ShadowComputation.AutomaticFull:
                    Camera.onPreRender -= AutomaticFull;
                    break;

                case ShadowComputation.AutomaticIncrementalCascade:
                    Camera.onPreRender -= AutomaticIncrementalCascade;
                    break;
            }
        DestroyInternals();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            UpdateShadowsFull();
    }

    void AutomaticFull(Camera cam)
    {
        if (cam == Camera.main)
            UpdateShadowsFull();
    }

    void AutomaticIncrementalCascade(Camera cam)
    {
        if (cam == Camera.main)
        {
            if (_auto_incr_cascade == null)
                _auto_incr_cascade = UpdateShadowsIncrementalCascade();
            if (!_auto_incr_cascade.MoveNext())
                _auto_incr_cascade = null;
        }
    }

    RenderTexture oldBackTarget1, oldBackTarget2;
    IEnumerator _auto_incr_cascade;

    public IEnumerator UpdateShadowsIncrementalCascade(int cullingMask = -1, Transform trackTransform = null)
    {
        /* Update one cascade between each yield.  It has no visible effect, until it has
         * been resumed "numCascades - 1" times, i.e. until it has computed the last cascade;
         * at this point it really updates the shadows and finishes. */
        Swap(ref oldBackTarget1, ref _backTarget1);
        Swap(ref oldBackTarget2, ref _backTarget2);
        if (!InitializeUpdateSteps())
            yield break;

        for (int i = numCascades - 1; i >= 0; i--)
        {
            ComputeCascade(i, cullingMask, trackTransform);
            if (i > 0)
                yield return null;
        }

        FinalizeUpdateSteps();
    }

    public void UpdateShadowsFull(int cullingMask = -1, Transform trackTransform = null)
    {
        _auto_incr_cascade = null;
        if (!InitializeUpdateSteps())
            return;
        for (int i = numCascades - 1; i >= 0; i--)
            ComputeCascade(i, cullingMask, trackTransform);

        FinalizeUpdateSteps();
    }

    bool InitializeUpdateSteps()
    {
        if (!UpdateRenderTexture())
            return false;

        SetUpShadowCam();
        _shadowCam.targetTexture = _target;

        _blur_material.SetVector("BlurPixelSize", new Vector2(1f / _resolution, 1f / _resolution));
        return true;
    }

    static Transform GetMainLightTransform()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
            sun = FindObjectOfType<Light>();
        return sun.transform;
    }

    void ComputeCascade(int lvl, int cullingMask, Transform trackTransform)
    {
        if (trackTransform == null)
            trackTransform = GetMainLightTransform();
        UpdateShadowCameraPos(cullingMask, trackTransform);

        _shadowCam.orthographicSize = firstCascadeLevelSize * Mathf.Pow(2, lvl);
        _shadowCam.RenderWithShader(_depthShader, "");

        float y1 = lvl / (float)numCascades;
        float y2 = (lvl + 1) / (float)numCascades;
        _blur_material.DisableKeyword("BLUR_NOTHING");
        _blur_material.EnableKeyword("BLUR_LINEAR_PART");
        CustomBlit(_target, _backTarget1, _blur_material, y1, y2);

        _blur_material.DisableKeyword("BLUR_LINEAR_PART");
        CustomBlit(_target, _backTarget2, _blur_material, y1, y2);
    }

    void FinalizeUpdateSteps()
    {
        _blur_material.EnableKeyword("BLUR_NOTHING");
        CustomBlit(_target, _backTarget1, _blur_material, 1f - 1f / _backTarget1.height, 1f);
        CustomBlit(_target, _backTarget2, _blur_material, 1f - 1f / _backTarget2.height, 1f);

        UpdateShaderValues();
    }

    static void CustomBlit(Texture source, RenderTexture target, Material mat, float y1, float y2)
    {
        var original = RenderTexture.active;
        RenderTexture.active = target;

        // Set the '_MainTex' variable to the texture given by 'source'
        mat.SetTexture("_MainTex", source);
        GL.PushMatrix();
        GL.LoadOrtho();
        // activate the first shader pass (in this case we know it is the only pass)
        mat.SetPass(0);

        GL.Begin(GL.QUADS);
        GL.TexCoord2(0f, 0f); GL.Vertex3(0f, y1, 0f);
        GL.TexCoord2(0f, 1f); GL.Vertex3(0f, y2, 0f);
        GL.TexCoord2(1f, 1f); GL.Vertex3(1f, y2, 0f);
        GL.TexCoord2(1f, 0f); GL.Vertex3(1f, y1, 0f);
        GL.End();
        GL.PopMatrix();
        RenderTexture.active = original;
    }

    void DestroyTargets()
    {
        if (_target)
        {
            DestroyImmediate(_target);
            _target = null;
        }
        if (_backTarget1)
        {
            DestroyImmediate(_backTarget1);
            _backTarget1 = null;
        }
        if (_backTarget2)
        {
            DestroyImmediate(_backTarget2);
            _backTarget2 = null;
        }
        if (oldBackTarget1)
        {
            DestroyImmediate(oldBackTarget1);
            oldBackTarget1 = null;
        }
        if (oldBackTarget2)
        {
            DestroyImmediate(oldBackTarget2);
            oldBackTarget2 = null;
        }
    }

    // Disable the shadows
    void DestroyInternals()
    {
        if (_shadowCam)
        {
            DestroyImmediate(_shadowCam.gameObject);
            _shadowCam = null;
        }
        DestroyTargets();
        //ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
    }

    private void OnDestroy()
    {
        DestroyInternals();
    }
#endregion

#region Update Functions
    void SetUpShadowCam()
    {
        if (_shadowCam) return;

        // Create the shadow rendering camera
        GameObject go = new GameObject("shadow cam (not saved)");
        //go.hideFlags = HideFlags.HideAndDontSave;
        go.hideFlags = HideFlags.DontSave;

        _shadowCam = go.AddComponent<Camera>();
        _shadowCam.orthographic = true;
        _shadowCam.nearClipPlane = 0;
        _shadowCam.enabled = false;
        _shadowCam.backgroundColor = new Color(65, 0, 0, 1);
        _shadowCam.clearFlags = CameraClearFlags.SolidColor;

        if (_blur_material == null)
            _blur_material = new Material(blurShader);
        _blur_material.SetColor("_Color", _shadowCam.backgroundColor);
    }

    void UpdateShaderValues()
    {
        //ForAllKeywords(s => Shader.DisableKeyword(ToKeyword(s)));
        //Shader.EnableKeyword(ToKeyword(Shadows.VARIANCE));

        // Set the qualities of the textures
        Shader.SetGlobalTexture("_ShadowTex1", _backTarget1);
        Shader.SetGlobalTexture("_ShadowTex2", _backTarget2);
        //Shader.SetGlobalFloat("_MaxShadowIntensity", maxShadowIntensity);
        //Shader.SetGlobalFloat("_VarianceShadowExpansion", varianceShadowExpansion);
        Shader.SetGlobalFloat("_DeltaExtraDistance", deltaExtraDistance);
        Shader.SetGlobalFloat("_InvNumCascades", 1f / numCascades);

        if (drawTransparent) Shader.EnableKeyword("DRAW_TRANSPARENT_SHADOWS");
        else Shader.DisableKeyword("DRAW_TRANSPARENT_SHADOWS");

        Vector3 size;
        size.y = _shadowCam.orthographicSize * 2;
        size.x = _shadowCam.aspect * size.y;
        size.z = _shadowCam.farClipPlane - _shadowCam.nearClipPlane;

        size.x = 1f / size.x;
        size.y = 1f / size.y;
        size.z = 128f / size.z;

        var mat = _shadowCam.transform.worldToLocalMatrix;
        Shader.SetGlobalMatrix("_LightMatrix", Matrix4x4.Scale(size) * mat);
        Shader.SetGlobalMatrix("_LightMatrixNormal", Matrix4x4.Scale(Vector3.one / _resolution) * mat);
    }

    // Refresh the render target if the scale has changed
    bool UpdateRenderTexture()
    {
        if (_target != null && _target.width != _resolution)
            DestroyTargets();
        if (_backTarget1 != null && (_backTarget1.filterMode != _filterMode ||
                                     _backTarget1.height != _resolution * numCascades))
            DestroyTargets();

        if (_target == null)
        {
            if (numCascades <= 0 || _resolution <= 0)
                return false;
            Debug.Log("Creating render textures for custom shadows");
            _target = CreateTarget();
        }
        if (_backTarget1 == null)
        {
            _backTarget1 = CreateBackTarget();
            _backTarget2 = CreateBackTarget();
        }
        return true;
    }

    void UpdateShadowCameraPos(int cullingMask, Transform trackTransform)
    {
        Camera cam = _shadowCam;

        cam.transform.position = trackTransform.position;
        cam.transform.rotation = trackTransform.rotation;
        cam.transform.LookAt(cam.transform.position + cam.transform.forward, cam.transform.up);

        /* Set up the clip planes so that we store depth values in the range [-0.5, 0.5],
         * with values near zero being near us even if depthOfShadowRange is very large.
         * This maximizes the precision in the RHalf textures near us. */
        cam.nearClipPlane = -depthOfShadowRange;
        cam.farClipPlane = depthOfShadowRange;
        cam.aspect = 1;
        cam.cullingMask = cullingMask;
    }

#if false
    // Update the camera view to encompass the geometry it will draw
    void UpdateShadowCameraPos()
    {
        // Update the position
        Camera cam = _shadowCam;
        Light l = FindObjectOfType<Light>();
        cam.transform.position = l.transform.position;
        cam.transform.rotation = l.transform.rotation;
        cam.transform.LookAt(cam.transform.position + cam.transform.forward, cam.transform.up);

        Vector3 center, extents;
        List<Renderer> renderers = new List<Renderer>();
        renderers.AddRange(FindObjectsOfType<Renderer>());

        GetRenderersExtents(renderers, cam.transform, out center, out extents);

        center.z -= extents.z / 2;
        cam.transform.position = cam.transform.TransformPoint(center);
        cam.nearClipPlane = 0;
        cam.farClipPlane = extents.z;

        cam.aspect = extents.x / extents.y;
        cam.orthographicSize = extents.y / 2;
    }
#endif
    #endregion

    #region Utilities
    // Creates a rendertarget
    RenderTexture CreateTarget()
    {
        RenderTexture tg = new RenderTexture(_resolution, _resolution, 24,
                                             RenderTextureFormat.RHalf);
        tg.wrapMode = TextureWrapMode.Clamp;
        tg.antiAliasing = 8;
        tg.Create();

        return tg;
    }

    RenderTexture CreateBackTarget()
    {
        var tg = new RenderTexture(_resolution, _resolution * numCascades, 0, RenderTextureFormat.RHalf);
        //tg.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        //tg.volumeDepth = CASCADES;
        tg.filterMode = _filterMode;
        tg.wrapMode = TextureWrapMode.Clamp;
        //tg.enableRandomWrite = true;
        //tg.autoGenerateMips = false;
        //tg.useMipMap = true;
        tg.Create();

        return tg;
    }

/*void ForAllKeywords(System.Action<Shadows> func)
{
    func(Shadows.HARD);
    func(Shadows.VARIANCE);
}

string ToKeyword(Shadows en)
{
    if (en == Shadows.HARD) return "HARD_SHADOWS";
    if (en == Shadows.VARIANCE) return "VARIANCE_SHADOWS";
    return "";
}*/

#if false
    // Returns the bounds extents in the provided frame
    void GetRenderersExtents(List<Renderer> renderers, Transform frame, out Vector3 center, out Vector3 extents)
    {
        Vector3[] arr = new Vector3[8];

        Vector3 min = Vector3.one * Mathf.Infinity;
        Vector3 max = Vector3.one * Mathf.NegativeInfinity;
        foreach (var r in renderers)
        {
            GetBoundsPoints(r.bounds, arr, frame.worldToLocalMatrix);

            foreach(var p in arr)
            {
                for(int i = 0; i < 3; i ++)
                {
                    min[i] = Mathf.Min(p[i], min[i]);
                    max[i] = Mathf.Max(p[i], max[i]);
                }
            }
        }

        extents = max - min;
        center = (max + min) / 2;
    }

    // Returns the 8 points for the given bounds multiplied by
    // the given matrix
    void GetBoundsPoints(Bounds b, Vector3[] points, Matrix4x4? mat = null)
    {
        Matrix4x4 trans = mat ?? Matrix4x4.identity;

        int count = 0;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 v = b.extents;
                    v.x *= x;
                    v.y *= y;
                    v.z *= z;
                    v += b.center;
                    v = trans.MultiplyPoint(v);

                    points[count++] = v;
                }
    }

#endif
    // Swap Elements A and B
    void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }
#endregion
}
