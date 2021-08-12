using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineWalkerVR : MonoBehaviour
{
    public BezierSpline spline;
    public Vector3 target;
    public float duration;

    public float progress;
    private float lerpSpeed ;
    private float speed = 1f;
    public float targetSpeed = 1f;
    private Quaternion targetRotation;

    private void Update()
    {
        
        lerpSpeed = Time.deltaTime * 0.85f;
        speed = Mathf.Lerp(speed, targetSpeed, lerpSpeed);
        progress += speed * Time.deltaTime / duration;
        if (progress > 1f)
        {
            progress -= 1f;
        }

        Vector3 position = spline.GetPoint(progress);
        transform.localPosition = position;
        
        if (progress > 0.36f)
        {
            targetRotation = Quaternion.LookRotation( spline.GetDirection(progress) + position - transform.position);
            targetSpeed = 0.25f;
        }
        else
        {
            targetRotation = Quaternion.LookRotation( target  - transform.position);
            targetSpeed = 1f;
        }
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lerpSpeed);
    }
}
