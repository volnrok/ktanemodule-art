using UnityEngine;
using System;
using System.Collections;

public class ArtModule : MonoBehaviour {

	// Access with [hue, value]
	private static Color[,] Palette = {
		// Black / Gray / White
		{new Color(0.15f,0.15f,0.15f),new Color(0.5f,0.5f,0.5f),new Color(1.0f,1.0f,1.0f)},
		// Dark red / Red / Pink
		{new Color(0.5f,0.1f,0.1f),new Color(1.0f,0.1f,0.1f),new Color(1.0f,0.65f,0.65f)},
		// Brown / Orange / Tan
		{new Color(0.5f,0.28f,0.1f),new Color(1.0f,0.5f,0.1f),new Color(1.0f,0.8f,0.5f)},
		// Yellow
		{new Color(1.0f,0.5f,0.1f),new Color(1.0f,0.9f,0.1f),new Color(1.0f,0.95f,0.5f)},
		// Green
		{new Color(0.1f,0.1f,0.1f),new Color(0.5f,0.5f,0.5f),new Color(1.0f,1.0f,1.0f)},
		// Blue
		{new Color(0.1f,0.1f,0.1f),new Color(0.5f,0.5f,0.5f),new Color(1.0f,1.0f,1.0f)},
		 // Purple
		{new Color(0.1f,0.1f,0.1f),new Color(0.5f,0.5f,0.5f),new Color(1.0f,1.0f,1.0f)}
	};

	private static int[] BrushSizes = {
		60,
		24,
		12
	};

	public Transform CanvasRoot;
	public Transform PaintSurface;
	public Collider CollisionSurface;
	public KMSelectable[] Brushes;

	private KMSelectable Selectable;
	private KMBombInfo BombInfo;
	private KMBombModule BombModule;
	private KMAudio Audio;

	private static ArtModule selectedModule;
	private bool isActive = false;
	private bool isComplete = false;
	private bool isSelected = true;
	private bool isSelectingModule = false; // Need to make sure the mouse is released after selecting before paint is enabled

	private Texture2D canvas;
	private Vector2 lastPosition;
	private Vector2 nullPosition = new Vector2 (-1, -1);
	private int brush = 0;
	private int hueIndex = 0;
	private int valueIndex = 1;

	private int moduleId;
	private static int moduleIdCounter = 1;

	private int CanvasWidth = 256;
	private int CanvasHeight = 384;
	//private int CanvasWidth = 64;
	//private int CanvasHeight = 96;
	private Vector2 CanvasSize;

	private Vector3 OutPosition = new Vector3 (0, 0, 0.09f);
	private Vector3 InPosition = new Vector3 (0, 0, 0.07f);
	private float MoveSpeed = 0.15f;

	protected void Start () {
		moduleId = moduleIdCounter++;
		CanvasSize = new Vector2 (CanvasWidth, CanvasHeight);

		Selectable = GetComponent<KMSelectable> ();
		BombInfo = GetComponent<KMBombInfo> ();
		BombModule = GetComponent<KMBombModule> ();
		Audio = GetComponent<KMAudio> ();

		canvas = new Texture2D (CanvasWidth, CanvasHeight, TextureFormat.ARGB32, false);
		canvas.wrapMode = TextureWrapMode.Clamp;
		for (int y = 0; y < CanvasHeight; y++) {
			for (int x = 0; x < CanvasWidth; x++) {
				canvas.SetPixel (x, y, Color.white);
			}
		}
		canvas.Apply ();
		PaintSurface.GetComponent<Renderer> ().material.mainTexture = canvas;

		for (int i = 0; i < Brushes.Length; i++) {
			int j = i;
			Brushes [j].OnInteract += delegate() {
				brush = j;
				return false;
			};
		}

		BombModule.OnActivate += OnActivate;
		Selectable.OnInteract += OnModuleSelect;
		Selectable.OnCancel += OnModuleDeselect;
		foreach (KMSelectable s in Brushes) {
			s.OnCancel += OnModuleDeselect;
		}
		//Selectable.OnCancel = new KMSelectable.OnCancelHandler (this.OnModuleDeselect);

		UpdatePalette (0, 0);
	}

	protected void OnActivate() {
		isActive = true;
	}

	protected bool OnModuleSelect() {
		if (moduleId == 1) {
			Audio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.BombDefused, transform);
		} else {
			Audio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.BombExplode, transform);
		}
		selectedModule = this;
		isSelected = true;
		isSelectingModule = true;
		return true;
	}

	public bool OnModuleDeselect() {
		Audio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.GameOverFanfare, transform);
		isSelected = false;
		return true;
	}

	protected void Update () {
		if (Input.GetMouseButtonUp (0)) {
			isSelectingModule = false;
		}

		if (Input.GetMouseButtonDown (0)) {
			lastPosition = nullPosition;
		}

		if (isSelected && this == selectedModule && !isSelectingModule && Input.GetMouseButton (0)) {
			// Make sure the canvas is facing towards the camera (z direction from canvas)
			if (Vector3.Dot (Camera.main.transform.TransformVector (Vector3.forward), PaintSurface.TransformVector (Vector3.forward)) > 0) {
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit;
				// Do a raycast to the canvas
				if (CollisionSurface.Raycast (ray, out hit, 100)) {
					// Calculate the position and do the paint operation
					Vector2 hitPosition = CanvasRoot.InverseTransformPoint (hit.point);
					if (lastPosition == nullPosition) {
						lastPosition = hitPosition;
					}
					DoPaint (lastPosition, hitPosition);
					lastPosition = hitPosition;
				} else {
					lastPosition = nullPosition;
				}
			}
		}

		for (int i = 0; i < Brushes.Length; i++) {
			if (i == brush) {
				Brushes [i].transform.localPosition = Vector3.Lerp (Brushes [i].transform.localPosition, OutPosition, MoveSpeed);
			} else {
				Brushes [i].transform.localPosition = Vector3.Lerp (Brushes [i].transform.localPosition, InPosition, MoveSpeed);
			}
		}
	}

	protected void DoPaint(Vector2 v0, Vector2 v1) {
		TextureDraw.DrawLine (canvas, Vector2.Scale (v0, CanvasSize), Vector2.Scale (v1, CanvasSize), BrushSizes [brush], Palette [0, 0]);
		canvas.Apply ();
	}

	protected void UpdatePalette(int hueMod, int valueMod) {
		hueIndex = ClampHue (hueIndex + hueMod);
		valueIndex = ClampValue (valueIndex + valueMod);
	}

	protected int ClampHue(int hue) {
		if (hue < 0) {
			return Palette.GetLength (0) - 1;
		} else if (hue >= Palette.GetLength (0)) {
			return 0;
		}
		return hue;
	}

	protected int ClampValue(int value) {
		if (value < 0) {
			return 0;
		} else if (value >= Palette.GetLength (1)) {
			return Palette.GetLength (1) - 1;
		}
		return value;
	}

	protected void ChangeZ(Transform t, float z) {
		// Helper function for vertically positioning buttons
		t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, z);
	}
}
