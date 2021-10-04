using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class PlatformRotationData
{
    public bool Moving = false;
    public float Goal = 0f;
    public int PassedGoalCount = 0;
    public float PassGoalMod = 0f;
    public bool DirectionGoingPositive = false;
    public float Speed = 2f;
    public int MaxWobbles = 4;
    public int MaxWeightDifference = 0;

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

public class Score
{
    public string score;
    public string name;

    public Score(string thisScore, string thisName)
    {
        score = thisScore;
        name = thisName;
    }

    public static List<Score> ParseRaw(string rawText)
    {
        List<Score> scores = new List<Score>();
        string[] charsToRemove = new string[] { "{", "}", "\n", "\"" };

        foreach (string c in charsToRemove)
        {
            rawText = rawText.Replace(c, string.Empty);
        }

        string[] lines = rawText.Split(',');

        foreach (string line in lines)
        {
            if (!line.Contains(":"))
                continue;

            Debug.Log(line);
            string[] piece = line.Split(':');
            scores.Add(new Score(piece[1], piece[0]));
        }

        return scores;
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
    public GameObject LevelLabel;
    public GameObject ScoreLable;
    public int StartingLevel = 1;
    private Tetromino savePiece = Tetromino.Ghost;
    private Tetromino nextPiece;
    private GameObject[] savedPieceCells = new GameObject[4];
    private GameObject[] nextPieceCells = new GameObject[4];

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
    private int currentLevel = 1;
    private int currentScore = 0;
    private int numberOfRowsCleared = 0;
    private bool isSentScore = false;
    private Dictionary<int, int> lineClearMods = new Dictionary<int, int>
    {
        { 0, 0 },
        { 1, 1 },
        { 2, 2 },
        { 3, 4 },
        { 4, 8 },
    };

    public GameObject HighScoresTable;
    public GameObject PlayerInput;
    public GameObject PostHighScoreButton;

    public Dictionary<int, Dictionary<int, Tuple<Tetromino, GameObject>>> ObjectGrid { get; set; } = new Dictionary<int, Dictionary<int, Tuple<Tetromino, GameObject>>>();

    public Dictionary<int, int> WeightMods { get; set; } = new Dictionary<int, int>();

    private void Awake()
    {
        ActivePiece = GetComponentInChildren<Piece>();
        GenerateWeightMods();

        foreach (TetrominoData data in tetrominoes)
        {
            data.Initialize();
            TetrominoPool[data.Tetromino] = new Queue<GameObject>();
            for (int i = 100; i > 0; i--)
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

        //nextPiece = GetNextPiece();
        //SetNextPiece(nextPiece);
        //SpawnPiece();
    }

    public void GenerateWeightMods()
    {
        int columsPerSide = (int)Mathf.Floor(GridSize.x / 2);
        int midColumn = 0;
        int columnMod = columsPerSide * -1;

        if (GridSize.x % 2 != 0)
        {
            midColumn = columsPerSide + 1;
        }

        for (int col = 1; col <= GridSize.x; col++)
        {
            if (midColumn == col)
            {
                WeightMods.Add(col, 0);
            }
            else
            {
                int actualMod = columnMod < 0 ? columnMod * -1 : columnMod;                
                WeightMods.Add(col, actualMod);
                columnMod++;
                if (columnMod == 0)
                    columnMod++;
            }
        }
    }

    public void LateUpdate()
    {
        LevelLabel.GetComponent<TextMeshProUGUI>().SetText(currentLevel.ToString());
        ScoreLable.GetComponent<TextMeshProUGUI>().SetText(currentScore.ToString());
    }

    public void GameStart()
    {
        MainMenuUI.SetActive(false);
//ActivePiece.HardDrop();
        GridAnchor.transform.rotation = new Quaternion(0, 0, 0, 0);
        rotationData = new PlatformRotationData();
        ResetBoard();
        GameActive = true;
        nextPiece = GetNextPiece();
        SetNextPiece(nextPiece);
        SpawnPiece();
        SoundDesign.BeingLevelTheme(1);
        SoundDesign.PlayOptionSelected();
        savePiece = Tetromino.Ghost;
        currentLevel = StartingLevel;
        currentScore = 0;
        isSentScore = false;
        PostHighScoreButton.SetActive(true);
    }

    public Queue<Tetromino> DrawList { get; set; } = new Queue<Tetromino>();

    public void ShuffleList()
    {
        List<Tetromino> tetrominos = new List<Tetromino>();

        for(int i = 0; i < tetrominoes.Length - 1; i++)
        {
            tetrominos.Add((Tetromino)i);
            //tetrominos.Add((Tetromino)i);
        }

        System.Random rand = new System.Random();
        while (tetrominos.Count > 0)
        {
            int index = rand.Next(tetrominos.Count);
            DrawList.Enqueue(tetrominos[index]);
            tetrominos.RemoveAt(index);
        }
    }

    public Tetromino GetNextPiece()
    {
        if (DrawList.Count == 0)
            ShuffleList();

        Tetromino nextPiece = DrawList.Peek();
        DrawList.Dequeue();
        return nextPiece;
    }

    public void SpawnPiece(bool swapWithSavedPiece = false)
    {
        if (swapWithSavedPiece)
        {
            Tetromino savedPiece = savePiece;
            if (savedPiece == Tetromino.Ghost)
            {
                savePiece = ActivePiece.Data.Tetromino;
                ActivePiece.Initialize(this, SpawnPosition, tetrominoes[(int)GetNextPiece()], currentLevel);
            }
            else
            {
                ClearSavedPiece(savedPiece);
                savePiece = ActivePiece.Data.Tetromino;
                
       
                ActivePiece.Initialize(this, SpawnPosition, tetrominoes[(int)savedPiece], currentLevel);
            }

            SetSavedPiece(savePiece);
        }
        else
        {
            int currentNextPiece = (int)nextPiece;
            ClearNextPiece(nextPiece);
            nextPiece = GetNextPiece();
            SetNextPiece(nextPiece);
            TetrominoData data = tetrominoes[currentNextPiece];
            ActivePiece.Initialize(this, SpawnPosition, data, currentLevel);
        }
        
        

        if (IsValidPosition(ActivePiece, SpawnPosition))
        {
            Set(ActivePiece);
        }
        else
        {
            GameOver();
        }
    }

    public void GoToMenu()
    {
        GameOverUI.SetActive(false);
        MainMenuUI.SetActive(true);
        SoundDesign.BeginMainTheme();
    }

    public void GameOver()
    {
        if (UnityEngine.Object.ReferenceEquals(savePiece, null))
        {
            ClearSavedPiece(savePiece);
        }

        rotationData.Moving = false;
        GameEndTime = Time.time;
        Clear(ActivePiece);
        ClearNextPiece(nextPiece);
        for (int x = GridSize.x; x > 0; x--)
        {
            for (int y = GridSize.y; y > 0; y--)
            {
                ReleaseBlock(new Vector3Int(x, y, SpawnPosition.z));
            }
        }

        GetHighScores();
        GameActive = false;
        SoundDesign.BeginLoseTheme();
    }

    public void SendHighScore()
    {
        if (isSentScore)
            return;

        try
        {
            string name = PlayerInput.GetComponent<TMP_InputField>().text;
            PostHighScoreButton.SetActive(false);

            UnityWebRequest postScore = UnityWebRequest.Post($"http://8c19-136-52-105-57.ngrok.io/newscore:{name}:{currentScore}", "");
            postScore.SendWebRequest();
            isSentScore = true;
            StartCoroutine(RefreshScores());
        }
        catch(Exception ex)
        {
            Debug.Log(ex);
        }
    }

    IEnumerator RefreshScores()
    {
        yield return new WaitForSeconds(1);
        GetHighScores();
    }

    public void GetHighScores()
    {
        string highscores = "";

        try
        {
            StartCoroutine(GetRequest("http://8c19-136-52-105-57.ngrok.io/getscores", (UnityWebRequest req) =>
            {
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log($"{req.error}: {req.downloadHandler.text}");
                }
                else
                {
                    List<Score> scores = Score.ParseRaw(req.downloadHandler.text);

                    foreach (Score score in scores)
                    {
                        highscores += $"{score.name}: {score.score}\n";
                    }

                    HighScoresTable.GetComponent<TextMeshProUGUI>().text = highscores;
                }
            }));
        }
        catch
        {
            // Do nothing I suppose
        }
    }

    IEnumerator GetRequest(string uri, Action<UnityWebRequest> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(uri))
        {
            yield return request.SendWebRequest();
            callback(request);
        }      
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
        if (UnityEngine.Object.ReferenceEquals(nextPiece, null))
        {
            ClearNextPiece(nextPiece);
        }
    }

    private void Update()
    {
        ManagePlatformRotation();
    }

    public void Credits()
    {
        SceneManager.LoadScene("credits", LoadSceneMode.Single);
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void ManagePlatformRotation()
    {
        if (rotationData.Moving)
        {
            rotationData.Speed = Math.Abs((GridAnchor.transform.rotation.z * Mathf.Rad2Deg) - rotationData.Goal);


            if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal)
            {
                GridAnchor.transform.Rotate(0.0f, 0.0f, Time.deltaTime * rotationData.Speed, Space.Self);

                if (!(GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal))
                {
                    rotationData.PassedGoalCount++;
                }
            }
            else if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal)
            {
                GridAnchor.transform.Rotate(0.0f, 0.0f, -Time.deltaTime * rotationData.Speed, Space.Self);

                if (!(GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal))
                {
                    rotationData.PassedGoalCount++;
                }
            }

            if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg < -7 || GridAnchor.transform.rotation.z * Mathf.Rad2Deg > 7)
                GameOver();

            //if (rotationData.Goal >= 0)
            //{
            //    if (rotationData.DirectionGoingPositive)
            //    {
            //        Debug.Log("GOING POSITIVE - 1");
            //        GridAnchor.transform.Rotate(0.0f, 0.0f, Time.deltaTime * rotationData.Speed, Space.Self);
            //        Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal * rotationData.PassGoalMod}");
            //        if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal * rotationData.PassGoalMod)
            //        {
            //            rotationData.PassedGoalCount += 1;
            //            rotationData.DirectionGoingPositive = false;
            //            rotationData.SetMod();
            //        }

            //        if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
            //        {
            //            rotationData.Moving = false;
            //            GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg + rotationData.Goal, Space.Self);
            //        }
            //    }
            //    else
            //    {
            //        Debug.Log("GOING Negative - 1");
            //        GridAnchor.transform.Rotate(0.0f, 0.0f, -Time.deltaTime * rotationData.Speed, Space.Self);
            //        Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal * rotationData.PassGoalMod}");
            //        if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal * rotationData.PassGoalMod)
            //        {
            //            rotationData.PassedGoalCount += 1;
            //            rotationData.DirectionGoingPositive = true;
            //            rotationData.SetMod();

            //            if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
            //            {
            //                rotationData.Moving = false;
            //                GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg - rotationData.Goal, Space.Self);
            //            }
            //        }
            //    }


            //}
            //else
            //{
            //    if (rotationData.Goal < 0)
            //    {
            //        if (rotationData.DirectionGoingPositive)
            //        {
            //            Debug.Log("GOING POSITIVE - 2");
            //            GridAnchor.transform.Rotate(0.0f, 0.0f, Time.deltaTime * rotationData.Speed, Space.Self);
            //            Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal / rotationData.PassGoalMod}");
            //            if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal / rotationData.PassGoalMod)
            //            {
            //                rotationData.PassedGoalCount += 1;
            //                rotationData.DirectionGoingPositive = false;
            //                rotationData.SetMod();
            //            }

            //            if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
            //            {
            //                rotationData.Moving = false;
            //                GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg + rotationData.Goal, Space.Self);
            //            }
            //        }
            //        else
            //        {
            //            Debug.Log("GOING Negative - 2");
            //            GridAnchor.transform.Rotate(0.0f, 0.0f, -Time.deltaTime * rotationData.Speed, Space.Self);
            //            Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal / rotationData.PassGoalMod}");
            //            if (GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal / rotationData.PassGoalMod)
            //            {
            //                rotationData.PassedGoalCount += 1;
            //                rotationData.DirectionGoingPositive = true;
            //                rotationData.SetMod();

            //                if (rotationData.PassedGoalCount > rotationData.MaxWobbles)
            //                {
            //                    rotationData.Moving = false;
            //                    GridAnchor.transform.Rotate(0.0f, 0.0f, GridAnchor.transform.rotation.z * Mathf.Rad2Deg - rotationData.Goal, Space.Self);
            //                }
            //            }
            //        }


            //    }
            //}
        }
        
        //else if (rotationData.Goal <= 0 && GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal)
        //{
        //    Debug.Log($"Rotate current : {GridAnchor.transform.rotation.z * Mathf.Rad2Deg} goal : {rotationData.Goal}");
        //    GridAnchor.transform.Rotate(0.0f, 0.0f, -0.5f, Space.Self);
        //}
    }

    public void CalculateBoardWeight(Piece piece)
    {
        BoardWeight = new Vector2(30, 30);

        for (int x = GridSize.x; x > 0; x--)
        {
            for (int y = GridSize.y; y > 0; y--)
            {
                Vector3Int position = new Vector3Int(x, y, SpawnPosition.z);
                if (HasTile(position) && !piece.HasTile(position))
                {
                    if (GridSize.x % 2 == 0)
                    {
                        if (x > GridSize.x / 2)
                        {
                            try
                            {
                                BoardWeight.y += 1 * WeightMods[x];
                            }
                            catch (Exception ex)
                            {
                                Debug.Log($"asdfasd {ex.Message}");
                            }

                        }
                        else
                        {

                            try
                            {
                                BoardWeight.x += 1 * WeightMods[x];
                            }
                            catch (Exception ex)
                            {
                                Debug.Log($"asdfasd {ex.Message}");
                            }

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

        int maxDifference = Mathf.Clamp((int)(BoardWeight.x + BoardWeight.y / 5), 15, 35);
        rotationData.Goal = (BoardWeight.x - BoardWeight.y) / 3f;
        rotationData.Moving = true;

        //float goal = 0.0f;
        ////Debug.Log($"Board Weight: {BoardWeight.x} | {BoardWeight.y} ");
        //if (BoardWeight.x > BoardWeight.y)
        //{
        //    goal = ((BoardWeight.y * failMod) - BoardWeight.x) * 0.5f;
        //    rotationData.DirectionGoingPositive = GridAnchor.transform.rotation.z * Mathf.Rad2Deg > rotationData.Goal; ;
        //}
        //else if (BoardWeight.x < BoardWeight.y)
        //{
        //    goal = ((BoardWeight.x * failMod) - BoardWeight.y) * -0.5f;

        //    rotationData.DirectionGoingPositive = GridAnchor.transform.rotation.z * Mathf.Rad2Deg < rotationData.Goal;
        //}

        //rotationData.Goal = goal;
        //rotationData.Moving = true;
        //rotationData.PassedGoalCount = 0;


        //rotationData.SetMod();


        

        //if (Math.Abs(BoardWeight.x - BoardWeight.y) > maxDifference)
        //{
        //    GameOver();
        //}

        //if ((BoardWeight.x > BoardWeight.y * failMod) || (BoardWeight.y > BoardWeight.x * failMod))
        //{
        //    GameOver();
        //}
    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + piece.Position;

            SetBlock(tilePosition, piece.Data.Tetromino);
        }
    }
   
    public void ClearSavedPiece(Tetromino tetrimino)
    {
        for (int i = 0; i < savedPieceCells.Length; i++)
        {
            GameObject obj = savedPieceCells[i];
            obj.transform.localPosition = poolObjectLocation;
            TetrominoPool[tetrimino].Enqueue(obj);
        }
    }

    public void SetSavedPiece(Tetromino tetrimino)
    {        
        for (int i = 0; i < savedPieceCells.Length; i++)
        {
            savedPieceCells[i] = TetrominoPool[tetrimino].Peek();
            TetrominoPool[tetrimino].Dequeue();

            Rigidbody rb = savedPieceCells[i].GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.velocity = new Vector3();

            savedPieceCells[i].transform.localPosition = new Vector3(tetrominoes[(int)tetrimino].Cells[i].x - (GridSize.x /2) - 3, tetrominoes[(int)tetrimino].Cells[i].y + 1, SpawnPosition.z);
        }
    }

    public void ClearNextPiece(Tetromino tetrimino)
    {
        for (int i = 0; i < nextPieceCells.Length; i++)
        {
            GameObject obj = nextPieceCells[i];
            obj.transform.localPosition = poolObjectLocation;
            TetrominoPool[tetrimino].Enqueue(obj);
        }
    }

    public void SetNextPiece(Tetromino tetrimino)
    {
        for (int i = 0; i < nextPieceCells.Length; i++)
        {
            nextPieceCells[i] = TetrominoPool[tetrimino].Peek();
            TetrominoPool[tetrimino].Dequeue();

            Rigidbody rb = nextPieceCells[i].GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.velocity = new Vector3();

            nextPieceCells[i].transform.localPosition = new Vector3(tetrominoes[(int)tetrimino].Cells[i].x + (GridSize.x / 2) + 4, tetrominoes[(int)tetrimino].Cells[i].y + 1, SpawnPosition.z);
        }
    }

    public void SetBlock(Vector3Int position, Tetromino tetrimino)
    {
        if (ObjectGrid[position.x].ContainsKey(position.y))
        {
            if (tetrimino == Tetromino.Ghost)
                return;

            ObjectGrid[position.x][position.y].Item2.transform.localPosition = poolObjectLocation;
            TetrominoPool[ObjectGrid[position.x][position.y].Item1].Enqueue(ObjectGrid[position.x][position.y].Item2);
            ObjectGrid[position.x].Remove(position.y);
        }

        GameObject newBlock = TetrominoPool[tetrimino].Peek();
        TetrominoPool[tetrimino].Dequeue();
        Rigidbody rb = newBlock.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.velocity = new Vector3();
        ObjectGrid[position.x].Add(position.y, new Tuple<Tetromino, GameObject>(tetrimino, newBlock));

        newBlock.transform.localPosition = position + gridOffsetFromCenter;
    }

    public void ClearBlock(Vector3Int position, bool isGhost = false)
    {
        if (ObjectGrid.ContainsKey(position.x) && ObjectGrid[position.x].ContainsKey(position.y))
        {
            if (isGhost && ObjectGrid[position.x][position.y].Item1 != Tetromino.Ghost)
                return;
            
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
        return (ObjectGrid.ContainsKey(position.x) && ObjectGrid[position.x].ContainsKey(position.y) && ObjectGrid[position.x][position.y].Item1 != Tetromino.Ghost);
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
        int linesCleared = 0;
        while (row <= GridSize.y)
        {
            if (IsLineFull(row))
            {
                linesCleared++;
                numberOfRowsCleared++;
                if (numberOfRowsCleared == 10)
                {
                    currentLevel++;
                    SoundDesign.BeingLevelTheme(currentLevel);
                    numberOfRowsCleared = 0;
                }

                LineClear(row);
            }
            else
            {
                row++;
            }
        }

        currentScore += lineClearMods[linesCleared] * 1000;
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

