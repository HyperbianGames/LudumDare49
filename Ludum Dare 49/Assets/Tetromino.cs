using UnityEngine;

public enum Tetromino
{
    I,
    O,
    T,
    J,
    L,
    S,
    Z,
}

[System.Serializable]
public class TetrominoData
{
    public Tetromino Tetromino;
    public GameObject Block;
    public Vector2Int[] Cells { get; set; } = new Vector2Int[GameData.MaxNumberOfCellsPerPeice];
    public Vector2Int[,] WallKicks { get; set; }

    public void Initialize()
    {
        this.Cells = GameData.Cells[this.Tetromino];
        this.WallKicks = GameData.WallKicks[this.Tetromino];
        Debug.Log("???");
    }

}