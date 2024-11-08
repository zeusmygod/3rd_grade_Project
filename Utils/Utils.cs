using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static Vector3 GetRandomSpawnPoint()
    {
        return new Vector3(Random.Range(-5,5), 4, Random.Range(-5,5));
    }
    public static Vector3 GetSpawnPointSphere()
    {
        // return new Vector3(13.43319f, 18.73f, -54.58f);
        // ! 待解決
        return new Vector3(-54.45f, 12.73f, -70.81029f);

    }
    public static void SetRenderLayerInChildren(Transform transform, int layerNumber)
    {
        foreach (Transform trans in transform.GetComponentInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber ;
        }
    }
}
