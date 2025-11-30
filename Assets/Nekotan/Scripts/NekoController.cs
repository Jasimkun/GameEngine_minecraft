using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NekoController : MonoBehaviour
{

    public float movementspeed = 5f;
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ControllPlater()
    {
        float moveHrizontal = Input.GetAxisRaw("Horizontal");
        float movevirtical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(moveHrizontal, 0.0f, movevirtical);

        if (movement != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 0.15f);
            anim.SetInteger("Walk", 1);
        }
        else
        {
            anim.SetInteger("Walk", 0);
        }

        transform.Translate(movement * movementspeed * Time.deltaTime, Space.World);
    }
}
