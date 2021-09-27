using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CoverTile : MonoBehaviour
{
    Vector2[,][] neighbors;
    Stopwatch timer;
    // Start is called before the first frame update
    void Start()
    {
        timer = new Stopwatch();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            timer.Start();
            int width = 100;
            int height = 100;

            neighbors = new Vector2[width, height][];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    List<Vector2> neighborList = new List<Vector2>();
                    for (int i = -1; i <= 1; i++)
                    {
                        int sweepY = y + i;
                        if (sweepY < 0 || sweepY > height - 1) continue;

                        for (int j = -1; j <= 1; j++)
                        {
                            int sweepX = x + j;
                            if (sweepX < 0 || sweepX > width - 1 || i == 0 && j == 0) continue;

                            neighborList.Add(new Vector2(sweepX, sweepY));
                        }
                    }
                    this.neighbors[x, y] = neighborList.ToArray();
                }
            }
            timer.Stop();
            print(timer.Elapsed.ToString(@"g"));
            timer.Reset();
        }
    }
}
