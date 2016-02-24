using UnityEngine;
using System.Collections;

public class TowerHighlighter : MonoBehaviour {

  private Color startcolor;
  private MeshRenderer MyRenderer;

  // Use this for initialization
  void Start () {
    MyRenderer = GetComponentInChildren<MeshRenderer>();
  }

  
  void OnMouseEnter()
  {
    startcolor = MyRenderer.material.color;
    MyRenderer.material.color = Color.yellow;
  }
  void OnMouseExit()
  {
    MyRenderer.material.color = startcolor;
  }
}
