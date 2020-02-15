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
    public Transform lightCenterAndDirection;

    [Header("Shadow Settings")]
    public int _resolution = 512;
    public int numCascades = 6;

    public float deltaExtraDistance = 0.003f;
    public float firstCascadeLevelSize = 8.0f;
    public float depthOfShadowRange = 1000.0f;
    public FilterMode _filterMode = FilterMode.Bilinear;
    public bool useDitheringForTransparent = false;

    [Header("Limit shadow casters")]
    public LayerMask cullingMask = -1;
    public bool onlyOpaqueCasters = true;

    [Header("Debugging")]
    // Render Targets
    public RenderTexture _backTarget1; // debugging, see the inspector
    public RenderTexture _backTarget2; // debugging, see the inspector
    public RenderTexture _target;      // debugging, see the inspector

    Camera _shadowCam;


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

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
            UpdateShadowsFull();
    }
#endif

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

    struct ComputeData
    {
        internal Transform trackTransform;
        internal int numCascades;
        internal float firstCascadeLevelSize;
    }

    void InitComputeData(out ComputeData cdata)
    {
        /* ComputeData stores parameters that we want to remain constant over several calls
         * to UpdateShadowsIncrementalCascade(), even if they are public fields that may be
         * modified at a random point. */
        if (lightCenterAndDirection == null)
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
                sun = FindObjectOfType<Light>();
            cdata.trackTransform = sun.transform;
        }
        else
            cdata.trackTransform = lightCenterAndDirection;

        cdata.numCascades = numCascades;
        cdata.firstCascadeLevelSize = firstCascadeLevelSize;
    }

    public IEnumerator UpdateShadowsIncrementalCascade()
    {
        /* Update one cascade between each yield.  It has no visible effect, until it has
         * been resumed "numCascades - 1" times, i.e. until it has computed the last cascade;
         * at this point it really updates the shadows and finishes. */
        Swap(ref oldBackTarget1, ref _backTarget1);
        Swap(ref oldBackTarget2, ref _backTarget2);
        if (!InitializeUpdateSteps())
            yield break;

        ComputeData cdata;
        InitComputeData(out cdata);

        for (int i = cdata.numCascades - 1; i >= 0; i--)
        {
            ComputeCascade(i, cdata);
            if (i > 0)
                yield return null;
        }

        FinalizeUpdateSteps(cdata);
    }

    public void UpdateShadowsFull()
    {
        _auto_incr_cascade = null;
        if (!InitializeUpdateSteps())
            return;

        ComputeData cdata;
        InitComputeData(out cdata);
        for (int i = cdata.numCascades - 1; i >= 0; i--)
            ComputeCascade(i, cdata);
        FinalizeUpdateSteps(cdata);
    }

    bool InitializeUpdateSteps()
    {
        if (!UpdateRenderTexture())
            return false;

        SetUpShadowCam();
        _shadowCam.targetTexture = _target;

        if (useDitheringForTransparent) Shader.EnableKeyword("VSM_DRAW_TRANSPARENT_SHADOWS");
        else Shader.DisableKeyword("VSM_DRAW_TRANSPARENT_SHADOWS");

        _blur_material.SetVector("BlurPixelSize", new Vector2(1f / _resolution, 1f / _resolution));
        return true;
    }

    void ComputeCascade(int lvl, ComputeData cdata)
    {
        UpdateShadowCameraPos(cdata.trackTransform);

        _shadowCam.orthographicSize = cdata.firstCascadeLevelSize * Mathf.Pow(2, lvl);
        _shadowCam.RenderWithShader(_depthShader, onlyOpaqueCasters ? "RenderType" : "");

        float y1 = lvl / (float)cdata.numCascades;
        float y2 = (lvl + 1) / (float)cdata.numCascades;
        _blur_material.DisableKeyword("BLUR_NOTHING");
        _blur_material.EnableKeyword("BLUR_LINEAR_PART");
        CustomBlit(_target, _backTarget1, _blur_material, y1, y2);

        _blur_material.DisableKeyword("BLUR_LINEAR_PART");
        CustomBlit(_target, _backTarget2, _blur_material, y1, y2);
    }

    void FinalizeUpdateSteps(ComputeData cdata)
    {
        _blur_material.EnableKeyword("BLUR_NOTHING");
        CustomBlit(_target, _backTarget1, _blur_material, 1f - 1f / _backTarget1.height, 1f);
        CustomBlit(_target, _backTarget2, _blur_material, 1f - 1f / _backTarget2.height, 1f);

        UpdateShaderValues(cdata);
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

    void SetUpShadowCam()
    {
        if (_shadowCam == null)
        {
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
            _shadowCam.aspect = 1;

            if (_blur_material == null)
                _blur_material = new Material(blurShader);
            _blur_material.SetColor("_Color", _shadowCam.backgroundColor);
        }

        /* Set up the clip planes so that we store depth values in the range [-0.5, 0.5],
         * with values near zero being near us even if depthOfShadowRange is very large.
         * This maximizes the precision in the RHalf textures near us. */
        _shadowCam.nearClipPlane = -depthOfShadowRange;
        _shadowCam.farClipPlane = depthOfShadowRange;
        _shadowCam.cullingMask = cullingMask;
    }

    void UpdateShaderValues(ComputeData cdata)
    {
        // Set the qualities of the textures
        Shader.SetGlobalTexture("VSM_ShadowTex1", _backTarget1);
        Shader.SetGlobalTexture("VSM_ShadowTex2", _backTarget2);
        Shader.SetGlobalFloat("VSM_DeltaExtraDistance", deltaExtraDistance);
        Shader.SetGlobalFloat("VSM_InvNumCascades", 1f / cdata.numCascades);

        Vector3 size;
        size.y = _shadowCam.orthographicSize * 2;
        size.x = _shadowCam.aspect * size.y;
        size.z = _shadowCam.farClipPlane - _shadowCam.nearClipPlane;

        size.x = 1f / size.x;
        size.y = 1f / size.y;
        size.z = 128f / size.z;

        var mat = _shadowCam.transform.worldToLocalMatrix;
        Shader.SetGlobalMatrix("VSM_LightMatrix", Matrix4x4.Scale(size) * mat);
        Shader.SetGlobalMatrix("VSM_LightMatrixNormal", Matrix4x4.Scale(Vector3.one * 1.2f / _backTarget1.width) * mat);
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
            _target = CreateTarget();
        }
        if (_backTarget1 == null)
        {
            _backTarget1 = CreateBackTarget();
            _backTarget2 = CreateBackTarget();
        }
        return true;
    }

    void UpdateShadowCameraPos(Transform trackTransform)
    {
        if (!trackTransform)
            return;
        Camera cam = _shadowCam;
        cam.transform.position = trackTransform.position;
        cam.transform.rotation = Quaternion.LookRotation(trackTransform.forward, Vector3.up);
    }

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
        tg.filterMode = _filterMode;
        tg.wrapMode = TextureWrapMode.Clamp;
        tg.Create();
        return tg;
    }

    // Swap Elements A and B
    void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }
}
