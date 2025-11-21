using UnityEngine;

public class Explosion : MonoBehaviour
{


    Animator animator;
    BoxCollider2D boxCollider2D;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = this.GetComponent<Animator>();               // Get a reference to the animator component
        boxCollider2D = this.GetComponent<BoxCollider2D>();
    }
/*
    // Update is called once per frame
    void OnTriggerEnter2D(Collider2D collision)
    {
        
        PlayExplosionAnimation(true);
        Destroy(gameObject);

    }


    void PlayExplosionAnimation()
    {
        animator.SetBool(explode)
    }*/
}
