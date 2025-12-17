using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boom : MonoBehaviour
{
    public float speed = 40f;  //이동 속도

    public float lifeTime = 2f;    //생존 시간 (초)

    // Start is called before the first frame update
    void Start()
    {
        //일정 시간 후 자동 삭제 (메모리 관리)
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        //로컬의 forward 방향(앞)으로 이동
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(3); // 체력 1 감소
            }

            Destroy(gameObject); // 총알 제거
        }
    }
}
