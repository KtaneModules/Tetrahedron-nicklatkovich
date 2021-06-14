using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TetrahedronModule : MonoBehaviour {
	private const float EDGE_LENGTH = 0.0643f;
	private static readonly Vector3[] BASE_NODES_POSITIONS = new Vector3[] {
		new Vector3(0, 0.0275f, 0.06f),
		new Vector3(-0.0693f, 0.0275f, -0.06f),
		new Vector3(0.0693f, 0.0275f, -0.06f),
	};

	private static int moduleIdCounter = 1;

	public TextMesh Display;
	public KMSelectable Selectable;
	public KMBombModule Module;
	public KMBossModule BossModule;
	public KMBombInfo BombInfo;
	public KMAudio Audio;
	public NodeComponent NodePrefab;
	public EdgeComponent EdgePrefab;

	private bool stageActive;
	private bool playActivationSound;
	private int moduleId;
	private int startingTimeInMinutes;
	private int passedStagesCount = 0;
	private int stagesCount;
	private int pathLength;
	private int registeredSolvesCount = 0;
	private string currentStagePathExample = null;
	private string currentPath;
	private HashSet<string> pathesExamples;
	private HashSet<string> usedPathes = new HashSet<string>();
	private HashSet<TetrahedronModule> otherTetrahedrons;
	private NodeComponent[] baseNodes;
	private NodeComponent startNode;
	private EdgeComponent[] baseEdges = new EdgeComponent[3];
	private EdgeComponent[] startEdges = new EdgeComponent[3];
	private HashSet<string> ignoredModules;

	private NodeComponent GetNodeById(char id) {
		return id == 'd' ? startNode : baseNodes[id - 'a'];
	}

	private void Start() {
		moduleId = moduleIdCounter++;
		baseNodes = BASE_NODES_POSITIONS.Select(pos => InstantiateNode(pos)).ToArray();
		startNode = InstantiateNode(new Vector3(0, 0.1075f, -0.02f));
		for (int i = 0; i < baseNodes.Length; i++) {
			NodeComponent from = baseNodes[i];
			NodeComponent to = baseNodes[(i + 1) % baseNodes.Length];
			EdgeComponent edge = Instantiate(EdgePrefab);
			edge.transform.parent = transform;
			edge.transform.localPosition = (from.transform.localPosition + to.transform.localPosition) / 2f;
			edge.transform.localScale = new Vector3(.01f, EDGE_LENGTH, .01f);
			edge.transform.localRotation = Quaternion.LookRotation(to.transform.localPosition - from.transform.localPosition, Vector3.up);
			edge.transform.Rotate(90, 0, 0, Space.Self);
			edge.Id = string.Format("{0}{1}", (char)('a' + i), (char)('a' + (i + 1) % 3));
			baseEdges[i] = edge;
			edge = Instantiate(EdgePrefab);
			edge.transform.parent = transform;
			edge.transform.localPosition = (from.transform.localPosition + startNode.transform.localPosition) / 2f;
			edge.transform.localScale = new Vector3(.01f, EDGE_LENGTH, .01f);
			edge.transform.localRotation = Quaternion.LookRotation(startNode.transform.localPosition - from.transform.localPosition, Vector3.up);
			edge.transform.Rotate(90, 0, 0, Space.Self);
			edge.Id = "d" + (char)('a' + i);
			startEdges[i] = edge;
		}
		Module.OnActivate += ActivateModule;
	}

	private void ActivateModule() {
		startingTimeInMinutes = Mathf.FloorToInt(BombInfo.GetTime() / 60f);
		Debug.LogFormat("[Tetrahedron #{0}] Starting time in minutes: {1}", moduleId, startingTimeInMinutes);
		int modulesCount = transform.parent.childCount;
		otherTetrahedrons = new HashSet<TetrahedronModule>(Enumerable.Range(0, modulesCount).Select(i => (
			transform.parent.GetChild(i).GetComponent<TetrahedronModule>()
		)).Where(m => m != null && m != this));
		playActivationSound = otherTetrahedrons.All(t => t.otherTetrahedrons == null);
		ignoredModules = new HashSet<string>(BossModule.GetIgnoredModules("Tetrahedron", TetrahedronData.DefaultIgnoredModules));
		List<string> allModules = BombInfo.GetSolvableModuleNames();
		int unignoredModulesCount = allModules.Where(m => !ignoredModules.Contains(m)).Count();
		int tetrahedronModulesCount = allModules.Count(s => s == "Tetrahedron");
		stagesCount = unignoredModulesCount + 1;
		int expectedPathesCount = stagesCount * tetrahedronModulesCount;
		pathesExamples = TetrahedronData.Generate(expectedPathesCount);
		pathLength = pathesExamples.PickRandom().Length;
		for (int i = 0; i < baseNodes.Length; i++) {
			int i_ = i;
			baseNodes[i].Selectable.OnInteract += () => { PressNode((char)('a' + i_)); return false; };
		}
		startNode.Selectable.OnInteract += () => { PressNode('d'); return false; };
		ActivateStage();
	}

	private void Update() {
		int solvedUnignoredModulesCount = BombInfo.GetSolvedModuleNames().Where(s => !ignoredModules.Contains(s)).Count();
		int newSolves = solvedUnignoredModulesCount - registeredSolvesCount;
		if (newSolves == 0) return;
		if (stageActive) {
			for (int i = 0; i < newSolves; i++) {
				Debug.LogFormat("[Tetrahedron #{0}] Other module is solved before input. Strike!", moduleId);
				Module.HandleStrike();
			}
		} else ActivateStage();
		registeredSolvesCount = solvedUnignoredModulesCount;
	}

	private void ActivateStage() {
		if (stageActive) return;
		if (playActivationSound) Audio.PlaySoundAtTransform("TetrahedronActivated", startNode.transform);
		HashSet<string> allUsedPathes = new HashSet<string>(usedPathes.Concat(otherTetrahedrons.SelectMany(t => t.usedPathes)).Concat(otherTetrahedrons.Select(t => (
			t.currentStagePathExample
		))));
		string possiblePath = pathesExamples.Where(p => !allUsedPathes.Contains(p)).PickRandom();
		HashSet<EdgeComponent> usedEdges = new HashSet<EdgeComponent>(PathToEdges(possiblePath));
		HashSet<Color> passableColors = new HashSet<Color>();
		HashSet<Color> impassableColors = new HashSet<Color>();
		foreach (Color color in TetrahedronData.COLORS) {
			if (TetrahedronData.IsPassable(color, BombInfo, startingTimeInMinutes)) passableColors.Add(color);
			else impassableColors.Add(color);
		}
		if (passedStagesCount == 0) {
			Debug.LogFormat("[Tetrahedron #{0}] Passable colors: {1}", moduleId, passableColors.Select(c => TetrahedronData.colorNames[c]).Join(","));
			Debug.LogFormat("[Tetrahedron #{0}] Impassable colors: {1}", moduleId, impassableColors.Select(c => TetrahedronData.colorNames[c]).Join(","));
		}
		Debug.LogFormat("[Tetrahedron #{0}] Stage #{1}:", moduleId, passedStagesCount + 1);
		foreach (EdgeComponent edge in baseEdges.Concat(startEdges)) {
			edge.color = usedEdges.Contains(edge) ? passableColors.PickRandom() : impassableColors.PickRandom();
			Debug.LogFormat("[Tetrahedron #{0}] Edges \"{1}\" is {2}", moduleId, edge.Id, TetrahedronData.colorNames[edge.color.Value]);
		}
		Debug.LogFormat("[Tetrahedron #{0}] Possible path: {1}", moduleId, possiblePath);
		stageActive = true;
		startNode.currentPosition = true;
		currentPath = "";
		UpdateDisplay();
	}

	private void PressNode(char nodeId) {
		if (!stageActive) return;
		char currentNodeId = currentPath.Length == 0 ? 'd' : currentPath.Last();
		if (currentNodeId == nodeId) return;
		GetNodeById(currentNodeId).currentPosition = false;
		currentPath += nodeId;
		EdgeComponent edge = MoveToEdge(currentNodeId, nodeId);
		if (!TetrahedronData.IsPassable(edge.color.Value, BombInfo, startingTimeInMinutes)) {
			Debug.LogFormat("[Tetrahedron #{0}] Trying to use edge \"{1}\" that is {2}. Strike!", moduleId, edge.Id, TetrahedronData.colorNames[edge.color.Value]);
			Module.HandleStrike();
			currentPath = "";
			startNode.currentPosition = true;
		} else if (currentPath.Length == pathLength) {
			Debug.LogFormat("[Tetrahedron #{0}] Submitted path: {1}", moduleId, currentPath);
			bool pathIsValid = true;
			if (usedPathes.Contains(currentPath)) {
				Debug.LogFormat("[Tetrahedron #{0}] Submitted path was used before. Strike!", moduleId);
				pathIsValid = false;
			} else if (currentPath.Last() != 'd') {
				Debug.LogFormat("[Tetrahedron #{0}] Submitted path not ends at \"d\" node. Strike!", moduleId);
				pathIsValid = false;
			} else if (otherTetrahedrons.Any(t => t.usedPathes.Contains(currentPath))) {
				Debug.LogFormat("[Tetrahedron #{0}] Submitted path was already used in other Tetrahedron. Strike!", moduleId);
				pathIsValid = false;
			}
			if (pathIsValid) {
				Debug.LogFormat("[Tetrahedron #{0}] Submitted path is correct", moduleId);
				usedPathes.Add(currentPath);
				passedStagesCount++;
				if (passedStagesCount == stagesCount) {
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
					Debug.LogFormat("[Tetrahedron #{0}] No more stages. Module solved", moduleId);
					Module.HandlePass();
				} else Audio.PlaySoundAtTransform("TetrahedronStagePassed", startNode.transform);
				stageActive = false;
				foreach (EdgeComponent e in baseEdges.Concat(startEdges)) e.color = null;
				if (registeredSolvesCount >= passedStagesCount) ActivateStage();
			} else {
				Module.HandleStrike();
				currentPath = "";
				startNode.currentPosition = true;
			}
		} else {
			NodeComponent pressedNode = GetNodeById(nodeId);
			pressedNode.currentPosition = true;
			Audio.PlaySoundAtTransform("TetrahedronNodePressed", pressedNode.transform);
		}
		UpdateDisplay();
	}

	private NodeComponent InstantiateNode(Vector3 localPosition) {
		NodeComponent node = Instantiate(NodePrefab);
		node.transform.parent = transform;
		node.transform.localPosition = localPosition;
		node.transform.localScale = Vector3.one;
		node.transform.localRotation = Quaternion.identity;
		node.Selectable.Parent = Selectable;
		Selectable.Children = Selectable.Children.Concat(new[] { node.Selectable }).ToArray();
		Selectable.UpdateChildren();
		return node;
	}

	private void UpdateDisplay() {
		Display.text = string.Format("{0} / {1}", stageActive ? currentPath.Length.ToString() : "-", pathLength);
	}

	private EdgeComponent[] PathToEdges(string path) {
		char prevPos = 'd';
		return path.Select(c => {
			EdgeComponent result = MoveToEdge(prevPos, c);
			prevPos = c;
			return result;
		}).ToArray();
	}

	private EdgeComponent MoveToEdge(char from, char to) {
			if (from == 'd') return startEdges[to - 'a'];
			if (to == 'd') return startEdges[from - 'a'];
			char[] sorted = new[] { from, to };
			Array.Sort(sorted);
			if (sorted[0] == 'a') return sorted[1] == 'b' ? baseEdges[0] : baseEdges[2];
			return baseEdges[1];
	}
}
