using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    public TMPro.TMP_Dropdown dropDownColor;
    public List<Material> materials;
    public List<GameObject> autoPartsSUV;
    public List<GameObject> autoPartsPorsche;
    public GameObject colorPicker;
    public GameObject reflectionProbe;

    public void ChangeColor()
    {
        switch (dropDownColor.value)
        {
            case 0:
                Painter(0);
                colorPicker.SetActive(false);
                break;
            case 1:
                Painter(1);
                colorPicker.SetActive(false);
                break;
            case 2:
                Painter(2);
                colorPicker.SetActive(false);
                break;
            case 3:
                Painter(3);
                colorPicker.SetActive(false);
                break;
            case 4:
                Painter(4);
                colorPicker.SetActive(false);
                break;
            case 5:
                Painter(5);
                colorPicker.SetActive(false);
                break;
            case 6:
                Painter(6);
                colorPicker.SetActive(true);
                break;
            default:
                break;
        }
    }

    public void Painter(int id)
    {
        if (CarSelector.carChoice == 0)
        {
            for (int i = 0; i < autoPartsSUV.Count; i++)
            {
                autoPartsSUV[i].GetComponent<MeshRenderer>().material = materials[id];
            }
            reflectionProbe.SetActive(false);
            reflectionProbe.SetActive(true);
        }
        else
        {
            autoPartsPorsche[0].GetComponent<MeshRenderer>().material = materials[id];
            reflectionProbe.SetActive(false);
            reflectionProbe.SetActive(true);
        }
        
    }
}
