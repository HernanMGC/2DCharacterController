using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{	
	[Header("Camera settings")]
    [Tooltip("Target the camera fill follow.")]
	public Transform target;
    [Tooltip("Camera follow lag.")]
	public float lag = 5f;
    [Tooltip("Camera positioning offset.")]
	public Vector2 cameraOffset = new Vector2(-2,2);

    void LateUpdate()
    {		
		Vector3 targetPosition = target.transform.position + new Vector3(cameraOffset.x, cameraOffset.y, 0);
		Vector3 currentPosition = Vector3.Lerp(transform.position, targetPosition, lag * Time.deltaTime);
		transform.position = new Vector3(currentPosition.x,currentPosition.y, transform.position.z);

    }
}
