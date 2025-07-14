using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObjectFrog : MonoBehaviour
{
	[Tooltip("How close the mouse must be to grab the object")] 
	public float grabRadius = 1.0f;

	[Tooltip("How fast the frog lerps to sit position")] 
	public float sitLerpSpeed = 5f;

	TargetJoint2D joint;
	bool isSitting = false;
	Vector3 sitPosition;

	public void Sit()
	{
		isSitting = true;
		sitPosition = transform.position;
		if (joint)
		{
			Destroy(joint);
			joint = null;
		}
	}

	public void Stand()
	{
		isSitting = false;
	}

	void Update()
	{
		if (isSitting)
		{
			// Move toward sit position using Rigidbody2D velocity (physics-friendly)
			var rb = GetComponent<Rigidbody2D>();
			if (rb != null)
			{
				Vector2 toSit = (Vector2)(sitPosition - transform.position);
				float sitSpeed = sitLerpSpeed; // Use sitLerpSpeed as a velocity multiplier
				rb.linearVelocity = toSit * sitSpeed;
			}
			if (joint)
			{
				Destroy(joint);
				joint = null;
			}
			ApplyGroundForce();
			return;
		}

		if (!Input.GetMouseButton(0))
		{
			if (joint)
			{
				Destroy(joint);
				joint = null;
			}

			// Still apply ground force when not dragging
			ApplyGroundForce();
			return;
		}

		Vector2 pos = TransparentWindow.Camera.ScreenToWorldPoint(Input.mousePosition);

		if (joint)
		{
			joint.target = pos;
			// Still apply ground force while dragging
			ApplyGroundForce();
			return;
		}

		if (!Input.GetMouseButtonDown(0))
		{
			// Still apply ground force
			ApplyGroundForce();
			return;
		}

		// Only check distance to this object's transform
		float dist = Vector2.Distance(pos, transform.position);
		if (dist > grabRadius)
		{
			// Still apply ground force
			ApplyGroundForce();
			return;
		}

		var attachedRigidbody = GetComponent<Rigidbody2D>();
		if (!attachedRigidbody)
		{
			// Still apply ground force
			ApplyGroundForce();
			return;
		}

		joint = attachedRigidbody.gameObject.AddComponent<TargetJoint2D>();
		joint.autoConfigureTarget = false;
		joint.anchor = attachedRigidbody.transform.InverseTransformPoint(pos);
		// Still apply ground force
		ApplyGroundForce();
	}

	void ApplyGroundForce()
	{
		// This logic should match your previous ground force logic
		// (You may want to move your ground force code here)
	}

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
		Gizmos.DrawSphere(transform.position, grabRadius);
	}
#endif
} 