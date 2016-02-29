using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerHighlighter : MonoBehaviour
{

    private List<Color> myRenderersColors;
    private MeshRenderer[] myRenderers;
    private TDTK.UnitTower unitTowerT;

    // Use this for initialization
    void Start()
    {
        myRenderers = GetComponentsInChildren<MeshRenderer>();
        myRenderersColors = new List<Color>(myRenderers.Length);
        for (int i = 0; i < myRenderers.Length; i++)
        {
            myRenderersColors.Add(myRenderers[i].material.color);
        }

        unitTowerT = GetComponent<TDTK.UnitTower>();
    }


    void OnMouseDown()
    {
        // Debug.Log("OnMouseDown " + unitTowerT.electricityReciever);
        if (unitTowerT.electricityReciever)
        {
            for (int i = 0; i < myRenderers.Length; i++)
            {
                myRenderers[i].material.color = Color.yellow;
            }
        }
    }



    void OnMouseEnter()
    {
        for (int i = 0; i < myRenderers.Length; i++)
        {
            myRenderers[i].material.color = Color.yellow;
        }
    }
    void OnMouseExit()
    {
        if (TDTK.UnitTower.lastBuiltEnergyRecieverTower != null && TDTK.UnitTower.lastBuiltEnergyRecieverTower == unitTowerT)
            return;

        Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < myRenderers.Length; i++)
        {
            myRenderers[i].material.color = myRenderersColors[i];
        }
    }
}