using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private Camera camera;
    private bool moved = false;

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!moved)
        {
            moved = true;
            camera.transform.position = new Vector3 (1f, 1f, 1f);
        }
    }
}
