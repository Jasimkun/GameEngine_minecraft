using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{

    public CinemachineVirtualCamera virtualCam;    //기본 tps 카메라

    public CinemachineFreeLook freeLookCam;     //자유 회전 tps 카메라

    public bool usingFreeLook = false;

    // Start is called before the first frame update
    void Start()
    {
        //시작은 Virtual Camera 활성화
        virtualCam.Priority = 10;
        freeLookCam.Priority = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))   //우클릭
        {
            usingFreeLook = !usingFreeLook;
            if (usingFreeLook)
            {
                freeLookCam.Priority = 20;    //FreeLook 활성화
                virtualCam.Priority = 0;
            }
            else
            {
                virtualCam.Priority = 20;    //Virtual Camera 활성화
                freeLookCam.Priority = 0;
            }
        }
    }
}
