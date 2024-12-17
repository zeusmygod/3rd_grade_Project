using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OriginAnimations : MonoBehaviour
{
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButton("Vertical"))
        {
            if (Input.GetButtonDown("Jump"))
            {
                animator.SetBool("isVerticalPressed", false);
            }
            else
            {
                animator.SetBool("isVerticalPressed", true);
            }
        }
        else
        {
            animator.SetBool("isVerticalPressed", false);
        }
    }
}
