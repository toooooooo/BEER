using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerHighlighter : MonoBehaviour
{

  private List<Color> myRenderersColors;
  private MeshRenderer[] myRenderers;

  // Use this for initialization
  void Start()
  {
    myRenderers = GetComponentsInChildren<MeshRenderer>();
    myRenderersColors = new List<Color>(myRenderers.Length);
    for (int i = 0; i < myRenderers.Length; i++)
    {
      myRenderersColors.Add(myRenderers[i].material.color);
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
    for (int i = 0; i < myRenderers.Length; i++)
    {
      myRenderers[i].material.color = myRenderersColors[i];
    }
  }
}
