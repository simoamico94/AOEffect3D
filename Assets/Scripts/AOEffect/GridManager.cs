using UnityEngine;

public class GridManager : MonoBehaviour
{
	public GameObject gridObjectPrefab; // Prefab for the game objects in the grid
	public Material lineMaterial; // Material for the line renderer
	public GameObject basePlane;

	public int gridSizeX = 40; // Grid size in X direction
	public int gridSizeY = 40; // Grid size in Y direction

	private GameObject[,] gridObjects; // Matrix to store the game objects in the grid

	void Start()
	{
		CreateGrid();
	}
	 
	void CreateGrid()
	{
		// Initialize the gridObjects matrix
		gridObjects = new GameObject[gridSizeX, gridSizeY];

		// Loop through each grid position and create a game object and line renderer
		for (int x = 0; x < gridSizeX; x++)
		{
			for (int y = 0; y < gridSizeY; y++)
			{
				// Calculate the position for the game object
				Vector3 position = new Vector3(x, 0, y);

				// Instantiate the game object at the calculated position
				GameObject obj = Instantiate(gridObjectPrefab, position, Quaternion.identity);
				obj.transform.parent = transform;
				gridObjects[x, y] = obj; // Store the game object in the matrix

				// Create a line renderer for the grid cell
				LineRenderer lineRenderer = obj.AddComponent<LineRenderer>();
				lineRenderer.material = lineMaterial;
				lineRenderer.startWidth = 0.05f;
				lineRenderer.endWidth = 0.05f;
				lineRenderer.positionCount = 5;

				// Define the points for the line renderer to draw a square
				Vector3[] points = new Vector3[5];
				points[0] = position + new Vector3(-0.5f, 0, -0.5f);
				points[1] = position + new Vector3(-0.5f, 0, 0.5f);
				points[2] = position + new Vector3(0.5f, 0, 0.5f);
				points[3] = position + new Vector3(0.5f, 0, -0.5f);
				points[4] = points[0]; // Close the square

				// Set the points for the line renderer
				lineRenderer.SetPositions(points);
			}
		}

		if(basePlane != null)
		{
			basePlane.transform.localScale = new Vector3(gridSizeX/10,1,gridSizeY/10);
		}
	}

	// Function to get the game object at a specific grid position
	public Transform GetGridPos(int x, int y)
	{
		if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
		{
			return gridObjects[x, y].transform;
		}
		else
		{
			Debug.LogError("Invalid grid position.");
			return null;
		}
	}
}