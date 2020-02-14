=====================================
              ShadowVSM
=====================================

*** Based on https://github.com/gkjohnson/unity-custom-shadow-experiments/ ***


This is a (hopefully) generally useful package that adds VSM shadows to Unity projects,
replacing the default shadows.  Notable improvements:

* It gives reasonable-quality shadows on Oculus Quest (and probably mobile targets
  in general).  On Quest the built-in shadows are always blocky.

* It is based on VSM (Variance Shadow Maps), which gives nicer smoothing.  At the moment
  there is only one simple block-blur done on the shadow map, but the idea is that it is
  possible to do many block-blur passes or more advanced image filtering on it, even
  though this is not possible with the traditional kind of shadow maps.

Limitations: it works only in the traditional pipeline (not HDRP/LDRP), and was only
tested in the forward rendering mode.

How to use:

(1) Likely, you want to completely disable the built-in shadows in Unity.
    (Project Settings -> Quality -> Shadows -> Shadows -> Disable Shadows)

(2) Drop the prefab "ShadowVSM Prefab" into all your scenes, or arrange to have
    it instantiated there, or put it only once and make it DontDestroyOnLoad.

(3) All your materials should use the provided shaders if they are supposed to
    receive shadows (see the "ShadowVSM/Shaders" folder, or the "ShadowVSM" section in
    the list of shader names).

By default all objects with "RenderType" = "Opaque" should cast shadows.  See the
"Limit shadow casters" options in the ShadowVSM prefab.

"Shadow computation" can be changed if you have a situation where the shadowmaps don't
need to be recomputed every frame.  "Automatic Incremental Cascade" will recompute it
incrementally over N frames, where N is the number of cascades (set below).  Or,
"Manual from script" means it will only recompute when you call the methods
ShadowVSM.UpdateShadowsFull() or ShadowVSM.UpdateShadowsIncrementalCascade().
