using UnityEngine;

public class EdgeComponent : MonoBehaviour {
	public Renderer Renderer;
	public string Id;

	private Color? _color;
	public Color? color {
		get { return _color; }
		set {
			_color = value;
			if (value != null) targetColor = value.Value;
			else targetColor = Color.black;
		}
	}

	private Color _realColor;
	public Color realColor {
		get { return _realColor; }
		private set {
			_realColor = value;
			UpdateColor();
		}
	}

	private Color targetColor;

	private void Start() {
		// realColor = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
		// targetColor = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
		realColor = Color.black;
		targetColor = Color.black;
	}

	private void Update() {
		float r = realColor.r + .01f * (targetColor.r - realColor.r);
		float g = realColor.g + .01f * (targetColor.g - realColor.g);
		float b = realColor.b + .01f * (targetColor.b - realColor.b);
		realColor = new Color(r, g, b);
		// if (Random.Range(0, 60) == 0 && color == null) targetColor = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
	}

	private void UpdateColor() {
		Renderer.material.color = realColor;
	}
}
