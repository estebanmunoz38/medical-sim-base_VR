using UnityEngine;

public class Limiter : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Removable")
        {
            //Destroy(other.gameObject);
            other.gameObject.SetActive(false);
        }
    }
}
