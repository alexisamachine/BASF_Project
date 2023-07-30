using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonalizedColorChange : MonoBehaviour
{
    public FlexibleColorPicker picker;
    public Material personalizedMaterial;
    public GameObject reflectionProbe;

    public void PrintColor()
    {
        Debug.Log(picker.color);
        personalizedMaterial.color = picker.color;
        reflectionProbe.SetActive(false);
        reflectionProbe.SetActive(true);
    }
}
