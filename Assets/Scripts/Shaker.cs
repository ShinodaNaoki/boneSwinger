using UnityEngine;

public class Shaker : MonoBehaviour
{
    public Vector2 amplitude = Vector2.zero; // 振幅
    public Vector2 frequency = Vector2.one; // 周波数

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float x = startPosition.x + Mathf.Sin(Time.time * frequency.x) * amplitude.x;
        float y = startPosition.y + Mathf.Sin(Time.time * frequency.y) * amplitude.y;
        transform.position = new Vector3(x, y, startPosition.z);
    }
}
