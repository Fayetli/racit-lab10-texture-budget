using UnityEngine;

// Coin or key: spins and bobs; collected by GameManager.
public class Pickup : MonoBehaviour
{
    public enum Kind { Coin, Key }
    public Kind kind;
    float baseY;

    void Start() { baseY = transform.position.y; }

    void Update()
    {
        transform.Rotate(0, 120f * Time.deltaTime, 0, Space.World);
        var p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * 2f + p.x) * 0.1f;
        transform.position = p;
    }
}
