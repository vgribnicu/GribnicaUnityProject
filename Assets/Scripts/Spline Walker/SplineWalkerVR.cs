using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineWalkerVR : MonoBehaviour
{
    public BezierSpline spline;
    public Vector3 target;
    public float duration;

    public float progress;
    public bool lookForward;
    private float lerpSpeed = 0.7f;

    private void Update()
    {
        progress += Time.deltaTime / duration;
        if (progress > 1f)
        {
            progress = 1f;
        }

        Vector3 position = spline.GetPoint(progress);
        transform.localPosition = position;
        Quaternion targetRotation;
        if (progress > 0.5f)
        {
            targetRotation = Quaternion.LookRotation( spline.GetDirection(progress) + position - transform.position);
        }
        else
        {
          targetRotation = Quaternion.LookRotation( target  - transform.position);
        }
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lerpSpeed);
    }
}
