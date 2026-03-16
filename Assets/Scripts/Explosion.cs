using UnityEngine;

public class Explosion : MonoBehaviour
{
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void OnEnable()
    {
        Invoke("Disable", 1f);
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }

    public void StartExplosion( string target)
    {
        anim.SetTrigger("OnExplosion");

        switch (target) 
        {
            case "S":
                transform.localScale = Vector3.one * 0.7f;
                break;
            case "P":
            case "M":
                transform.localScale = Vector3.one * 1f;
                break;
            case "L":
                transform.localScale = Vector3.one * 2f;
                break;
            case "B":
                transform.localScale = Vector3.one * 3f;
                break;
        }
    }
}
