using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkSelector
{
    private Dictionary<(GroundType, int), bool[]> openInfo = new Dictionary<(GroundType, int), bool[]>
    {
        // 4Way: 모든 방향 연결
        {(GroundType.Roadway_4way, 0), new bool[]{true, true, true, true } },

        // 3Way: Rotation에 따라 한 면만 막힘
        {(GroundType.Roadway_3way, 0), new bool[]{true, true, false, true } }, // ㅗ
        {(GroundType.Roadway_3way, 1), new bool[]{true, true, true, false} }, // ㅏ
        {(GroundType.Roadway_3way, 2), new bool[]{false, true, true, true} }, // ㅜ
        {(GroundType.Roadway_3way, 3), new bool[]{true, false, true, true} }, // ㅓ

        // 2Way Curved: 인접한 두 면 연결
        {(GroundType.Roadway_2way_Curved, 0), new bool[]{true, true, false, false} }, // └ (우, 상)
        {(GroundType.Roadway_2way_Curved, 1), new bool[]{false, true, true, false} }, // ┌ (우, 하)
        {(GroundType.Roadway_2way_Curved, 2), new bool[]{false, false, true, true} }, // ┐ (좌, 하)
        {(GroundType.Roadway_2way_Curved, 3), new bool[]{true, false, false, true} }, // ┘ (좌, 상)

        // 2Way Straight: 마주보는 두 면 연결
        {(GroundType.Roadway_2way_Straight, 0), new bool[]{true, false, true, false} }, // │ (상, 하)
        {(GroundType.Roadway_2way_Straight, 1), new bool[]{false, true, false, true} },  // ─ (좌, 우)
        
        {(GroundType.Roadway_1way, 0), new bool[]{true, false, false, false} }, 
        {(GroundType.Roadway_1way, 1), new bool[]{false, true, false, false} },
        {(GroundType.Roadway_1way, 2), new bool[]{false, false, true, false} },
        {(GroundType.Roadway_1way, 3), new bool[]{false, false, false, true } },

        {(GroundType.Roadway_0way, 0), new bool[]{false, false, false, false} }, 
    };
    private ChunkMapConfig mapConfig;
    private ChunkPreset[,] chunkPresets;
    public ChunkSelector(ChunkMapConfig mapConfig, ChunkPreset[,] chunkPresets)
    {
        this.mapConfig = mapConfig;
        this.chunkPresets = chunkPresets;
    }

    // -------------- public API ---------------
    public ChunkPreset SelectChunk(ChunkDataConfig dataConfig, System.Random rng, int x, int z)
    {
        List<ChunkPreset> ablePresets = new List<ChunkPreset>();
        bool theresManyWays = false;
        for (int i = 0; i < dataConfig.chunkPresets.Length; i++)
        {
            if (IsRoadType(dataConfig.chunkPresets[i].groundType) == false) continue;
            var candidate = (dataConfig.chunkPresets[i].groundType, RotToInt(dataConfig.chunkPresets[i].rotation));
            if (IsValidPlacement(x, z, candidate))
            {
                if (candidate.groundType != GroundType.Roadway_0way && candidate.groundType != GroundType.Roadway_1way)
                    theresManyWays = true;
                ablePresets.Add(dataConfig.chunkPresets[i]);
            }
        }
        if (ablePresets.Count == 0) return null;

        ChunkPreset result;
        do
        {
            result = ablePresets[rng.Next(ablePresets.Count)];
        } while (theresManyWays && (result.groundType == GroundType.Roadway_0way || result.groundType == GroundType.Roadway_1way));

        return result;
    }


    // -------------- private ---------------

    // 특정 위치(x, y)에 특정 타일 배치가 가능한지 검사
    bool IsValidPlacement(int x, int z, (GroundType, int) candidate)
    {
        return CheckDirection(openInfo[candidate][0], x, z + 1, 2)
            && CheckDirection(openInfo[candidate][1], x + 1, z, 3)
            && CheckDirection(openInfo[candidate][2], x, z - 1, 0)
            && CheckDirection(openInfo[candidate][3], x - 1, z, 1);
    }
    bool CheckDirection(bool isOpen, int nx, int nz, int neighborSide)
    {
        // Closed Map 제약: 그리드 경계 밖은 막힘이어야 함
        if (nx < 0 || nx >= mapConfig.gridWidth || nz < 0 || nz >= mapConfig.gridHeight)
        {
            return isOpen == false;
        }

        // 인접한 칸에 이미 타일이 배치되어 있다면 open이 일치해야 함
        if (chunkPresets[nz, nx] != null)
        {
            bool[] opened = openInfo[(chunkPresets[nz, nx].groundType, RotToInt(chunkPresets[nz, nx].rotation))];

            return isOpen == opened[neighborSide];
        }

        return true; // 아직 배치 안 된 칸은 일단 통과 (나중에 후보에서 필터링됨)
    }

    int RotToInt(Vector3 rotation)
    {
        // 음수 포함 정규화: [-180, 180) 또는 [-360, 360) 모두 처리
        float normalized = ((rotation.y % 360f) + 360f) % 360f; // [0, 360)
        int index = Mathf.FloorToInt(normalized / 90f + 0.5f) % 4; // 반올림
        return index;
    }
    bool IsRoadType(GroundType t) =>
        t == GroundType.Roadway_0way ||
        t == GroundType.Roadway_1way ||
        t == GroundType.Roadway_2way_Curved || 
        t == GroundType.Roadway_2way_Straight || 
        t == GroundType.Roadway_3way || 
        t == GroundType.Roadway_4way;
}