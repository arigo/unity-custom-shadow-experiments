using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ManualFromScript : MonoBehaviour
{
    IEnumerator Start()
    {
        Quaternion render_pos, display_pos;

        display_pos = Quaternion.LookRotation(Vector3.forward + Vector3.down);

        while (true)
        {
            yield return new WaitForSeconds(2.25f);

            render_pos =
                Quaternion.Lerp(
                    Quaternion.LookRotation(Vector3.forward),
                    Quaternion.LookRotation(Vector3.down),
                    Random.Range(0.1f, 0.9f));


            var a = GetComponent<ShadowVSM>().UpdateShadowsIncrementalCascade();
            GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, render_pos);
            while (a.MoveNext())
            {
                GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, display_pos);
                yield return null;
                GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, render_pos);
            }



            display_pos = render_pos;
            GetComponent<ShadowVSM>().SetShadowCameraPosition(Vector3.up, display_pos);
        }
    }
}
