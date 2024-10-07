using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeDisabler : MonoBehaviour
{
    public void Disable()
    {
        transform.parent.gameObject.SetActive(false);
    }
}
