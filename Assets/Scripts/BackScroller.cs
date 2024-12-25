using NaughtyAttributes;
using UnityEngine;

[ExecuteAlways]
public class BackScroller : MonoBehaviour
{

    [SerializeField]
    private GameObject target;

    [SerializeField]
    private Vector2 moveScale;

    [SerializeField]
    private Vector2 offset;

    [SerializeField]
    private Vector2 origin;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 nowPos = transform.position;
        var diff = nowPos - origin;
        var tPos = new Vector2(offset.x + diff.x * moveScale.x, offset.y + diff.y * moveScale.y);
        target.transform.position = tPos;
    }

    [Button("Set origin and offset from current")]
    public void SetOriginAndOffset() {
        origin = transform.position;
        if (target == null)
        {
            offset = Vector3.zero;
            return;
        }
        offset = target.transform.position;
    }
}
