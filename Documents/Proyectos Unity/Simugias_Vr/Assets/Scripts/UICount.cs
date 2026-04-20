using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UICount : MonoBehaviour
{
    [SerializeField] string prefix = "count";
    [SerializeField] string sufix = "/23";
    public int count = 0;
    public Text countLabel;

    public void Sum()
    {
        count++;
        countLabel.text = prefix + count + sufix;
    }
}