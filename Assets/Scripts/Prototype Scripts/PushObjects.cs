using UnityEngine;

public class PushObjects : MonoBehaviour
{

    public float pushForce = 2f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.collider.CompareTag("PushableObject"))
            return;

        Rigidbody body = hit.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
            return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        body.AddForce(pushDir * pushForce, ForceMode.Impulse);
    }
}

