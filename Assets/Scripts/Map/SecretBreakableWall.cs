using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Tilemap_SecretWall에 붙이는 스크립트
// 공격을 받으면 맞은 위치의 비밀벽 타일만 제거
public class SecretBreakableWall : MonoBehaviour
{
    [Header("Tilemap")]
    [SerializeField] private Tilemap tilemapSecretWall;

[Header("Break Settings")]
    [SerializeField] private int requiredHitsToBreak = 1;
    [SerializeField] private GameObject breakEffectPrefab;

    // 타일 좌표별 맞은 횟수 저장
    private Dictionary<Vector3Int, int> hitCountByCellPosition = new Dictionary<Vector3Int, int>();

    private void Awake()
    {
        // Tilemap_SecretWall 오브젝트에 붙어있는 Tilemap을 자동으로 가져온다.
        if (tilemapSecretWall == null)
        {
            tilemapSecretWall = GetComponent<Tilemap>();
        }
    }

    public void HitWallAtWorldPosition(Vector2 worldPosition)
    {
        if (tilemapSecretWall == null)
        {
            Debug.LogError("SecretBreakableWall: Tilemap_SecretWall의 Tilemap이 연결되지 않았습니다.");
            return;
        }

        Vector3Int cellPosition = tilemapSecretWall.WorldToCell(worldPosition);

        // 공격 지점이 타일 경계에 걸리면 바로 찾은 칸에 타일이 없을 수 있으므로 주변 칸도 확인한다.
        if (!tilemapSecretWall.HasTile(cellPosition))
        {
            if (!TryFindNearbySecretWallTile(cellPosition, out cellPosition))
            {
                return;
            }
        }

        HitSecretWallTile(cellPosition);
    }

    private void HitSecretWallTile(Vector3Int cellPosition)
    {
        if (!tilemapSecretWall.HasTile(cellPosition)) return;

        if (!hitCountByCellPosition.ContainsKey(cellPosition))
        {
            hitCountByCellPosition[cellPosition] = 0;
        }

        hitCountByCellPosition[cellPosition]++;

        Debug.Log("Secret wall tile hit: " + cellPosition + " / "
            + hitCountByCellPosition[cellPosition] + " / " + requiredHitsToBreak);

        if (hitCountByCellPosition[cellPosition] >= requiredHitsToBreak)
        {
            BreakSecretWallTile(cellPosition);
        }
    }

    private void BreakSecretWallTile(Vector3Int cellPosition)
    {
        Vector3 breakPosition = tilemapSecretWall.GetCellCenterWorld(cellPosition);

        // 해당 칸의 타일만 제거한다. Tilemap_SecretWall 전체를 삭제하지 않는다.
        tilemapSecretWall.SetTile(cellPosition, null);
        tilemapSecretWall.RefreshTile(cellPosition);

        hitCountByCellPosition.Remove(cellPosition);

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, breakPosition, Quaternion.identity);
        }

        Debug.Log("Secret wall tile broken: " + cellPosition);
    }

    private bool TryFindNearbySecretWallTile(Vector3Int centerCell, out Vector3Int foundCell)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int checkCell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);

                if (tilemapSecretWall.HasTile(checkCell))
                {
                    foundCell = checkCell;
                    return true;
                }
            }
        }

        foundCell = centerCell;
        return false;
    }

}
