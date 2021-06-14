using UnityEngine;

public class NodeComponent : MonoBehaviour {
	public Renderer Renderer;
	public KMSelectable Selectable;

	private bool _currentPosition;
	public bool currentPosition {
		get { return _currentPosition; }
		set { _currentPosition = value; UpdateColor(); }
	}

	private void Start() {
		currentPosition = false;
	}

	private void UpdateColor() {
		Renderer.material.color = currentPosition ? Color.green : Color.white;
	}
}
