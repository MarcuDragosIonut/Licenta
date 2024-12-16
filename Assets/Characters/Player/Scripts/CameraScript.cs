using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField]
    private Transform followTarget;

    private void Update()
    {
        transform.position = new Vector3(followTarget.position.x, followTarget.position.y, transform.position.z);
    }
}