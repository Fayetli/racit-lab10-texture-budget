using UnityEngine;

// Ground zone (decal): the red splat deals damage, the oil slows the player.
public class Hazard : MonoBehaviour
{
    public enum Kind { Damage, Slow }
    public Kind kind;
    public float radius = 1f;
    public float damagePerSecond = 25f;
    public float slowFactor = 0.45f;

    public bool Contains(Vector3 worldPos)
    {
        var d = worldPos - transform.position; d.y = 0;
        return d.sqrMagnitude <= radius * radius;
    }
}
