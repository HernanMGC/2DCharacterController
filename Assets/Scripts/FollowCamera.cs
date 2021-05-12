using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
	public Transform target;
	public float lag = 5f;
	public Vector2 cameraOffset = new Vector2(-2,2);

    void LateUpdate()
    {		
    		float dir = 1;
    		if (target.gameObject.GetComponent<PlayerController2D>() != null) {
    		//	dir = target.gameObject.GetComponent<PlayerController2D>().GetMovementDir();
    		}

    		Vector3 targetPosition = target.transform.position + new Vector3(cameraOffset.x * dir, cameraOffset.y, 0);
    		Vector3 currentPosition = Vector3.Lerp(transform.position, targetPosition, lag * Time.deltaTime);
    		transform.position = new Vector3(currentPosition.x,currentPosition.y, transform.position.z);

    }
}
