using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineWalkerController : MonoBehaviour
{
    public List<float> stops = new List<float>();

    public SplineWalker splineWalker;

    public int currentStop = 1;
    // Start is called before the first frame update
    void Start()
    {
        splineWalker.yMax = stops[currentStop];
        splineWalker.yMin = stops[currentStop - 1];
    }

    public void Continue()
    {
        if (currentStop < stops.Count - 1)
        {
            currentStop++;
            splineWalker.yMax = stops[currentStop];
            splineWalker.yPrev = stops[currentStop - 1];
            //splineWalker.yMin = stops[currentStop - 1];
            splineWalker.zoom = (32.5f * stops[currentStop - 1] / stops[currentStop]) +
                                (stops[currentStop] / stops[currentStop - 1] / stops[currentStop]*0.5f);
        }
    }
    
    public void EnterMainScene()
    {
        if (currentStop < stops.Count - 1)
        {
            currentStop++;
            splineWalker.yMax = stops[currentStop];
            splineWalker.yMin = stops[currentStop - 1];
            if (currentStop == stops.Count - 1)
            {
                splineWalker.inputEnabled = false;
                splineWalker.zoom = 32.5f;
            }
        }
    }
}
