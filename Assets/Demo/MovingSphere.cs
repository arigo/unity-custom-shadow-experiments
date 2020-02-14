using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    // Start is called before the first frame update
    IEnumerator Start()
    {
        float ymin = transform.position.y;
        float ymax = ymin + 2f;
        while (true)
        {
            while (transform.position.y < ymax)
            {
                transform.position += Vector3.up * Time.deltaTime * 10;
                yield return null;
            }
            while (transform.position.y > ymin)
            {
                transform.position -= Vector3.up * Time.deltaTime * 10;
                yield return null;
            }
        }
    }
}
