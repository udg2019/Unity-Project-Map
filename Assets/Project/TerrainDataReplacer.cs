using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 기존 read-only Terrain을 편집 가능하게 변환
/// 모든 지형 데이터를 그대로 유지합니다
/// </summary>
public class MakeTerrainEditable : MonoBehaviour
{
    [MenuItem("Tools/?? 선택한 Terrain을 편집 가능하게")]
    static void ConvertSelectedTerrain()
    {
        // 선택된 오브젝트에서 Terrain 찾기
        Terrain terrain = Selection.activeGameObject?.GetComponent<Terrain>();
        
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("오류", 
                "Terrain을 선택해주세요!\n\nHierarchy에서 Terrain_0을 선택한 후 다시 시도하세요.", 
                "확인");
            return;
        }

        ConvertTerrain(terrain);
    }

    [MenuItem("Tools/?? 씬의 모든 Terrain을 편집 가능하게")]
    static void ConvertAllTerrains()
    {
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        
        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog("오류", "씬에 Terrain이 없습니다!", "확인");
            return;
        }

        foreach (Terrain terrain in terrains)
        {
            ConvertTerrain(terrain);
        }
        
        EditorUtility.DisplayDialog("완료", 
            $"{terrains.Length}개의 Terrain이 편집 가능해졌습니다!", 
            "확인");
    }

    static void ConvertTerrain(Terrain terrain)
    {
        TerrainData oldData = terrain.terrainData;
        
        if (oldData == null)
        {
            Debug.LogWarning($"{terrain.name}에 TerrainData가 없습니다.");
            return;
        }

        // 새 TerrainData 생성 및 모든 데이터 복사
        TerrainData newData = new TerrainData();
        
        // 기본 설정 복사
        newData.heightmapResolution = oldData.heightmapResolution;
        newData.alphamapResolution = oldData.alphamapResolution;
        newData.baseMapResolution = oldData.baseMapResolution;
        newData.size = oldData.size;
        
        // 높이맵 복사 (지형 모양)
        float[,] heights = oldData.GetHeights(0, 0, 
            oldData.heightmapResolution, 
            oldData.heightmapResolution);
        newData.SetHeights(0, 0, heights);
        
        // 텍스처 레이어 복사
        if (oldData.terrainLayers != null && oldData.terrainLayers.Length > 0)
        {
            newData.terrainLayers = oldData.terrainLayers;
            
            // 알파맵 복사 (텍스처 블렌딩)
            float[,,] alphamaps = oldData.GetAlphamaps(0, 0, 
                oldData.alphamapWidth, 
                oldData.alphamapHeight);
            newData.SetAlphamaps(0, 0, alphamaps);
        }
        
        // 나무 복사
        if (oldData.treeInstances != null && oldData.treeInstances.Length > 0)
        {
            newData.treePrototypes = oldData.treePrototypes;
            newData.treeInstances = oldData.treeInstances;
        }
        
        // Detail (풀) 복사
        if (oldData.detailPrototypes != null && oldData.detailPrototypes.Length > 0)
        {
            newData.detailPrototypes = oldData.detailPrototypes;
            newData.SetDetailResolution(oldData.detailResolution, oldData.detailResolutionPerPatch);
            
            // 모든 Detail 레이어 복사
            for (int i = 0; i < oldData.detailPrototypes.Length; i++)
            {
                int[,] detailLayer = oldData.GetDetailLayer(0, 0, 
                    oldData.detailWidth, 
                    oldData.detailHeight, 
                    i);
                newData.SetDetailLayer(0, 0, i, detailLayer);
            }
        }
        
        // 에셋으로 저장
        string folderPath = "Assets/TerrainData";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "TerrainData");
        }
        
        // 파일명에 원본 Terrain 이름 사용
        string fileName = $"{terrain.name}_편집가능";
        string assetPath = $"{folderPath}/{fileName}.asset";
        
        // 중복 파일 처리
        int counter = 1;
        while (AssetDatabase.LoadAssetAtPath<TerrainData>(assetPath) != null)
        {
            assetPath = $"{folderPath}/{fileName}_{counter}.asset";
            counter++;
        }
        
        // 새 TerrainData를 에셋으로 저장
        AssetDatabase.CreateAsset(newData, assetPath);
        AssetDatabase.SaveAssets();
        
        // Terrain에 새 TerrainData 할당
        terrain.terrainData = newData;
        
        // 씬 저장 알림
        EditorUtility.SetDirty(terrain);
        
        Debug.Log($"? {terrain.name} 변환 완료!\n" +
                  $"?? 저장 위치: {assetPath}\n" +
                  $"?? 이제 Detail 브러시로 풀을 자유롭게 그릴 수 있습니다!");
    }

    // Validation (메뉴 활성화 조건)
    [MenuItem("Tools/?? 선택한 Terrain을 편집 가능하게", true)]
    static bool ValidateConvertSelectedTerrain()
    {
        return Selection.activeGameObject?.GetComponent<Terrain>() != null;
    }
}
#endif