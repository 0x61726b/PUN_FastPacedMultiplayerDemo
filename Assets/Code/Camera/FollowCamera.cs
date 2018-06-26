using System.Collections;
using System.Collections.Generic;
using Assets.Code.Networking;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public float dampTime = 0.15f;
    private Vector3 _velocity = Vector3.zero;

    public Transform Target;

    void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float cameraSize = 5.0f;
        if (Target)
        { 
            Vector3 targetPos = Target.position;

            Vector3 point = Camera.main.WorldToViewportPoint(targetPos);
            Vector3 delta = targetPos - Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z));
            Vector3 destination = transform.position + delta;
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref _velocity, dampTime);

            float sizeVelocity = 0.0f;
            Camera.main.orthographicSize = Mathf.SmoothDamp(Camera.main.orthographicSize, cameraSize, ref sizeVelocity, 0.5f);
        }

    }
}
