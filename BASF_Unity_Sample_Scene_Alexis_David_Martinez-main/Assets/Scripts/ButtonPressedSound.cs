using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonPressedSound : MonoBehaviour
{
    public AudioSource buttonPressedSound;

    public void Playsound()
    {
        buttonPressedSound.Play();
    }
}
