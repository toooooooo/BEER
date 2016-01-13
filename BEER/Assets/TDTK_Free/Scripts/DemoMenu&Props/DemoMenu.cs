using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using TDTK;

namespace TDTK {

	public class DemoMenu : MonoBehaviour {

		public RectTransform frame;
		
		public List<string> displayedName=new List<string>();
		public List<string> levelName=new List<string>();
		public List<UnityButton> buttonList=new List<UnityButton>();
		
		// Use this for initialization
		void Start (){
			for(int i=0; i<levelName.Count; i++){
				if(i==0) buttonList[0].Init();
				else if(i>0){
					buttonList.Add(buttonList[0].Clone("ButtonStart"+(i+1), new Vector3(0, -i*40, 0)));
				}
				
				buttonList[i].label.text=displayedName[i];
                
            }


            buttonList.Add(buttonList[0].Clone("ButtonQuit", new Vector3(0, -levelName.Count * 40, 0)));
            buttonList[levelName.Count].label.text = "Quit";

            frame.sizeDelta=new Vector2(200, 30+(levelName.Count + 1)*40);
		}
		
		// Update is called once per frame
		void Update () {
		
		}
		
		public void OnStartButton(GameObject butObj){
            if (butObj.name == "ButtonQuit")
            {
                Application.Quit();
            }
            else
            {
                for (int i = 0; i < buttonList.Count; i++)
                {
                    if (buttonList[i].rootObj == butObj)
                    {
                        Application.LoadLevel(levelName[i]);
                    }
                }
            }
		}
		
	}

}