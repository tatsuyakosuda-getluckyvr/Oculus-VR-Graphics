using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppSpaceWarpHelper : MonoBehaviour
{
    private void Awake()
    {
        //OVRManager.display.displayFrequency = 90;
    }

    // Start is called before the first frame update
    protected void Start()
    {
        Debug.Log($"spacewarp = {OVRManager.GetSpaceWarp()}");
    }

    // Update is called once per frame
    protected void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.B, OVRInput.Controller.RTouch))
        {
            OVRManager.SetSpaceWarp(!OVRManager.GetSpaceWarp());
            Debug.Log($"spacewarp = {OVRManager.GetSpaceWarp()}");
        }
    }
}
