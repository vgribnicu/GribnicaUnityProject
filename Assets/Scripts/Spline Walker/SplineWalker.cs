
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplineWalker : MonoBehaviour
{
	public bool inputEnabled = true;
	public Vector3 target;
	public BezierSpline spline;

	public float scrollSensitivity = 0.75f;

	public Slider slider;

	public bool lookForward;

	public SplineWalkerMode mode;

	private float progress;
	private bool goingForward = true;

	//Zoom [0f, 1f]
	public float zoom = 0f;
	public float zoom01 = 0f;
	public float relZoom01 = 0f;

	//Zoom Y
	public float yMin = 0f;
	public float yMax = 0f;
	public float yPrev = 0f;
	
	public float startAngle;
	public float endAngle;

	public float goalProgress = 0f;
	private Quaternion goalRotation;

	public Camera mainCamera;
	
	private float startPos;
	private float lerpSpeed;
	private float start = 0.6f;
	private float end = 0.8f;

	private bool isMobile;
	private string OSName = "OS";
	private string browserName = "Browser";
	private Vector3 _mousePos;

#if !UNITY_EDITOR && UNITY_WEBGL
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern bool IsMobile();
	
		[System.Runtime.InteropServices.DllImport("__Internal")]
        static extern string GetOSName();
    
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern string GetBrowserName();
#endif

	void Start()
	{
#if !UNITY_EDITOR && UNITY_WEBGL
            isMobile = IsMobile();
            OSName = GetOSName();
            browserName = GetBrowserName();

            if (!isMobile && browserName.Equals("Chrome"))
            {
	            if (OSName.Equals("Windows OS"))
	            {
		            scrollSensitivity = 2f;
	            } else if (OSName.Equals("Macintosh"))
	            {
		            scrollSensitivity = 0.05f;
	            }
            }
#endif
	slider.onValueChanged.AddListener(delegate {ScrollSensitivity(slider.value); });
	ScrollSensitivity(slider.value);

	}
	void Update()
	{
		if (isMobile)
		{
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);

				if (touch.phase == TouchPhase.Began)
				{
					startPos = touch.position.y;
				}

				if (touch.phase == TouchPhase.Moved)
				{
					zoom += 7 * (touch.position.y - startPos) / Screen.height;
					startPos = touch.position.y;
				}
			}
		}
		else if (inputEnabled)
		{
			float k = 0;
			if (Input.GetAxis("Mouse ScrollWheel") != 0)
			{
				k = Input.GetAxis("Mouse ScrollWheel");
			}
			else if (Input.GetAxis("Vertical") != 0)
			{
				k = Input.GetAxis("Vertical") * 0.04f;
			}
			
			zoom += k  * scrollSensitivity;
		}
		zoom = Mathf.Clamp(zoom, 0f, 32.5f);
		zoom01 = zoom / 32.5f;
		zoom01 = Mathf.Clamp01(zoom01);
		
		relZoom01 =  Mathf.InverseLerp(yPrev, yMax, goalProgress);
		
		lerpSpeed = Time.deltaTime * 0.95f;

		progress = Mathf.Lerp(progress, goalProgress, lerpSpeed);
		var prog = Mathf.Lerp(yMin, yMax, zoom01);
		
		goalProgress = prog;
		
		Vector3 position = spline.GetPoint(progress);
		transform.localPosition = position;

		

		if (lookForward)
		{
			_mousePos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
			target.x = LerpMousePos(_mousePos.x);
			target.y = LerpMousePos(_mousePos.y);
			target = mainCamera.transform.InverseTransformDirection(target);
			target.z = 0;
			Quaternion targetRotation = Quaternion.LookRotation( spline.GetDirection(progress) + position - transform.position + target);
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lerpSpeed);
		}
		else
		{
			transform.LookAt( target);
		}
	}

	public float LerpMousePos(float pos)
	{
		 _mousePos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
		 return  Mathf.Lerp(-1f, 1f,  Mathf.InverseLerp(0, 1, pos));
	}
	
	public void ScrollSensitivity (float value)
	{
		scrollSensitivity = value;
	}

	public void SetLookForward()
	{
		lookForward = true;
	}
}