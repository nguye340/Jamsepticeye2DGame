using UnityEngine;

public class BouncyCorpse : MonoBehaviour
{
    public float bounceForce = 5f;
    public float horizontalForce = 2f;
    public float torque = 50f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Random horizontal direction
        float direction = Random.value > 0.5f ? 1f : -1f;

        // Apply an initial force and spin
        rb.linearVelocity = new Vector2(direction * horizontalForce, bounceForce);
        rb.AddTorque(Random.Range(-torque, torque));
    }
}
