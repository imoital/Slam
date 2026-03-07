using UnityEngine;

public class WavingFlag : MonoBehaviour
{
	[Header("Target")]
	public MeshFilter targetMeshFilter;
	public Mesh sourceMeshOverride;

	[Header("Wave")]
	public float amplitude = 0.08f;
	public float speed = 2.0f;
	public float frequency = 4.0f;
	public float flutter = 0.02f;
	[Range(1.0f, 6.0f)]
	public float edgeStiffness = 2.5f;

	private Mesh runtimeMesh;
	private Mesh sourceMesh;
	private Vector3[] baseVertices;
	private Vector3[] deformedVertices;

	void OnEnable()
	{
		if (!Application.isPlaying) return;
		EnsureMesh();
		ApplyWave(GetTimeValue());
	}

	void OnDisable()
	{
		ReleaseRuntimeMesh();
	}

	void OnValidate()
	{
		amplitude = Mathf.Max(0f, amplitude);
		speed = Mathf.Max(0f, speed);
		frequency = Mathf.Max(0f, frequency);
		flutter = Mathf.Max(0f, flutter);
		edgeStiffness = Mathf.Max(1f, edgeStiffness);
	}

	void Update()
	{
		if (!Application.isPlaying) return;
		if (!EnsureMesh()) return;
		ApplyWave(GetTimeValue());
	}

	bool EnsureMesh()
	{
		if (targetMeshFilter == null) {
			targetMeshFilter = GetComponentInChildren<MeshFilter>();
		}

		if (targetMeshFilter == null || targetMeshFilter.sharedMesh == null) {
			return false;
		}

		if (runtimeMesh != null && baseVertices != null) {
			if (targetMeshFilter.sharedMesh != runtimeMesh) {
				targetMeshFilter.sharedMesh = runtimeMesh;
			}
			return true;
		}

		sourceMesh = sourceMeshOverride != null ? sourceMeshOverride : targetMeshFilter.sharedMesh;
		if (sourceMesh == null) {
			return false;
		}

		if (sourceMesh.name.Contains("(WavingFlag)")) {
			Debug.LogWarning("WavingFlag needs the original imported mesh, not a generated '(WavingFlag)' mesh. Reassign the original mesh or a fresh FlagCloth instance.", this);
			return false;
		}

		runtimeMesh = Instantiate(sourceMesh);
		runtimeMesh.name = sourceMesh.name + " (WavingFlag)";
		targetMeshFilter.sharedMesh = runtimeMesh;

		baseVertices = runtimeMesh.vertices;
		deformedVertices = new Vector3[baseVertices.Length];
		return true;
	}

	void ApplyWave(float timeValue)
	{
		if (runtimeMesh == null || baseVertices == null) return;

		float minX = float.MaxValue;
		float maxX = float.MinValue;
		for (int i = 0; i < baseVertices.Length; i++) {
			minX = Mathf.Min(minX, baseVertices[i].x);
			maxX = Mathf.Max(maxX, baseVertices[i].x);
		}

		float width = Mathf.Max(0.0001f, maxX - minX);

		for (int i = 0; i < baseVertices.Length; i++) {
			Vector3 vertex = baseVertices[i];
			float edgeFactor = 1f - Mathf.Clamp01((vertex.x - minX) / width);
			edgeFactor = Mathf.Pow(edgeFactor, edgeStiffness);

			float primaryWave = Mathf.Sin((vertex.x * frequency) + (timeValue * speed));
			float secondaryWave = Mathf.Sin((vertex.y * (frequency * 0.7f)) + (timeValue * (speed * 1.7f)));
			float zOffset = (primaryWave * amplitude + secondaryWave * flutter) * edgeFactor;

			deformedVertices[i] = new Vector3(vertex.x, vertex.y, vertex.z + zOffset);
		}

		runtimeMesh.vertices = deformedVertices;
		runtimeMesh.RecalculateNormals();
		runtimeMesh.RecalculateBounds();
	}

	float GetTimeValue()
	{
		return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
	}

	void ReleaseRuntimeMesh()
	{
		if (targetMeshFilter != null && runtimeMesh != null) {
			targetMeshFilter.sharedMesh = sourceMeshOverride != null ? sourceMeshOverride : sourceMesh;
		}

		if (runtimeMesh != null) {
			if (Application.isPlaying) {
				Destroy(runtimeMesh);
			} else {
				DestroyImmediate(runtimeMesh);
			}
		}

		runtimeMesh = null;
		sourceMesh = null;
		baseVertices = null;
		deformedVertices = null;
	}
}
