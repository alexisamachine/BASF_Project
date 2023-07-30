// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace AxF2Unity
{
    [CustomEditor(typeof(AxFCPA2ColorManager))]
    public class AxFCPA2ColorManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            AxFCPA2ColorManager component = (AxFCPA2ColorManager)target;

            if(GUILayout.Button("Collect Materials"))
            {
                component.CollectMaterialsFromScene();
            }

            if(GUILayout.Button("Filter Cubemaps"))
            {
                component.UpdateFilteredCubemaps();
            }
        }

    }
}
