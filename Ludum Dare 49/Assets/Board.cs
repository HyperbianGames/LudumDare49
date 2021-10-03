using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformRotationData
{
    public bool Moving = false;
    public float Goal = 0f;
    public int PassedGoalCount = 0;
    public float PassGoalMod = 0f;
    public bool DirectionGoingPositive = false;
    public float Speed = 2f;
    public int MaxWobbles = 4;

    public void SetMod()
    {
        switch (PassedGoalCount)
        {
            case 0:
                Speed = 10f;
                PassGoalMod = 1.1f;
                break;
            case 1:
                Speed = 8f;
                PassGoalMod = 1.05f;
                break;
            case 2:
                Speed = 4f;
                PassGoalMod = 1.025f;
                break;
            default:
                Speed = 2f;
                PassGoalMod = 1.1f;
                break;
        }
    }
}

public class Board : MonoBehaviour
{
    public TetrominoData[] tetrominoes;
    public Dictionary<Tetromino, Queue<GameObject>> TetrominoPool { get; set; } = new Dictionary<Tetromino, Queue<GameObject>>();
    public Piece ActivePiece { get; set; }
    public bool DrawGameGrid;
    public GameObject GridAnchor;
    public GameObject GridCube;
    public GameObject Platform;
    public SoundDesigner SoundDesign;
    public Vector2Int GridSize;
    private readonly Vector3 poolObjectLocation = new Vector3(-100, 0, 0);
    public Vector3Int SpawnPosition;
    public Vector2 BoardWeight = new Vector2();
    public float failMod;
    private Vector3 gridOffsetFromCenter = new Vector3();
    public GameObject MainMenuUI;
    public GameObject GameOverUI;
    public bool GameActive = false;
    public float GameEndTime { get; set; } = 0;
    public float GameEndTimePopup;
    private PlatformRotationData rotationData = new PlatformRotationData();

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

        if (GridSize.x % 2 == 0)
        {
            gridOffsetFromCenter = new Vector3(-GridSize.x / 2, 0, 0);
        }
        else
        {
            gridOffsetFromCenter = new Vector3(-(GridSize.x / 2) - 0.5f, 0, 0);
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

        SpawnPiece();
    }

    public void GameStart()
    {
        MainMenuUI.SetActive(false);
        ActivePiece.HardDrop();
        Platform.transform.rotation = new Quaternion(0, 0, 0, 0);
        rotationData = new PlatformRotationData();
        ResetBoard();
        GameActive = true;
        SpawnPiece();
        SoundDesign.BeingLevelTheme(1);
        SoundDesign.PlayOptionSelected();
    }

    public void SpawnPiece()
    {
        int random = UnityEngine.Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];
        ActivePiece.Initialize(this, SpawnPosition, data);

