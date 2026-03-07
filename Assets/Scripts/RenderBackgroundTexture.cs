using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RenderBackgroundTexture : MonoBehaviour {

	void Start() 
	{
		Graphic uiGraphic = GetComponent<Image>();
		if (uiGraphic == null) {
			uiGraphic = GetComponent<RawImage>();
		}

		if (uiGraphic == null) {
			Debug.LogWarning("RenderBackgroundTexture requires an Image or RawImage component.", this);
			return;
		}

		uiGraphic.transform.position = new Vector3(-Screen.width / 2.0f, -Screen.height / 2.0f, Screen.width);
	}
}
