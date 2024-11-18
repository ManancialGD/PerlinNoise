using System;
using UnityEngine;

public class PerlinNoise
{
    private int[] permutation;
    private int[] p; // Permutation table
    private int gridSize;

    /// <summary>
    /// <br>Initializes a new instance of PerlinNoise with a given seed and grid size.</br>
    /// <br>The seed ensures reproducibility of the generated noise, while the grid size controls the resolution of the noise.</br>
    /// </summary>
    /// <param name="seed">The seed for random number generation to ensure reproducibility.</param>
    /// <param name="gridSize">The grid size for generating Perlin noise (default is 16).</param>
    public PerlinNoise(int seed, int gridSize = 16)
    {
        this.gridSize = gridSize;
        System.Random random = new System.Random(seed);

        // Initialize and shuffle permutation array
        permutation = new int[256];
        for (int i = 0; i < 256; i++) permutation[i] = i;

        // Shuffle permutation array for randomness
        for (int i = 0; i < 256; i++)
        {
            int swapIndex = random.Next(256);
            int temp = permutation[i];
            permutation[i] = permutation[swapIndex];
            permutation[swapIndex] = temp;
        }

        // Duplicate the array for wrapping
        p = new int[512];
        for (int i = 0; i < 512; i++) p[i] = permutation[i % 256];
    }

    /// <summary>
    /// <br>Applies a fade function for smooth interpolation of values.</br>
    /// <br>The fade function smooths transitions between values by applying a cubic function.</br>
    /// </summary>
    /// <param name="t">The input value to be faded.</param>
    /// <returns>A faded value that smooths the interpolation.</returns>
    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    /// <summary>
    /// <br>Computes a gradient based on a hash value and 2D coordinates.</br>
    /// <br>This function uses the hash to select a gradient direction, then computes the dot product with the input coordinates.</br>
    /// </summary>
    /// <param name="hash">A hashed value used to determine the gradient direction.</param>
    /// <param name="x">The x-coordinate used in the gradient calculation.</param>
    /// <param name="y">The y-coordinate used in the gradient calculation.</param>
    /// <returns>The dot product of the gradient and the coordinates.</returns>
    private float Gradient(int hash, float x, float y)
    {
        // Generate gradients as unit vectors based on the hash
        float angle = (hash & 15) * Mathf.PI / 8;  // 16 possible angles (22.5° apart)
        return Mathf.Cos(angle) * x + Mathf.Sin(angle) * y;
    }

    /// <summary>
    /// <br>Generates a Perlin noise value for the specified coordinates and parameters.</br>
    /// <br>This method supports multiple octaves, persistence, contrast, and other settings to refine the generated noise.</br>
    /// </summary>
    /// <param name="x">The x-coordinate for which to generate the noise value.</param>
    /// <param name="y">The y-coordinate for which to generate the noise value.</param>
    /// <param name="octaves">The number of octaves (layers of detail) to generate (default is 4).</param>
    /// <param name="persistence">The persistence value controlling amplitude decrease per octave (default is 0.5).</param>
    /// <param name="contrast">The contrast value controlling how sharp or soft the noise is (default is 1.0).</param>
    /// <param name="contrastFirstOctave">Contrast for the first octave (default is 0.5).</param>
    /// <param name="bias">The bias value applied to the first octave for darker results (default is 0.1).</param>
    /// <param name="factor">A factor that further scales the result (default is 1.5).</param>
    /// <returns>The Perlin noise value, normalized between [0, 1].</returns>
    public float GetValue(float x, float y, int octaves = 4, float persistence = 0.5f, float contrast = 1.0f, float contrastFirstOctave = 0.5f, float bias = 0.1f, float factor = 1.5f)
    {
        float total = 0;
        float amplitude = 1;
        float maxAmplitude = 0; // Used for normalization
        int currentGridSize = gridSize;

        for (int i = 0; i < octaves; i++)
        {
            // Get the value for the current octave
            float result = SingleOctaveValue(x, y, currentGridSize);

            // Apply special contrast for the first octave to make it darker
            if (i == 0)
            {
                // Apply contrast enhancement and shift for the first octave
                result = Mathf.Pow(result, contrastFirstOctave); // Apply higher contrast to the first octave
                result = (result * 2.0f - 1.0f) * factor - bias; // Scale and apply bias for darker values
            }
            else
            {
                result = Mathf.Pow(result, contrast);  // Apply normal contrast for other octaves
            }

            // Accumulate the result and prepare for the next octave
            total += result * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            currentGridSize /= 2;  // Double frequency (halve grid size)
        }

        // Normalize the final value between [0, 1]
        return Mathf.Clamp01(total / maxAmplitude);
    }

    /// <summary>
    /// <br>Computes the Perlin noise value for a single octave based on grid cell coordinates.</br>
    /// <br>This method calculates the gradient at each corner of the grid cell and interpolates the values.</br>
    /// </summary>
    /// <param name="x">The x-coordinate within the grid cell.</param>
    /// <param name="y">The y-coordinate within the grid cell.</param>
    /// <param name="gridSize">The size of the grid used for the noise generation.</param>
    /// <returns>The noise value for the single octave, normalized between [0, 1].</returns>
    private float SingleOctaveValue(float x, float y, int gridSize)
    {
        // Determine grid cell coordinates
        int gridX = Mathf.FloorToInt(x / gridSize);
        int gridY = Mathf.FloorToInt(y / gridSize);

        // Relative position within the grid cell
        float localX = (x / gridSize) - gridX;
        float localY = (y / gridSize) - gridY;

        // Compute hashed corners
        int topLeft = p[p[gridX] + gridY];
        int topRight = p[p[gridX + 1] + gridY];
        int bottomLeft = p[p[gridX] + gridY + 1];
        int bottomRight = p[p[gridX + 1] + gridY + 1];

        // Calculate dot products with gradients
        float dotTL = Gradient(topLeft, localX, localY);
        float dotTR = Gradient(topRight, localX - 1, localY);
        float dotBL = Gradient(bottomLeft, localX, localY - 1);
        float dotBR = Gradient(bottomRight, localX - 1, localY - 1);

        // Smooth interpolation
        float u = Fade(localX);
        float v = Fade(localY);

        float lerpTop = Mathf.Lerp(dotTL, dotTR, u);
        float lerpBottom = Mathf.Lerp(dotBL, dotBR, u);

        // Final blend and normalization
        float result = Mathf.Lerp(lerpTop, lerpBottom, v);
        return result * 0.5f + 0.5f;  // Normalize to [0, 1]
    }
}
