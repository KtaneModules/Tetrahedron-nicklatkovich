using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public static class TetrahedronData {
	public static readonly Color[] COLORS = new[] {
		Color.black,
		Color.red,
		Color.green,
		Color.blue,
		Color.yellow,
		Color.magenta,
		Color.cyan,
		Color.white,
	};

	public static readonly Dictionary<Color, string> colorNames = new Dictionary<Color, string>() {
		{ Color.black, "black" },
		{ Color.red, "red" },
		{ Color.green, "green" },
		{ Color.blue, "blue" },
		{ Color.yellow, "yellow" },
		{ Color.magenta, "magenta" },
		{ Color.cyan, "cyan" },
		{ Color.white, "white" },
	};

	public static HashSet<string> Generate(int stagesCount) {
		int zd = 1;
		int za = 0;
		HashSet<string> vd = new HashSet<string>();
		HashSet<string> va = new HashSet<string>();
		do {
			int newZd = za * 3;
			za = za * 2 + zd;
			zd = newZd;
			HashSet<string> prevVd = new HashSet<string>(vd);
			vd = new HashSet<string>(va.Select(s => s + 'd'));
			while (vd.Count > stagesCount) vd.Remove(vd.PickRandom());
			// Debug.Log(vd.Join(","));
			if (zd >= stagesCount) return vd;
			va = vd.Count == 0 && va.Count == 0 ? new HashSet<string>(new[] { "a", "b", "c" }) : new HashSet<string>(va.SelectMany(s => (
				new[] { s + 'a', s + 'b', s + 'c' }
			).Where(s1 => s1.Last() != s1[s1.Length - 2]).Concat(prevVd.SelectMany(s2 => new[] { s2 + 'a', s2 + 'b', s2 + 'd' }))));
			while (va.Count > stagesCount) va.Remove(va.PickRandom());
		} while (true);
	}

	public static bool IsPassable(Color color, KMBombInfo bombInfo, int startingTimeInMinutes) {
		if (color == Color.black) return false;
		if (color == Color.red) return bombInfo.GetModuleIDs().Count > 27;
		if (color == Color.green) return bombInfo.GetSerialNumberNumbers().Count() <= 2;
		if (color == Color.blue) return bombInfo.GetIndicators().Count() > 2;
		if (color == Color.yellow) return startingTimeInMinutes % 2 == 1;
		if (color == Color.magenta) return bombInfo.GetBatteryCount() <= 3;
		if (color == Color.cyan) return bombInfo.GetPortPlateCount() % 2 == 0;
		if (color == Color.white) return true;
		throw new System.Exception("Unknown color");
	}

	public static readonly string[] DefaultIgnoredModules = new[] {
		"+",
		"14",
		"42",
		"501",
		"A>N<D",
		"Access Codes",
		"Bamboozling Time Keeper",
		"Black Arrows",
		"Brainf---",
		"Busy Beaver",
		"Cube Synchronization",
		"Don’t Touch Anything",
		"Floor Lights",
		"Forget Any Color",
		"Forget Enigma",
		"Forget Everything",
		"Forget Infinity",
		"Forget It Not",
		"Forget Maze Not",
		"Forget Me Later",
		"Forget Me Not",
		"Forget Perspective",
		"Forget The Colors",
		"Forget Them All",
		"Forget This",
		"Forget Us Not",
		"The Heart",
		"Iconic",
		"Keypad Directionality",
		"Kugelblitz",
		"Multitask",
		"OmegaDestroyer",
		"OmegaForget",
		"Organization",
		"Password Destroyer",
		"Purgatory",
		"RPS Judging",
		"Security Council",
		"Shoddy Chess",
		"Simon Forgets",
		"Simon's Stages",
		"Soulscream",
		"Souvenir",
		"Tallordored Keys",
		"Tetrahedron",
		"The Time Keeper",
		"Timing Is Everything",
		"The Troll",
		"Turn The Key",
		"The Twin",
		"Übermodule",
		"Ultimate Custom Night",
		"The Very Annoying Button",
		"Whiteout",
	};
}