        if (IsValidPosition(ActivePiece, SpawnPosition))
        {
            Set(ActivePiece);
        }
        else
        {
            GameOver();
        }
    }

    internal void GoToMenu()
    {
        GameOverUI.SetActive(false);
        MainMenuUI.SetActive(true);
        SoundDesign.BeginMainTheme();
    }

    public void GameOver()
    {
        rotationData.Moving = false;
        GameEndTime = Time.time;
        Clear(ActivePiece);
        for (int x = GridSize.x; x > 0; x--)
        {
            for (int y = GridSize.y; y > 0; y--)
            {
                ReleaseBlock(new Vector3Int(x, y, SpawnPosition.z));
            }
        }

        GameActive = false;
        SoundDesign.BeginLoseTheme();
    }

    public void ResetBoard()
    {
        for (int x = GridSize.x; x > 0; x--)
        {
            for (int y = GridSize.y; y > 0; y--)
            {
                ClearBlock(new Vector3Int(x, y, SpawnPosition.z));
            }
        }
    }

    private void Update()
    {
        ManagePlatformRotation();
    }

    private void ManagePlatformRotation()
    {
        if (rotationData.Moving)
        {
            if (rotationData.Goal >= 0)
            {
                if (rotationData.DirectionGoingPositive)
                {
                    Debug.Log("GOING POSITIVE - 1");
                    GridAnchor.transform.Rotate(0.0f, 0.0f, Time.deltaTime * rotationData.Speed, Space.Self);
                    Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal * rotationData.PassGoalMod}");
                    if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal * rotationData.PassGoalMod)
                    {
                        rotationData.PassedGoalCount += 1;
                        rotationData.DirectionGoingPositive = false;
                        rotationData.SetMod();
                    }

                    if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
                    {
                        rotationData.Moving = false;
                        GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg + rotationData.Goal, Space.Self);
                    }
                }
                else
                {
                    Debug.Log("GOING Negative - 1");
                    GridAnchor.transform.Rotate(0.0f, 0.0f, -Time.deltaTime * rotationData.Speed, Space.Self);
                    Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal * rotationData.PassGoalMod}");
                    if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal * rotationData.PassGoalMod)
                    {
                        rotationData.PassedGoalCount += 1;
                        rotationData.DirectionGoingPositive = true;
                        rotationData.SetMod();

                        if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
                        {
                            rotationData.Moving = false;
                            GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg - rotationData.Goal, Space.Self);
                        }
                    }
                }
               

            }
            else
            {
                if (rotationData.Goal < 0)
                {
                    if (rotationData.DirectionGoingPositive)
                    {
                        Debug.Log("GOING POSITIVE - 2");
                        GridAnchor.transform.Rotate(0.0f, 0.0f, Time.deltaTime * rotationData.Speed, Space.Self);
                        Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal / rotationData.PassGoalMod}");
                        if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal / rotationData.PassGoalMod)
                        {
                            rotationData.PassedGoalCount += 1;
                            rotationData.DirectionGoingPositive = false;
                            rotationData.SetMod();
                        }

                        if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
                        {
                            rotationData.Moving = false;
                            GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg + rotationData.Goal, Space.Self);
                        }
                    }
                    else
                    {
                        Debug.Log("GOING Negative - 2");
                        GridAnchor.transform.Rotate(0.0f, 0.0f, -Time.deltaTime * rotationData.Speed, Space.Self);
                        Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal / rotationData.PassGoalMod}");
                        if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal / rotationData.PassGoalMod)
                        {
                            rotationData.PassedGoalCount += 1;
                            rotationData.DirectionGoingPositive = true;
                            rotationData.SetMod();

                            if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
                            {
                                rotationData.Moving = false;
                                GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg - rotationData.Goal, Space.Self);
                            }
                        }
                    }


                }
            }
        }
        
        //else if (rotationData.Goal <= 0 && GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal)
        //{
        //    Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal}");
        //    GridAnchor.transform.Rotate(0.0f, 0.0f, -0.5f, Space.Self);
        //}
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
                    if (GridSize.x % 2 == 0)
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
        float goal = 0.0f;
        //Debug.Log($"Board Weight: {BoardWeight.x} | {BoardWeight.y} ");
        if (BoardWeight.x > BoardWeight.y)
        {
            goal = ((BoardWeight.y * failMod) - BoardWeight.x) * 0.5f;
            rotationData.DirectionGoingPositive = GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal; ;
        }
        else if (BoardWeight.x < BoardWeight.y)
        {
            goal = ((BoardWeight.x * failMod) - BoardWeight.y) * -0.5f;

            rotationData.DirectionGoingPositive = GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal;
        }

        rotationData.Goal = goal;
        rotationData.Moving = true;
        rotationData.PassedGoalCount = 0;
        

        rotationData.SetMod();


        if ((BoardWeight.x > BoardWeight.y * failMod) || (BoardWeight.y > BoardWeight.x * failMod))
        {
            GameOver();
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
            ObjectGrid[position.x][position.y].Item2.transform.localPosition = poolObjectLocation;
            TetrominoPool[ObjectGrid[position.x][position.y].Item1].Enqueue(ObjectGrid[position.x][position.y].Item2);
            ObjectGrid[position.x].Remove(position.y);
        }

        GameObject newBlock = TetrominoPool[tetrimino].Peek();
        Rigidbody rb = newBlock.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.velocity = new Vector3();
        TetrominoPool[tetrimino].Dequeue();
        ObjectGrid[position.x].Add(position.y, new Tuple<Tetromino, GameObject>(tetrimino, newBlock));

        newBlock.transform.localPosition = position + gridOffsetFromCenter;
    }

    public void ClearBlock(Vector3Int position)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            Rigidbody rb = ObjectGrid[position.x][position.y].Item2.GetComponent<Rigidbody>();
            rb.velocity = new Vector3();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;

            ObjectGrid[position.x][position.y].Item2.transform.localPosition = poolObjectLocation;
            ObjectGrid[position.x][position.y].Item2.transform.rotation = new Quaternion(0, 0, 0, 0);

            TetrominoPool[ObjectGrid[position.x][position.y].Item1].Enqueue(ObjectGrid[position.x][position.y].Item2);
            ObjectGrid[position.x].Remove(position.y);
        }
    }

    public void ReleaseBlock(Vector3Int position)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            //ObjectGrid[position.x][position.y].Item2.transform.localPosition = poolObjectLocation;
            //TetrominoPool[ObjectGrid[position.x][position.y].Item1].Enqueue(ObjectGrid[position.x][position.y].Item2);
            //ObjectGrid[position.x].Remove(position.y);

            Rigidbody rb = ObjectGrid[position.x][position.y].Item2.GetComponent<Rigidbody>();
            rb.velocity = new Vector3();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.freezeRotation = false;
        }
    }


    public void MoveBlockDown(Vector3Int position)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            Tuple<Tetromino, GameObject> block = ObjectGrid[position.x][position.y];
            Vector3Int newPos = new Vector3Int(position.x, position.y - 1, SpawnPosition.z);
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
                if (GridSize.x % 2 == 0)
                {
                    Instantiate(GridCube, new Vector3 { x = x, y = y, z = -1 } + gridOffsetFromCenter, GridAnchor.transform.rotation, GridAnchor.transform);
                }

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
