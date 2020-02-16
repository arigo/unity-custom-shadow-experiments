using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ManualFromScript : MonoBehaviour
{
    IEnumerator Start()
    {
        Quaternion render_pos, display_pos;

        display_pos = Quaternion.LookRotation(Vector3.forward + Vector3.down);
        float xx = 0;

        while (true)
        {
            for (int i = 0; i < 5; i++)
            {
                GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, display_pos);
                yield return null;
            }

            xx += 0.217f;
            render_pos =
                Quaternion.Lerp(
                    Quaternion.LookRotation(Vector3.forward),
                    Quaternion.LookRotation(Vector3.down),
                    xx % 0.8f) *
                Quaternion.Lerp(
                    Quaternion.LookRotation(Vector3.forward),
                    Quaternion.LookRotation(Vector3.right),
                    xx % 0.9f);


            var a = GetComponent<ShadowVSM>().UpdateShadowsIncrementalCascade();
            GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, render_pos);
            while (a.MoveNext())
            {
                GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, display_pos);
                yield return null;
                GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, render_pos);
            }



            display_pos = render_pos;
            //GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, display_pos, update: true);
        }
    }
}
