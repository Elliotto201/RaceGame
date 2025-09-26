using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform carTransform;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private float positionLerpSpeed = 5f;
    [SerializeField] private float rotationLerpSpeed = 5f;

    void LateUpdate()
    {
        if (carTransform == null) return;

        Vector3 targetPos = carTransform.position + carTransform.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionLerpSpeed);

        Quaternion targetRot = Quaternion.LookRotation(carTransform.position - transform.position) * Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
    }
}
