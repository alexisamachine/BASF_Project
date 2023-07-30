using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSelector : MonoBehaviour
{
    public List<GameObject> carsList;
    public TMPro.TMP_Dropdown dropDownCar;
    public static int carChoice = 0;
    public GameObject reflectionprobe;
    public GameObject colorDropdown, sunMoonSlider;

    public void CarActivator()
    {
        switch (dropDownCar.value)
        {
            case 0:
                carsList[0].SetActive(true);
                carsList[1].SetActive(false);
                reflectionprobe.SetActive(false);
                reflectionprobe.SetActive(true);
                carChoice = 0;
                colorDropdown.GetComponent<TMPro.TMP_Dropdown>().value = 0;
                colorDropdown.GetComponent<ColorChanger>().ChangeColor();
                sunMoonSlider.GetComponent<SunAndMoon>().DayAndNight();
                break;

            case 1:
                carsList[0].SetActive(false);
                carsList[1].SetActive(true);
                reflectionprobe.SetActive(false);
                reflectionprobe.SetActive(true);
                carChoice = 1;
                colorDropdown.GetComponent<TMPro.TMP_Dropdown>().value = 0;
                colorDropdown.GetComponent<ColorChanger>().ChangeColor();
                sunMoonSlider.GetComponent<SunAndMoon>().DayAndNight();
                break;
            default:
                break;
        }


    }
}
