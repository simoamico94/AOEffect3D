using UnityEngine;

public class GridManager : MonoBehaviour
{
	public GameObject gridObjectPrefab; // Prefab for the game objects in the grid
	public Material lineMaterial; // Material for the line renderer

	public int gridSizeX = 40; // Grid size in X direction
	public int gridSizeZ = 40; // Grid size in Z direction

	private GameObject[,] gridObjects; // Matrix to store the game objects in the grid

	public void CreateGrid(int gridX = 40, int gridZ = 40)
	{
		DestroyAndClearGridObjects();

		gridSizeX = gridX;
		gridSizeZ = gridZ;

		Debug.Log($"Creating new grid {gridSizeX}x{gridSizeZ}");

		// Initialize the gridObjects matrix
		gridObjects = new GameObject[gridSizeX, gridSizeZ];

		// Loop through each grid position and create a game object and line renderer
		for (int x = 0; x < gridSizeX; x++)
		{
			for (int z = 0; z < gridSizeZ; z++)
			{
				// Calculate the position for the game object
				Vector3 position = new Vector3(x, 0, z);

				// Instantiate the game object at the calculated position
				GameObject obj = Instantiate(gridObjectPrefab, position, Quaternion.identity);
				obj.transform.parent = transform;

				obj.name = $"[{x+1},{z+1}]";

				gridObjects[x, z] = obj; // Store the game object in the matrix

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
	}

	// Function to get the game object at a specific grid position
	public Transform GetGridPos(int x, int z)
	{
		if (x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ)
		{
			return gridObjects[x, z].transform;
		}
		else
		{
			Debug.LogError("Invalid grid position. " + x + " " + z);
			return null;
		}
	}

	public void DestroyAndClearGridObjects()
	{
		if (gridObjects != null)
		{
			int rows = gridObjects.GetLength(0);
			int cols = gridObjects.GetLength(1);

			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < cols; j++)
				{
					GameObject obj = gridObjects[i, j];
					if (obj != null)
					{
						Destroy(obj); // Destroy the GameObject
						gridObjects[i, j] = null; // Clear the element in the matrix
					}
				}
			}
		}
	}
}