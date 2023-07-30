using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SunAndMoon : MonoBehaviour
{
    public Slider sunMoonSlider;
    public GameObject directionallight, nightLight, reflectionProbe;
    public Material whiteEmissiveOn, blueEmissiveOn, whiteEmissiveOff, blueEmissiveOff,
        redEmissiveOn, redEmissiveOff, porscheEmissiveWhiteOn, porscheEmissiveWhiteOff,
        porscheEmissiveRedOn, porscheEmissiveRedOff;

    public List<GameObject> EmissivePartsWhite, EmissivePartsBlue, EmissivePartsRed,
        porscheEmissiveWhite, porscheEmissiveRed;

    public void DayAndNight()
    {
        directionallight.transform.rotation = Quaternion.Euler(sunMoonSlider.value, 46.2f, -27f);

        if (sunMoonSlider.value >= 185)
        {
            directionallight.SetActive(false);
            nightLight.SetActive(true);
            reflectionProbe.SetActive(false);
            reflectionProbe.SetActive(true);
            TurnOnEmissives(1);

        }
        else
        {
            directionallight.SetActive(true);
            nightLight.SetActive(false);
            reflectionProbe.SetActive(false);
            reflectionProbe.SetActive(true);
            TurnOnEmissives(0);
        }
    }

    public void TurnOnEmissives(int switcher)
    {
        if (CarSelector.carChoice == 0)
        {
            switch (switcher)
            {
                case 0:
                    for (int i = 0; i < EmissivePartsWhite.Count; i++)
                    {
                        EmissivePartsWhite[i].GetComponent<Renderer>().material =
                            whiteEmissiveOff;
                    }
                    for (int i = 0; i < EmissivePartsRed.Count; i++)
                    {
                        EmissivePartsRed[i].GetComponent<Renderer>().material =
                            redEmissiveOff;
                    }
                    for (int i = 0; i < EmissivePartsBlue.Count; i++)
                    {
                        EmissivePartsBlue[i].GetComponent<Renderer>().material =
                            blueEmissiveOff;
                    }
                    break;

                case 1:
                    for (int i = 0; i < EmissivePartsWhite.Count; i++)
                    {
                        EmissivePartsWhite[i].GetComponent<Renderer>().material =
                            whiteEmissiveOn;
                    }
                    for (int i = 0; i < EmissivePartsRed.Count; i++)
                    {
                        EmissivePartsRed[i].GetComponent<Renderer>().material =
                            redEmissiveOn;
                    }
                    for (int i = 0; i < EmissivePartsBlue.Count; i++)
                    {
                        EmissivePartsBlue[i].GetComponent<Renderer>().material =
                            blueEmissiveOn;
                    }
                    break;

                default:
                    break;
            }
        }
        else
        {
            switch (switcher)
            {
                case 0:
                    Material[] materialsOff1 = porscheEmissiveWhite[0].
                        GetComponent<Renderer>().materials;
                    materialsOff1[1] = porscheEmissiveWhiteOff;
                    porscheEmissiveWhite[0].GetComponent<Renderer>().materials = materialsOff1;
                    Material[] materialsOff2 = porscheEmissiveWhite[1].
                        GetComponent<Renderer>().materials;
                    materialsOff2[0] = porscheEmissiveWhiteOff;
                    porscheEmissiveWhite[1].GetComponent<Renderer>().materials =
                        materialsOff2;
                    porscheEmissiveRed[0].GetComponent<Renderer>().material =
                        porscheEmissiveRedOff;
                    break;
                case 1:
                    Material[] materialsOn = porscheEmissiveWhite[0].
                        GetComponent<Renderer>().materials;
                    materialsOn[1] = porscheEmissiveWhiteOn;
                    porscheEmissiveWhite[0].GetComponent<Renderer>().materials = materialsOn;
                    Material[] materialsOn2 = porscheEmissiveWhite[1].
                        GetComponent<Renderer>().materials;
                    materialsOn2[0] = porscheEmissiveWhiteOn;
                    porscheEmissiveWhite[1].GetComponent<Renderer>().materials =
                        materialsOn2;
                    porscheEmissiveRed[0].GetComponent<Renderer>().material =
                        porscheEmissiveRedOn;
                    break;
                default:
                    break;
            }
        }
        
    }



}
