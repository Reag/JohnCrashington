using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCube : MonoBehaviour
{
    [SerializeField]
    private float rotateAmount = 1f;
    [SerializeField]
    private Vector3 rotationEuler = Vector3.up;

    private void FixedUpdate()
    {
        transform.Rotate(rotationEuler, rotateAmount);
    }
}
