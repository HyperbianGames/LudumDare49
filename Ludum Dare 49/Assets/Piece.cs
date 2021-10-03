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

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        RotationIndex = 0;
        Board = board;
        Data = data;
        Position = position;
        stepTime = Time.time + stepDelay;
        lockTime = 0f;
        
        for (int i = 0; i < data.Cells.Length; i++)
        {
            Cells[i] = (Vector3Int)data.Cells[i];
        }
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
        stepTime = Time.time + stepDelay;
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
