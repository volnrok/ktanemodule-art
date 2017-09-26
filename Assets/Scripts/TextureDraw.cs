using UnityEngine;
using System.Collections;

public static class TextureDraw {
	private static int Max(int a, int b) {
		return a > b ? a : b;
	}

	private static int Min(int a, int b) {
		return a < b ? a : b;
	}

	private static Vector2 ComponentMax(Vector2[] vectors) {
		Vector2 max = vectors [0];
		for (int i = 0; i < vectors.Length; i++) {
			max = Vector2.Max (max, vectors [i]);
		}
		return max;
	}

	private static Vector2 ComponentMin(Vector2[] vectors) {
		Vector2 min = vectors [0];
		for (int i = 0; i < vectors.Length; i++) {
			min = Vector2.Min (min, vectors [i]);
		}
		return min;
	}
	
	public static void DrawLine(Texture2D tex, Vector2 start, Vector2 end, float width, Color c) {
		// Convert the line to a polygon
		float angle = Mathf.Atan2 (end.x - start.x, end.y - start.y);
		//Debug.Log ("Direction is: " + (end - start).ToString ());
		Vector2 perpendicular = new Vector2 (Mathf.Sin (angle + Mathf.PI / 2), Mathf.Cos (angle + Mathf.PI / 2)) * width / 2;
		//Debug.Log ("Perpendicular is: " + perpendicular.ToString ());
		Vector2[] vertices = new Vector2[4] {
			start - perpendicular,
			start + perpendicular,
			end + perpendicular,
			end - perpendicular
		};

		RasterPolygon (tex, vertices, c);
		RasterCircle (tex, (int)start.x, (int)start.y, (int)(width / 2), c);
		RasterCircle (tex, (int)end.x, (int)end.y, (int)(width / 2), c);
	}

	// Polygon rasterization algorithm:
	// http://alienryderflex.com/polygon_fill/
	public static void RasterPolygon(Texture2D tex, Vector2[] poly, Color c) {
		int nodes, i, j, temp;
		int[] nodeX = new int[poly.Length];

		// Centre the int pixels by shifting the polygon backwards half a pixel TODO: Does this help?
		Vector2 offset = new Vector2(-0.5f, -0.5f);
		for (i = 0; i < poly.Length; i++) {
			poly [i] = poly [i];// + offset;
		}

		// Calculate boundaries
		Vector2 max = ComponentMax (poly);
		Vector2 min = ComponentMin (poly);
		int maxX = Min ((int)(max.x + 1), tex.width);
		int maxY = Min ((int)(max.y + 1), tex.height);
		int minX = Max ((int)(min.x), 0);
		int minY = Max ((int)(min.y), 0);

		// Loop through rows of the image
		for (int y = minY; y < maxY; y++) {
			// Build a list of nodes
			nodes = 0;
			j = poly.Length - 1;
			for (i = 0; i < poly.Length; i++) {
				if (poly [i].y < y && poly [j].y >= y || poly [j].y < y && poly [i].y >= y) {
					nodeX [nodes++] = (int)(poly [i].x + (y - poly [i].y) / (poly [j].y - poly [i].y) * (poly [j].x - poly [i].x));
				}
				j = i;
			}

			// Sort the nodes
			i = 0;
			for (i = 0; i < nodes; i++) {
				for (j = i + 1; j < nodes; j++) {
					if (nodeX[i] > nodeX[j]) {
						temp = nodeX[i];
						nodeX[i] = nodeX[j];
						nodeX[j] = temp;
					}
				}
			}

			// Fill the pixels between node pairs
			for (i = 0; i < nodes; i += 2) {
				if (nodeX [i] >= maxX) {
					break;
				} else if (nodeX [i + 1] > minX) {
					if (nodeX [i] < minX) {
						nodeX [i] = minX;
					}
					if (nodeX [i + 1] > maxX) {
						nodeX [i + 1] = maxX;
					}
					for (int x = nodeX [i]; x < nodeX [i + 1]; x++) {
						tex.SetPixel (x, y, c);
					}
				}
			}
		}
	}

	private static void HorizontalLine(Texture2D tex, int x0, int x1, int y, Color c) {
		if (y < 0 || y >= tex.height) {
			return;
		}
		x0 = Max (x0, 0);
		x1 = Min (x1, tex.width - 1);
		for (int x = x0; x <= x1; x++) {
			tex.SetPixel (x, y, c);
		}
	}

	// https://en.wikipedia.org/wiki/Midpoint_circle_algorithm
	public static void RasterCircle(Texture2D tex, int x0, int y0, int radius, Color c) {
		int x = radius-1;
		int y = 0;
		int dx = 1;
		int dy = 1;
		int err = dx - (radius << 1);

		while (x >= y)
		{
			HorizontalLine (tex, x0 - x, x0 + x, y0 + y, c);
			HorizontalLine (tex, x0 - x, x0 + x, y0 - y, c);
			HorizontalLine (tex, x0 - y, x0 + y, y0 + x, c);
			HorizontalLine (tex, x0 - y, x0 + y, y0 - x, c);

			if (err <= 0)
			{
				y++;
				err += dy;
				dy += 2;
			}
			if (err > 0)
			{
				x--;
				dx += 2;
				err += (-radius << 1) + dx;
			}
		}
	}
}
