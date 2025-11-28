using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{

    public GameObject projectilePrefab;   //projectile 프리팹

    public GameObject BoomPrefab;

    public Transform firePoint;           //발사 위치 (총구)

    Camera cam;

    private GameObject currentWeaponPrefab; // 현재 선택된 무기

    private bool isBoomMode = false;        // 무기 전환 상태

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;        //메인 카메라 가져오기
        currentWeaponPrefab = projectilePrefab;
    }

    // Update is called once per frame
    void Update()
    {

        // Z 키로 무기 전환
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isBoomMode = !isBoomMode;
            currentWeaponPrefab = isBoomMode ? BoomPrefab : projectilePrefab;
            if(isBoomMode)
            { 
                Debug.Log("폭탄띠");
            }
            else
            {
                Debug.Log("안폭탄");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        //화면에서 마우스 -> 광선(Ray) 쏘기
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;
        targetPoint = ray.GetPoint(50f);
        Vector3 direction = (targetPoint - firePoint.position).normalized;  //방향 벡터

        //Projectile 생성
        GameObject proj = Instantiate(currentWeaponPrefab, firePoint.position, Quaternion.LookRotation(direction));
    }
}
