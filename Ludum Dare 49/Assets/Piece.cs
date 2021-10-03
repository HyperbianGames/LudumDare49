using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board Board { get; set; }
    public TetrominoData Data { get; set; }
    public Vector3Int Position { get; set; }
    public Vector3Int[] Cells { get; set; } = new Vector3Int[GameData.MaxNumberOfCellsPerPeice];
    public int RotationIndex { get; set; }

    public float stepDelay = 1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float lockTime;
    private int level;

    public Dictionary<int, float> stepDelayPerLevel = new Dictionary<int, float>
    {
        { 1, 0.8f },
        { 2, 0.7166666666666667f },
        { 3, 0.6333333333333333f },
        { 4, 0.55f },
        { 5, 0.4666666666666667f },
        { 6, 0.3833333333333333f },
        { 7, 0.3f },
        { 8, 0.2166666666666667f },
        { 9, 0.1333333333333333f },
        { 10, 0.1f },
        { 11, 0.0833333333333333f },
        { 12, 0.0833333333333333f },
        { 13, 0.0833333333333333f },
        { 14, 0.0666666666666667f },
        { 15, 0.0666666666666667f },
        { 16, 0.0666666666666667f },
        { 17, 0.05f },
        { 18, 0.05f },
        { 19, 0.05f },
        { 20, 0.0333333333333333f },
        { 21, 0.0333333333333333f },
        { 22, 0.0333333333333333f },
        { 23, 0.0333333333333333f },
        { 24, 0.0333333333333333f },
        { 25, 0.0333333333333333f },
        { 26, 0.0333333333333333f },
        { 27, 0.0333333333333333f },
        { 28, 0.0333333333333333f },
        { 29, 0.0333333333333333f },
        { 30, 0.0166666666666667f },
    };

    public void Initialize(Board board, Vector3Int position, TetrominoData data, int currentGameLevel)
    {
        RotationIndex = 0;
        Board = board;
        Data = data;
        Position = position;
        lockTime = 0f;
        level = currentGameLevel;
        SetStepTime();
        for (int i = 0; i < data.Cells.Length; i++)
        {
            Cells[i] = (Vector3Int)data.Cells[i];
        }
    }

    public void SetStepTime()
    {
        if (level > stepDelayPerLevel.Count)
        {
            level = stepDelayPerLevel.Count;
        }


        stepTime = Time.time + stepDelayPerLevel[level];
    }

    public bool HasTile(Vector3Int position)
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            if (Cells[i] + Position == position)
                return true;
        }

        return false;
    }

    private void Update()
    {
        if (Board.GameActive && !Board.MainMenuUI.activeSelf)
        {
            Board.Clear(this);
            lockTime += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                Rotate(-1);
            }
            else if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl))
            {
                Rotate(1);
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Move(Vector2Int.left);
                SetStepTime();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                Move(Vector2Int.right);
            }
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                HardDrop();
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                Move(Vector2Int.down);
            }

            if (Time.time >= stepTime)
            {
                Step();
            }

            Board.Set(this);
        }
        else
        {
            if (!Board.MainMenuUI.activeSelf && Board.GameEndTime + Board.GameEndTimePopup < Time.time)
            {
                Board.GameOverUI.SetActive(true);

                if (Input.anyKey)
                {
                    Board.GoToMenu();
                }
            }
        }
    }

    private void Step()
    {
        SetStepTime();
        Move(Vector2Int.down, true);
        if (lockTime >= lockDelay)
        {
            Lock();

        }
    }

    private void Lock()
    {
        Board.Set(this);
        Board.ClearLines();
        Board.SpawnPiece();
        Board.CalculateBoardWeight(this);
    }

    private void Rotate(int direction)
    {
        int originalRotation = RotationIndex;
        RotationIndex = WrapIndex(RotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        if (!TestWallKicks(RotationIndex, direction))
        {
            RotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3 cell = Cells[i];
            int x, y;
            switch (Data.Tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * GameData.RotationMatrix[0] * direction) + (cell.y * GameData.RotationMatrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * GameData.RotationMatrix[2] * direction) + (cell.y * GameData.RotationMatrix[3] * direction));
                    break;
                default:
                    x = Mathf.RoundToInt((cell.x * GameData.RotationMatrix[0] * direction) + (cell.y * GameData.RotationMatrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * GameData.RotationMatrix[2] * direction) + (cell.y * GameData.RotationMatrix[3] * direction));
                    break;
            }

            this.Cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int currentRotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(currentRotationIndex, rotationDirection);

        for (int i = 0; i < Data.WallKicks.GetLength(1); i++)
        {
            Vector2Int tranlation = Data.WallKicks[wallKickIndex, i];
            if (Move(tranlation))
                return true;
        }

        return false;
    }

    private int GetWallKickIndex(int currentRotationIndex, int rotationDirection)
    {
        int wallKickIndex = RotationIndex * 2;

        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }

        return WrapIndex(wallKickIndex, 0, Data.WallKicks.GetLength(0));
    }

    private int WrapIndex(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - -min);
        }
    }

    public void HardDrop()
    {
        while (Move(Vector2Int.down, true))
        {
            continue;
        }

        Lock();
    }

    private bool Move(Vector2Int translation, bool hardDropOrStep = false)
    {
        Vector3Int newPos = this.Position;
        newPos += (Vector3Int)translation;
        bool valid = Board.IsValidPosition(this, newPos);
        if (valid)
        {
            if (!hardDropOrStep)
            {
                SoundDesigner.Instance.PlayInputEffect();
            }
            Position = newPos;
            lockTime = 0f;
        }

        return valid;
    }
}
