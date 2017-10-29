using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotating : MonoBehaviour {
    public int centerX = 1024;
    public int centerY = 1024;
	// Use this for initialization
	void Start () {
        this.transform.position = new Vector3(centerX, 800, -256);
	}

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(new Vector3(centerX, 0, centerY), Vector3.up, 30 * Time.deltaTime);
    }

}
