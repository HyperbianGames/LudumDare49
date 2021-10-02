using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public TetrominoData[] tetrominoes;
    public Dictionary<Tetromino, Queue<GameObject>> TetrominoPool { get; set; } = new Dictionary<Tetromino, Queue<GameObject>>();
    public Piece ActivePiece { get; set; }
    public bool DrawGameGrid;
    public GameObject GridAnchor;
    public GameObject GridCube;
    public Vector2Int GridSize;
    private readonly Vector3 poolObjectLocation = new Vector3(-100, 0, 0);
    public Vector3Int SpawnPosition;
    public Vector2 BoardWeight = new Vector2();
    public float failMod;

    public Dictionary<int, Dictionary<int, Tuple<Tetromino, GameObject>>> ObjectGrid { get; set; } = new Dictionary<int, Dictionary<int, Tuple<Tetromino, GameObject>>>();

    private void Awake()
    {
        ActivePiece = GetComponentInChildren<Piece>();

        foreach (TetrominoData data in tetrominoes)
        {
            data.Initialize();
            TetrominoPool[data.Tetromino] = new Queue<GameObject>();
            for (int i = 50; i > 0; i--)
            {
                GameObject newPoolObject = Instantiate(data.Block, poolObjectLocation, GridAnchor.transform.rotation, GridAnchor.transform);
                TetrominoPool[data.Tetromino].Enqueue(newPoolObject);
            }

        }

        if (DrawGameGrid)
        {
            DrawGrid();
        }

        // Used for tracking what is currently in each spot
        for (int x = GridSize.x; x > 0; x--)
        {
            ObjectGrid.Add(x, new Dictionary<int, Tuple<Tetromino, GameObject>>());
        }

        GameStart();
    }

    private void GameStart()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        int random = UnityEngine.Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];
        ActivePiece.Initialize(this, SpawnPosition, data);
        Set(ActivePiece);
    }

    public void CalculateBoardWeight(Piece piece)
    {
        BoardWeight = new Vector2(15, 15);
        for (int x = GridSize.x; x > 0; x--)
        {
            for (int y = GridSize.y; y > 0; y--)
            {
                Vector3Int position = new Vector3Int(x, y, SpawnPosition.z);
                if (!piece.HasTile(position) & HasTile(position))
                {
                    if  (GridSize.x % 2 == 0)
                    {
                        if (x > GridSize.x / 2)
                        {
                            BoardWeight.y += 1;
                        }
                        else
                        {
                            BoardWeight.x += 1;
                        }
                    }
                    else
                    {
                        // Other logic needed
                        Debug.Log("LOGICNEEDED");
                    }                    
                }    
            }
        }

        Debug.Log($"Board Weight: {BoardWeight.x} | {BoardWeight.y} ");

        if ((BoardWeight.x > BoardWeight.y * failMod) || (BoardWeight.y > BoardWeight.x * failMod))
        {
            Debug.Log("YOU LOSE YOU LOSE YOU LOSE");
        }
    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + piece.Position;

            SetBlock(tilePosition, piece.Data.Tetromino);
        }
    }

    public void SetBlock(Vector3Int position, Tetromino tetrimino)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            ObjectGrid[position.x][position.y].Item2.transform.position = poolObjectLocation;
            TetrominoPool[ObjectGrid[position.x][position.y].Item1].Enqueue(ObjectGrid[position.x][position.y].Item2);
            ObjectGrid[position.x].Remove(position.y);
        }

        GameObject newBlock = TetrominoPool[tetrimino].Peek();
        TetrominoPool[tetrimino].Dequeue();
        ObjectGrid[position.x].Add(position.y, new Tuple<Tetromino, GameObject>(tetrimino, newBlock));
        newBlock.transform.position = position;
    }

    public void ClearBlock(Vector3Int position)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            ObjectGrid[position.x][position.y].Item2.transform.position = poolObjectLocation;
            TetrominoPool[ObjectGrid[position.x][position.y].Item1].Enqueue(ObjectGrid[position.x][position.y].Item2);
            ObjectGrid[position.x].Remove(position.y);
        }
    }

    public void MoveBlockDown(Vector3Int position)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            Tuple<Tetromino, GameObject> block = ObjectGrid[position.x][position.y];
            Vector3Int newPos = new Vector3Int(position.x, position.y-1, SpawnPosition.z);
            ClearBlock(position);
            SetBlock(newPos, block.Item1);
        }
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + piece.Position;
            ClearBlock(tilePosition);
        }
    }

    public bool HasTile(Vector3Int position)
    {
        return (ObjectGrid.ContainsKey(position.x) && ObjectGrid[position.x].ContainsKey(position.y));
    }

    public void DrawGrid()
    {
        GridCube.transform.localScale = new Vector3 { x = 0.95f, y = 0.95f, z = 0.05f };
        for (int x = GridSize.x; x > 0; x--)
        {
            for (int y = GridSize.y; y > 0; y--)
            {
                Instantiate(GridCube, new Vector3 { x = x, y = y, z = -1 }, GridAnchor.transform.rotation, GridAnchor.transform);
            }
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + position;
            if (tilePosition.x < 1 || tilePosition.x > GridSize.x)
                return false;

            if (tilePosition.y < 1 || tilePosition.y > GridSize.y)
                return false;

            if (HasTile(tilePosition))
                return false;
        }

        return true;
    }

    public void LineClear(int row)
    {
        for (int col = 1; col <= GridSize.x; col++)
        {
            Vector3Int pos = new Vector3Int(col, row, 0);
            ClearBlock(pos);
        }

        while (row <= GridSize.y)
        {
            for (int col = 1; col <= GridSize.x; col++)
            {
                Vector3Int pos = new Vector3Int(col, row + 1, 0);
                MoveBlockDown(pos);
            }

            row++;
        }
    }

    public void ClearLines()
    {
        int row = 1;
        while (row <= GridSize.y)
        {
            if (IsLineFull(row))
            {
                LineClear(row);
            }
            else
            {
                row++;
            }
        }
    }

    private bool IsLineFull(int row)
    {
        for (int col = 1; col <= GridSize.x; col++)
        {
            Vector3Int pos = new Vector3Int(col, row, 0);
            if (!HasTile(pos))
                return false;
        }

        return true;
    }
}
