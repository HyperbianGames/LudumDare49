using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostPiece : MonoBehaviour
{
    public GameObject Block;
    public Board Board;
    public Piece TrackingPiece;

    public Vector3Int[] Cells { get; set; } = new Vector3Int[4];
    public Vector3Int Position { get; set; }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Board.GameActive)
        {
            Clear();
            Copy();
            Drop();
            Set();
        }
    }

    private void Clear()
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3Int tilePosition = Cells[i] + Position;
            Board.ClearBlock(tilePosition, true);
        }
    }

    private void Copy()
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i] = TrackingPiece.Cells[i];
        }
    }

    private void Drop()
    {
        Vector3Int position = TrackingPiece.Position;
        int currentRow = position.y;
        int bottom = 1;

        Board.Clear(TrackingPiece);

        for (int row = currentRow; row >= bottom; row --)
        {
            position.y = row;

            if (Board.IsValidPosition(TrackingPiece, position))
            {
                Position = position;
            }
            else
            {
                break;
            }
        }

        Board.Set(TrackingPiece);


        //newPos += (Vector3Int)translation;
        //bool valid = Board.IsValidPosition(this, newPos);
        //if (valid)
        //{
        //    if (!hardDropOrStep)
        //    {
        //        SoundDesigner.Instance.PlayInputEffect();
        //    }
        //    Position = newPos;
        //    lockTime = 0f;
        //}

        //return valid;
    }

    private void Set()
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3Int tilePosition = Cells[i] + Position;

            Board.SetBlock(tilePosition, Tetromino.Ghost);
        }
    }
}
